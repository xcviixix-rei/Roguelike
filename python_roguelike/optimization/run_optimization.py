"""
run_optimization.py  –  Main entry point for NSGA-II game balance optimization.

Usage:
    python -m python_roguelike.optimization.run_optimization \
        --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip \
        --pop-size 50 \
        --generations 30 \
        --runs-per-genome 20 \
        --output results/

See --help for all options.
"""

import argparse
import json
import os
import sys
import time
from datetime import datetime

import numpy as np

from pymoo.algorithms.moo.nsga2 import NSGA2
from pymoo.operators.crossover.sbx import SBX
from pymoo.operators.mutation.pm import PM
from pymoo.operators.sampling.rnd import FloatRandomSampling
from pymoo.optimize import minimize
from pymoo.termination import get_termination

from .hierarchical_genome import HierarchicalGenome
from .fitness import MultiObjectiveEvaluator
from .simulation_runner import SimulationRunner, PPOAgent
from .nsga2_problem import RoguelikeBalanceProblem


def parse_args():
    p = argparse.ArgumentParser(
        description="NSGA-II multi-objective game balance optimization with PPO agents"
    )
    p.add_argument(
        "--models", nargs="+", required=True,
        help="Paths to trained PPO model .zip files "
             "(e.g. aggressive.zip defensive.zip balanced.zip adaptive.zip)"
    )
    p.add_argument(
        "--game-data", type=str, default=None,
        help="Path to GameData.json (default: python_roguelike/GameData.json)"
    )
    p.add_argument(
        "--pop-size", type=int, default=50,
        help="NSGA-II population size (default: 50)"
    )
    p.add_argument(
        "--generations", type=int, default=30,
        help="Number of generations (default: 30)"
    )
    p.add_argument(
        "--runs-per-genome", type=int, default=20,
        help="Simulation runs per genome per agent (default: 20)"
    )
    p.add_argument(
        "--seed", type=int, default=42,
        help="Random seed (default: 42)"
    )
    p.add_argument(
        "--output", type=str, default="optimization_results",
        help="Output directory for results (default: optimization_results)"
    )
    p.add_argument(
        "--no-masking", action="store_true",
        help="Disable action masking (use regular PPO instead of MaskablePPO)"
    )
    p.add_argument(
        "--eta-crossover", type=float, default=20.0,
        help="SBX crossover distribution index (default: 20.0)"
    )
    p.add_argument(
        "--eta-mutation", type=float, default=20.0,
        help="Polynomial mutation distribution index (default: 20.0)"
    )
    p.add_argument(
        "--mutation-prob", type=float, default=None,
        help="Per-variable mutation probability (default: 1/n_var)"
    )
    p.add_argument(
        "--crossover-prob", type=float, default=0.9,
        help="Crossover probability (default: 0.9)"
    )
    p.add_argument(
        "--verbose", action="store_true", default=True,
        help="Print per-evaluation progress"
    )
    p.add_argument(
        "--quiet", action="store_true",
        help="Suppress per-evaluation output"
    )
    return p.parse_args()


def save_results(result, output_dir: str, args):
    """Save Pareto front genomes and metadata."""
    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    pareto_X = result.X
    pareto_F = result.F

    pareto_genomes = []
    for i in range(pareto_X.shape[0]):
        genome = HierarchicalGenome.from_vector(pareto_X[i])
        genome_dict = {
            "index": i,
            "balance_score": float(-pareto_F[i, 0]),
            "engagement_score": float(-pareto_F[i, 1]),
            "coherence_score": float(-pareto_F[i, 2]),
            "parameters": {
                spec[0]: float(pareto_X[i, j])
                for j, spec in enumerate(HierarchicalGenome.PARAM_SPECS)
            },
        }
        pareto_genomes.append(genome_dict)

    pareto_path = os.path.join(output_dir, f"pareto_front_{timestamp}.json")
    with open(pareto_path, "w") as f:
        json.dump(pareto_genomes, f, indent=2)
    print(f"\nPareto front ({len(pareto_genomes)} solutions) saved to: {pareto_path}")

    scores = -pareto_F
    ideal = scores.max(axis=0)
    nadir = scores.min(axis=0)
    range_ = nadir - ideal
    range_[range_ == 0] = 1.0
    normalised = (scores - ideal) / range_
    distances = np.sqrt((normalised ** 2).sum(axis=1))
    best_idx = int(distances.argmin())

    best_genome = HierarchicalGenome.from_vector(pareto_X[best_idx])
    best_path = os.path.join(output_dir, f"best_compromise_{timestamp}.json")
    with open(best_path, "w") as f:
        json.dump(pareto_genomes[best_idx], f, indent=2)
    print(f"Best compromise solution saved to: {best_path}")

    _save_balanced_game_data(best_genome, output_dir, timestamp, args)

    meta = {
        "timestamp": timestamp,
        "pop_size": args.pop_size,
        "generations": args.generations,
        "runs_per_genome": args.runs_per_genome,
        "seed": args.seed,
        "models": args.models,
        "n_pareto_solutions": len(pareto_genomes),
        "best_compromise_index": best_idx,
        "best_scores": {
            "balance": float(scores[best_idx, 0]),
            "engagement": float(scores[best_idx, 1]),
            "coherence": float(scores[best_idx, 2]),
        },
    }
    meta_path = os.path.join(output_dir, f"run_metadata_{timestamp}.json")
    with open(meta_path, "w") as f:
        json.dump(meta, f, indent=2)
    print(f"Run metadata saved to: {meta_path}")

    return pareto_genomes, best_idx


def _save_balanced_game_data(
    genome: HierarchicalGenome,
    output_dir: str,
    timestamp: str,
    args,
):
    """Apply best genome to GameData.json and save the balanced version."""
    import copy
    from ..data_loader import load_game_data
    from . import hierarchical_applicator

    json_path = args.game_data
    if json_path is None:
        json_path = os.path.join(
            os.path.dirname(os.path.dirname(__file__)), "GameData.json"
        )

    card_pool, relic_pool, enemy_pool, effect_pool, event_pool, room_configs, hero_data = (
        load_game_data(json_path)
    )

    card_pool = copy.deepcopy(card_pool)
    relic_pool = copy.deepcopy(relic_pool)
    enemy_pool = copy.deepcopy(enemy_pool)
    hero_data = copy.deepcopy(hero_data)
    hierarchical_applicator.apply(genome, card_pool, enemy_pool, relic_pool, hero_data)

    with open(json_path, "r", encoding="utf-8") as f:
        game_data = json.load(f)

    for card_dict in game_data.get("Cards", []):
        card_id = card_dict["Id"]
        if card_id in card_pool.cards_by_id:
            card = card_pool.cards_by_id[card_id]
            card_dict["ManaCost"] = card.mana_cost
            for j, action in enumerate(card_dict.get("Actions", [])):
                if j < len(card.actions):
                    action["Value"] = card.actions[j].value

    for enemy_dict in game_data.get("Enemies", []):
        enemy_id = enemy_dict["Id"]
        if enemy_id in enemy_pool.enemies_by_id:
            enemy = enemy_pool.enemies_by_id[enemy_id]
            enemy_dict["StartingHealth"] = enemy.starting_health
            for j, wa in enumerate(enemy_dict.get("ActionSet", [])):
                if j < len(enemy.action_set):
                    wa["Action"]["Value"] = enemy.action_set[j].item.value

    hero_section = game_data.get("Hero", {})
    hero_section["StartingHealth"] = hero_data.starting_health
    hero_section["StartingGold"] = hero_data.starting_gold
    hero_section["StartingMana"] = hero_data.starting_mana

    balanced_path = os.path.join(output_dir, f"GameData_balanced_{timestamp}.json")
    with open(balanced_path, "w", encoding="utf-8") as f:
        json.dump(game_data, f, indent=2, ensure_ascii=False)
    print(f"Balanced GameData.json saved to: {balanced_path}")


def main():
    args = parse_args()
    verbose = args.verbose and not args.quiet

    print("=" * 70)
    print("  NSGA-II Game Balance Optimization (Python + PPO)")
    print("=" * 70)
    print(f"  Models        : {args.models}")
    print(f"  Population    : {args.pop_size}")
    print(f"  Generations   : {args.generations}")
    print(f"  Runs/genome   : {args.runs_per_genome}")
    print(f"  Seed          : {args.seed}")
    print(f"  Output        : {args.output}")
    print(f"  Action masking: {'disabled' if args.no_masking else 'enabled'}")
    print("=" * 70)

    agents = []
    for path in args.models:
        agent = PPOAgent(model_path=path, use_masking=not args.no_masking)
        agent.load()
        agents.append(agent)
        print(f"  Loaded agent: {path}")

    runner = SimulationRunner(
        agents=agents,
        json_path=args.game_data,
    )

    evaluator = MultiObjectiveEvaluator()

    problem = RoguelikeBalanceProblem(
        runner=runner,
        evaluator=evaluator,
        runs_per_genome=args.runs_per_genome,
        base_seed=args.seed,
        verbose=verbose,
    )

    n_var = HierarchicalGenome.N_CONTINUOUS
    mut_prob = args.mutation_prob if args.mutation_prob else 1.0 / n_var

    algorithm = NSGA2(
        pop_size=args.pop_size,
        sampling=FloatRandomSampling(),
        crossover=SBX(
            prob=args.crossover_prob,
            eta=args.eta_crossover,
        ),
        mutation=PM(
            prob=mut_prob,
            eta=args.eta_mutation,
        ),
        eliminate_duplicates=True,
    )

    termination = get_termination("n_gen", args.generations)

    print(f"\nStarting NSGA-II optimization...")
    t_start = time.time()

    result = minimize(
        problem,
        algorithm,
        termination,
        seed=args.seed,
        verbose=True,
        save_history=False,
    )

    elapsed = time.time() - t_start
    print(f"\nOptimization completed in {elapsed / 60:.1f} minutes")
    print(f"Total evaluations: {problem._eval_count}")

    if result.X is not None and len(result.X.shape) > 0:
        pareto, best_idx = save_results(result, args.output, args)
        print(f"\n{'=' * 70}")
        print(f"  RESULTS SUMMARY")
        print(f"{'=' * 70}")
        print(f"  Pareto front size : {len(pareto)}")
        best = pareto[best_idx]
        print(f"  Best compromise   : Balance={best['balance_score']:.3f}  "
              f"Engagement={best['engagement_score']:.3f}  "
              f"Coherence={best['coherence_score']:.3f}")
        print(f"  Output directory  : {args.output}")
        print(f"{'=' * 70}")
    else:
        print("\nWARNING: No feasible solutions found. Try increasing generations or population size.")


if __name__ == "__main__":
    main()
