"""
NSGA-II Game Balance Optimization using trained RL agents.
Mirrors the C# HierarchicalGenome + NSGA2Optimizer but runs entirely in Python,
using the 4 trained MaskablePPO agents as fitness evaluators.
"""
import copy, json, math, os, random, sys, time
from dataclasses import dataclass, field
from typing import List, Dict, Optional, Tuple

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from sb3_contrib import MaskablePPO
from python_roguelike.env.roguelike_env import RoguelikeEnv


# ─── Config ───────────────────────────────────────────────────────────
POP_SIZE        = 30        # population per generation
N_GENERATIONS   = 15        # total generations
SIMS_PER_AGENT  = 10        # episodes per agent per genome
N_AGENTS        = 4         # aggressive, balanced, defensive, adaptive
TARGET_WR       = 0.45      # ideal win rate
TARGET_HP_PCT   = 0.35      # ideal HP% on win
TARGET_FLOOR    = 10.0      # ideal avg floor on death
MODELS_DIR      = os.path.join(os.path.dirname(__file__), 'models')
JSON_PATH       = os.path.join(os.path.dirname(__file__), '..', 'python_roguelike', 'GameData.json')
LOG_DIR         = os.path.join(os.path.dirname(__file__), 'ga_results')

AGENT_FILES = {
    'aggressive': 'aggressive_final.zip',
    'balanced':   'balanced_final.zip',
    'defensive':  'defensive_final.zip',
    'adaptive':   'adaptive_final.zip',
}


# ─── Genome ───────────────────────────────────────────────────────────
@dataclass
class HierarchicalGenome:
    """Python mirror of the C# HierarchicalGenome (~35 genes)."""
    # Global multipliers
    global_damage:  float = 1.0
    global_health:  float = 1.0
    global_block:   float = 1.0
    global_mana:    float = 1.0

    # Progression scaling (early / mid / late)
    early_dmg:  float = 1.0;  mid_dmg:  float = 1.0;  late_dmg:  float = 1.0
    early_hp:   float = 1.0;  mid_hp:   float = 1.0;  late_hp:   float = 1.0
    early_blk:  float = 1.0;  mid_blk:  float = 1.0;  late_blk:  float = 1.0

    # Enemy star scalars (★1-5)
    enemy_star: Dict[int, float] = field(default_factory=lambda: {i: 1.0 for i in range(1, 6)})
    # Card star scalars (★1-5)
    card_star:  Dict[int, float] = field(default_factory=lambda: {i: 1.0 for i in range(1, 6)})

    # Hero params
    hero_hp_scalar:   float = 1.0
    rest_heal_scalar: float = 1.0

    # Difficulty progression rate
    diff_rate: float = 0.03

    def to_vector(self) -> np.ndarray:
        vals = [self.global_damage, self.global_health, self.global_block, self.global_mana,
                self.early_dmg, self.mid_dmg, self.late_dmg,
                self.early_hp, self.mid_hp, self.late_hp,
                self.early_blk, self.mid_blk, self.late_blk]
        vals += [self.enemy_star[i] for i in range(1, 6)]
        vals += [self.card_star[i] for i in range(1, 6)]
        vals += [self.hero_hp_scalar, self.rest_heal_scalar, self.diff_rate]
        return np.array(vals, dtype=np.float32)

    @staticmethod
    def from_vector(v: np.ndarray) -> 'HierarchicalGenome':
        g = HierarchicalGenome()
        g.global_damage, g.global_health, g.global_block, g.global_mana = v[0], v[1], v[2], v[3]
        g.early_dmg, g.mid_dmg, g.late_dmg = v[4], v[5], v[6]
        g.early_hp, g.mid_hp, g.late_hp = v[7], v[8], v[9]
        g.early_blk, g.mid_blk, g.late_blk = v[10], v[11], v[12]
        for i in range(5): g.enemy_star[i+1] = v[13+i]
        for i in range(5): g.card_star[i+1] = v[18+i]
        g.hero_hp_scalar, g.rest_heal_scalar, g.diff_rate = v[23], v[24], v[25]
        return g

    @staticmethod
    def random(rng: random.Random) -> 'HierarchicalGenome':
        g = HierarchicalGenome()
        g.global_damage  = rng.uniform(0.7, 1.3)
        g.global_health  = rng.uniform(0.7, 1.3)
        g.global_block   = rng.uniform(0.7, 1.3)
        g.global_mana    = rng.uniform(0.85, 1.15)
        g.early_dmg = rng.uniform(0.8, 1.1); g.mid_dmg = rng.uniform(1.0, 1.3); g.late_dmg = rng.uniform(1.1, 1.5)
        g.early_hp  = rng.uniform(0.8, 1.1); g.mid_hp  = rng.uniform(1.0, 1.3); g.late_hp  = rng.uniform(1.1, 1.5)
        g.early_blk = rng.uniform(0.9, 1.1); g.mid_blk = rng.uniform(1.0, 1.2); g.late_blk = rng.uniform(1.0, 1.2)
        for s in range(1, 6): g.enemy_star[s] = rng.uniform(0.85, 1.15)
        for s in range(1, 6): g.card_star[s]  = rng.uniform(0.85, 1.15)
        g.hero_hp_scalar   = rng.uniform(0.7, 1.3)
        g.rest_heal_scalar = rng.uniform(0.6, 1.4)
        g.diff_rate        = rng.uniform(0.0, 0.06)
        return g

    BOUNDS_LO = np.array([0.7,0.7,0.7,0.85, 0.8,1.0,1.1, 0.8,1.0,1.1, 0.9,1.0,1.0,
                          0.85,0.85,0.85,0.85,0.85, 0.85,0.85,0.85,0.85,0.85, 0.7,0.6,0.0], dtype=np.float32)
    BOUNDS_HI = np.array([1.3,1.3,1.3,1.15, 1.1,1.3,1.5, 1.1,1.3,1.5, 1.1,1.2,1.2,
                          1.15,1.15,1.15,1.15,1.15, 1.15,1.15,1.15,1.15,1.15, 1.3,1.4,0.06], dtype=np.float32)
    N_GENES = 26


# ─── Fitness ──────────────────────────────────────────────────────────
@dataclass
class MultiObjectiveFitness:
    balance:    float = 0.0
    engagement: float = 0.0
    coherence:  float = 0.0
    rank:       int   = 0
    crowding:   float = 0.0
    evaluated:  bool  = False
    # Raw stats for logging
    win_rates:  Dict[str, float] = field(default_factory=dict)
    avg_hp_pct: float = 0.0
    avg_floor:  float = 0.0

    def dominates(self, other: 'MultiObjectiveFitness') -> bool:
        s = [self.balance, self.engagement, self.coherence]
        o = [other.balance, other.engagement, other.coherence]
        at_least_one = False
        for si, oi in zip(s, o):
            if si < oi: return False
            if si > oi: at_least_one = True
        return at_least_one


@dataclass
class Individual:
    genome:  HierarchicalGenome
    fitness: MultiObjectiveFitness = field(default_factory=MultiObjectiveFitness)


# ─── Genome Applicator ────────────────────────────────────────────────
def apply_genome_to_gamedata(base_data: dict, genome: HierarchicalGenome) -> dict:
    """Apply genome scalars to a deep copy of GameData.json dict."""
    data = copy.deepcopy(base_data)

    # --- Cards ---
    for card in data.get("Cards", []):
        star = card.get("StarRating", 1)
        star_s = genome.card_star.get(star, 1.0)
        for action in card.get("Actions", []):
            atype = action.get("Type", "")
            if atype == "DealDamage":
                base_val = action["Value"]
                scalar = genome.global_damage * star_s
                action["Value"] = max(1, round(base_val * scalar))
            elif atype == "GainBlock":
                base_val = action["Value"]
                scalar = genome.global_block * star_s
                action["Value"] = max(1, round(base_val * scalar))
        if "ManaCost" in card:
            card["ManaCost"] = max(0, round(card["ManaCost"] * genome.global_mana))

    # --- Enemies ---
    for enemy in data.get("Enemies", []):
        star = enemy.get("StarRating", 1)
        star_s = genome.enemy_star.get(star, 1.0)
        # HP scaling
        base_hp = enemy.get("StartingHealth", 50)
        hp_scalar = genome.global_health * star_s
        enemy["StartingHealth"] = max(1, round(base_hp * hp_scalar))
        # Action scaling
        for action_set in enemy.get("ActionSet", []):
            action = action_set.get("Action", {})
            atype = action.get("Type", "")
            if atype == "DealDamage":
                base_val = action.get("Value", 0)
                scalar = genome.global_damage * star_s
                action["Value"] = max(1, round(base_val * scalar))
            elif atype == "GainBlock":
                base_val = action.get("Value", 0)
                scalar = genome.global_block * star_s
                action["Value"] = max(1, round(base_val * scalar))

    # --- Hero ---
    hero = data.get("Hero", {})
    if "StartingHealth" in hero:
        hero["StartingHealth"] = max(1, round(hero["StartingHealth"] * genome.hero_hp_scalar))

    return data


# ─── Evaluator ────────────────────────────────────────────────────────
def evaluate_genome(genome: HierarchicalGenome, models: Dict[str, MaskablePPO],
                    base_data: dict, n_episodes: int = SIMS_PER_AGENT) -> MultiObjectiveFitness:
    """Run all 4 RL agents on a genome-modified game, compute fitness."""
    modified_data = apply_genome_to_gamedata(base_data, genome)

    # Write to temp JSON
    tmp_path = os.path.join(LOG_DIR, '_tmp_gamedata.json')
    with open(tmp_path, 'w', encoding='utf-8') as f:
        json.dump(modified_data, f)

    all_wins, all_hp_pcts, all_floors = [], [], []
    agent_wrs = {}

    for agent_name, model in models.items():
        env = RoguelikeEnv(json_path=tmp_path)
        wins = 0
        for ep in range(n_episodes):
            obs, _ = env.reset()
            done = False
            while not done:
                masks = env.action_masks()
                action, _ = model.predict(obs, deterministic=True, action_masks=masks)
                obs, _, terminated, truncated, info = env.step(int(action))
                done = terminated or truncated

            hp = info.get('hp', 0)
            max_hp = info.get('max_hp', 1)
            floor = info.get('floor', 0)

            if hp > 0:
                wins += 1
                all_hp_pcts.append(hp / max_hp)
            else:
                all_floors.append(floor)

        env.close()
        wr = wins / n_episodes
        agent_wrs[agent_name] = wr
        all_wins.append(wr)

    # --- Compute 3 objectives ---
    avg_wr = np.mean(all_wins)
    avg_hp = np.mean(all_hp_pcts) if all_hp_pcts else 0.0
    avg_floor = np.mean(all_floors) if all_floors else 15.0

    # Balance: Gaussian distance to targets
    wr_score  = math.exp(-10 * (avg_wr - TARGET_WR)**2)
    hp_score  = math.exp(-8 * (avg_hp - TARGET_HP_PCT)**2)
    fl_score  = math.exp(-0.05 * (avg_floor - TARGET_FLOOR)**2)
    balance   = (wr_score + hp_score + fl_score) / 3.0

    # Engagement: variance across agent win rates (lower = more balanced across styles)
    wr_std = np.std(all_wins)
    engagement = max(0, 1.0 - wr_std * 5.0)  # penalize large spread

    # Coherence: how close genome scalars are to 1.0 (natural game feel)
    vec = genome.to_vector()
    deviation = np.mean((vec[:23] - 1.0)**2)  # skip diff_rate
    coherence = max(0, 1.0 - deviation * 10.0)

    return MultiObjectiveFitness(
        balance=float(balance), engagement=float(engagement), coherence=float(coherence),
        evaluated=True,
        win_rates=agent_wrs, avg_hp_pct=float(avg_hp), avg_floor=float(avg_floor)
    )


# ─── NSGA-II ──────────────────────────────────────────────────────────
def fast_non_dominated_sort(pop: List[Individual]) -> List[List[int]]:
    n = len(pop)
    dom_count = [0] * n
    dom_set = [[] for _ in range(n)]
    fronts = [[]]

    for i in range(n):
        for j in range(n):
            if i == j: continue
            if pop[i].fitness.dominates(pop[j].fitness):
                dom_set[i].append(j)
            elif pop[j].fitness.dominates(pop[i].fitness):
                dom_count[i] += 1
        if dom_count[i] == 0:
            pop[i].fitness.rank = 0
            fronts[0].append(i)

    k = 0
    while fronts[k]:
        next_front = []
        for i in fronts[k]:
            for j in dom_set[i]:
                dom_count[j] -= 1
                if dom_count[j] == 0:
                    pop[j].fitness.rank = k + 1
                    next_front.append(j)
        k += 1
        fronts.append(next_front)

    return [f for f in fronts if f]


def crowding_distance(pop: List[Individual], front: List[int]):
    n = len(front)
    if n <= 2:
        for i in front:
            pop[i].fitness.crowding = float('inf')
        return
    for i in front:
        pop[i].fitness.crowding = 0.0

    for obj_fn in [lambda f: f.balance, lambda f: f.engagement, lambda f: f.coherence]:
        sorted_idx = sorted(front, key=lambda i: obj_fn(pop[i].fitness))
        pop[sorted_idx[0]].fitness.crowding = float('inf')
        pop[sorted_idx[-1]].fitness.crowding = float('inf')
        obj_range = obj_fn(pop[sorted_idx[-1]].fitness) - obj_fn(pop[sorted_idx[0]].fitness)
        if obj_range < 1e-9: continue
        for k in range(1, n-1):
            dist = (obj_fn(pop[sorted_idx[k+1]].fitness) - obj_fn(pop[sorted_idx[k-1]].fitness)) / obj_range
            pop[sorted_idx[k]].fitness.crowding += dist


def tournament_select(pop: List[Individual], rng: random.Random) -> Individual:
    i, j = rng.sample(range(len(pop)), 2)
    a, b = pop[i], pop[j]
    if a.fitness.rank < b.fitness.rank: return a
    if b.fitness.rank < a.fitness.rank: return b
    return a if a.fitness.crowding > b.fitness.crowding else b


def sbx_crossover(p1: np.ndarray, p2: np.ndarray, rng: random.Random, eta: float = 20.0) -> Tuple[np.ndarray, np.ndarray]:
    c1, c2 = p1.copy(), p2.copy()
    for i in range(len(p1)):
        if rng.random() > 0.5: continue
        if abs(p1[i] - p2[i]) < 1e-14: continue
        u = rng.random()
        if u <= 0.5:
            beta = (2*u)**(1/(eta+1))
        else:
            beta = (1/(2*(1-u)))**(1/(eta+1))
        c1[i] = 0.5*((1+beta)*p1[i] + (1-beta)*p2[i])
        c2[i] = 0.5*((1-beta)*p1[i] + (1+beta)*p2[i])
    c1 = np.clip(c1, HierarchicalGenome.BOUNDS_LO, HierarchicalGenome.BOUNDS_HI)
    c2 = np.clip(c2, HierarchicalGenome.BOUNDS_LO, HierarchicalGenome.BOUNDS_HI)
    return c1, c2


def polynomial_mutation(vec: np.ndarray, rng: random.Random, rate: float = 0.05, eta: float = 20.0) -> np.ndarray:
    result = vec.copy()
    for i in range(len(vec)):
        if rng.random() > rate: continue
        u = rng.random()
        lo, hi = HierarchicalGenome.BOUNDS_LO[i], HierarchicalGenome.BOUNDS_HI[i]
        delta = hi - lo
        if delta < 1e-14: continue
        if u < 0.5:
            delta_q = (2*u)**(1/(eta+1)) - 1
        else:
            delta_q = 1 - (2*(1-u))**(1/(eta+1))
        result[i] = np.clip(vec[i] + delta_q * delta, lo, hi)
    return result


# ─── Main ─────────────────────────────────────────────────────────────
def main():
    os.makedirs(LOG_DIR, exist_ok=True)
    rng = random.Random(42)

    # Load base game data
    with open(JSON_PATH, 'r', encoding='utf-8') as f:
        base_data = json.load(f)

    # Load RL models
    print("Loading RL agent models...")
    models = {}
    for name, fname in AGENT_FILES.items():
        path = os.path.join(MODELS_DIR, fname)
        print(f"  {name}: {path}")
        models[name] = MaskablePPO.load(path)
    print(f"Loaded {len(models)} agents.\n")

    # Initialize population
    print(f"Initializing population of {POP_SIZE}...")
    population: List[Individual] = []
    for _ in range(POP_SIZE):
        g = HierarchicalGenome.random(rng)
        population.append(Individual(genome=g))

    # Add baseline genome (all 1.0)
    population[0] = Individual(genome=HierarchicalGenome())

    # Evolution log
    log_lines = ["gen,best_balance,best_engagement,best_coherence,avg_wr,best_wr_spread,time_sec"]

    for gen in range(N_GENERATIONS):
        gen_start = time.time()
        print(f"\n{'='*60}")
        print(f"Generation {gen+1}/{N_GENERATIONS}  (pop={len(population)})")
        print(f"{'='*60}")

        # Evaluate
        for idx, ind in enumerate(population):
            if not ind.fitness.evaluated:
                t0 = time.time()
                ind.fitness = evaluate_genome(ind.genome, models, base_data, SIMS_PER_AGENT)
                dt = time.time() - t0
                wrs = ', '.join(f"{k}={v:.0%}" for k, v in ind.fitness.win_rates.items())
                print(f"  [{idx+1:2d}/{len(population)}] B={ind.fitness.balance:.3f} "
                      f"E={ind.fitness.engagement:.3f} C={ind.fitness.coherence:.3f} "
                      f"WR=[{wrs}] ({dt:.1f}s)")

        # Non-dominated sort
        fronts = fast_non_dominated_sort(population)
        for front in fronts:
            crowding_distance(population, front)

        # Log best
        front0 = [population[i] for i in fronts[0]]
        best = max(front0, key=lambda x: x.fitness.balance + x.fitness.engagement + x.fitness.coherence)
        gen_time = time.time() - gen_start
        avg_wr = np.mean([np.mean(list(ind.fitness.win_rates.values())) for ind in front0])
        wr_spreads = [np.std(list(ind.fitness.win_rates.values())) for ind in front0]
        best_spread = min(wr_spreads) if wr_spreads else 0

        print(f"\n  Pareto Front: {len(fronts[0])} solutions")
        print(f"  Best: B={best.fitness.balance:.3f} E={best.fitness.engagement:.3f} "
              f"C={best.fitness.coherence:.3f}")
        print(f"  Avg WR on front: {avg_wr:.1%}  |  Time: {gen_time:.0f}s")

        log_lines.append(f"{gen+1},{best.fitness.balance:.4f},{best.fitness.engagement:.4f},"
                         f"{best.fitness.coherence:.4f},{avg_wr:.4f},{best_spread:.4f},{gen_time:.1f}")

        # Save log after each generation
        with open(os.path.join(LOG_DIR, 'evolution_log.csv'), 'w') as f:
            f.write('\n'.join(log_lines))

        if gen == N_GENERATIONS - 1:
            break  # don't create offspring on last gen

        # Create offspring
        offspring: List[Individual] = []
        while len(offspring) < POP_SIZE:
            p1 = tournament_select(population, rng)
            p2 = tournament_select(population, rng)
            v1, v2 = sbx_crossover(p1.genome.to_vector(), p2.genome.to_vector(), rng)
            v1 = polynomial_mutation(v1, rng)
            v2 = polynomial_mutation(v2, rng)
            offspring.append(Individual(genome=HierarchicalGenome.from_vector(v1)))
            offspring.append(Individual(genome=HierarchicalGenome.from_vector(v2)))
        offspring = offspring[:POP_SIZE]

        # Evaluate offspring before combining
        print(f"\n  Evaluating {len(offspring)} offspring...")
        for idx, ind in enumerate(offspring):
            t0 = time.time()
            ind.fitness = evaluate_genome(ind.genome, models, base_data, SIMS_PER_AGENT)
            dt = time.time() - t0
            wrs = ', '.join(f"{k}={v:.0%}" for k, v in ind.fitness.win_rates.items())
            print(f"  [offspring {idx+1:2d}/{len(offspring)}] B={ind.fitness.balance:.3f} "
                  f"E={ind.fitness.engagement:.3f} C={ind.fitness.coherence:.3f} "
                  f"WR=[{wrs}] ({dt:.1f}s)")

        # Combine parent + offspring, select next generation
        combined = population + offspring
        combined_fronts = fast_non_dominated_sort(combined)
        for front in combined_fronts:
            crowding_distance(combined, front)

        # Select top POP_SIZE by rank then crowding
        next_pop = []
        for front in combined_fronts:
            if len(next_pop) + len(front) <= POP_SIZE:
                next_pop.extend(front)
            else:
                remaining = POP_SIZE - len(next_pop)
                sorted_front = sorted(front, key=lambda i: combined[i].fitness.crowding, reverse=True)
                next_pop.extend(sorted_front[:remaining])
                break

        population = [combined[i] for i in next_pop]

    # ─── Final Report ─────────────────────────────────────────────
    print(f"\n{'='*60}")
    print("FINAL RESULTS")
    print(f"{'='*60}")

    fronts = fast_non_dominated_sort(population)
    front0 = [population[i] for i in fronts[0]]
    best = max(front0, key=lambda x: x.fitness.balance + x.fitness.engagement + x.fitness.coherence)

    print(f"\nPareto Front Size: {len(front0)}")
    print(f"Best Compromise Solution:")
    print(f"  Balance:    {best.fitness.balance:.4f}")
    print(f"  Engagement: {best.fitness.engagement:.4f}")
    print(f"  Coherence:  {best.fitness.coherence:.4f}")
    print(f"  Win Rates:  {best.fitness.win_rates}")
    print(f"  Avg HP%:    {best.fitness.avg_hp_pct:.1%}")
    print(f"  Avg Floor:  {best.fitness.avg_floor:.1f}")

    # Save best genome
    best_vec = [float(x) for x in best.genome.to_vector().tolist()]
    result = {
        'genome_vector': best_vec,
        'fitness': {
            'balance': float(best.fitness.balance),
            'engagement': float(best.fitness.engagement),
            'coherence': float(best.fitness.coherence),
            'win_rates': {k: float(v) for k, v in best.fitness.win_rates.items()},
            'avg_hp_pct': float(best.fitness.avg_hp_pct),
            'avg_floor': float(best.fitness.avg_floor),
        },
        'pareto_front': [{
            'balance': float(population[i].fitness.balance),
            'engagement': float(population[i].fitness.engagement),
            'coherence': float(population[i].fitness.coherence),
            'win_rates': {k: float(v) for k, v in population[i].fitness.win_rates.items()},
        } for i in fronts[0]]
    }
    with open(os.path.join(LOG_DIR, 'best_result.json'), 'w') as f:
        json.dump(result, f, indent=2)

    # Save the game data with best genome applied
    best_data = apply_genome_to_gamedata(base_data, best.genome)
    with open(os.path.join(LOG_DIR, 'best_GameData.json'), 'w', encoding='utf-8') as f:
        json.dump(best_data, f, indent=2, ensure_ascii=False)

    print(f"\nResults saved to {LOG_DIR}/")
    print("Done!")


if __name__ == '__main__':
    main()
