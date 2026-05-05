"""
nsga2_problem.py  –  pymoo Problem definition for roguelike balance optimisation.

Wraps the simulation runner and fitness evaluator into a pymoo-compatible
multi-objective problem so that pymoo's NSGA-II can drive the optimisation.
"""

import time
from typing import List, Optional

import numpy as np
from pymoo.core.problem import Problem

from .hierarchical_genome import HierarchicalGenome
from .fitness import MultiObjectiveEvaluator, FitnessResult
from .simulation_runner import SimulationRunner, PPOAgent


class RoguelikeBalanceProblem(Problem):
    """
    3-objective minimisation problem for pymoo.

    pymoo minimises by convention, so we negate the three scores
    (Balance, Engagement, Coherence) which we want to *maximise*.

    Decision variables  : 40 continuous floats (genome parameters)
    Objectives          : 3 (negated Balance, Engagement, Coherence)
    Constraints         : 3 inequality constraints (win rate, viability, traps)
    """

    def __init__(
        self,
        runner: SimulationRunner,
        evaluator: Optional[MultiObjectiveEvaluator] = None,
        runs_per_genome: int = 30,
        base_seed: int = 42,
        verbose: bool = True,
    ):
        self.runner = runner
        self.evaluator = evaluator or MultiObjectiveEvaluator()
        self.runs_per_genome = runs_per_genome
        self.verbose = verbose
        self._eval_count = 0
        self._seed_rng = np.random.RandomState(base_seed)

        xl, xu = HierarchicalGenome.get_bounds()
        n_var = HierarchicalGenome.N_CONTINUOUS

        super().__init__(
            n_var=n_var,
            n_obj=3,
            n_ieq_constr=3,
            xl=xl,
            xu=xu,
            type_var=np.float64,
        )

    def _evaluate(self, X, out, *args, **kwargs):
        """
        Called by pymoo for each generation.
        X shape: (pop_size, n_var)
        """
        pop_size = X.shape[0]
        F = np.zeros((pop_size, 3))
        G = np.zeros((pop_size, 3))

        for i in range(pop_size):
            genome = HierarchicalGenome.from_vector(X[i])
            fitness = self._evaluate_genome(genome, i)

            F[i, 0] = -fitness.balance_score
            F[i, 1] = -fitness.engagement_score
            F[i, 2] = -fitness.coherence_score

            G[i, 0] = max(
                self.evaluator.min_acceptable_win_rate - fitness.win_rate,
                fitness.win_rate - self.evaluator.max_acceptable_win_rate,
            )
            G[i, 1] = self.evaluator.min_viable_cards - fitness.viable_cards
            G[i, 2] = fitness.trap_cards - self.evaluator.max_trap_cards

        out["F"] = F
        out["G"] = G

    def _evaluate_genome(self, genome: HierarchicalGenome, idx: int) -> FitnessResult:
        self._eval_count += 1
        t0 = time.time()

        eval_seed = int(self._seed_rng.randint(0, 2**31))
        results = self.runner.run_batch(
            genome,
            n_runs=self.runs_per_genome,
            base_seed=eval_seed,
        )
        fitness = self.evaluator.evaluate(results)

        if self.verbose:
            elapsed = time.time() - t0
            tag = "OK" if fitness.is_feasible else "!!"
            print(
                f"  [{tag}] Eval #{self._eval_count:>4d}  "
                f"Bal={fitness.balance_score:.3f}  "
                f"Eng={fitness.engagement_score:.3f}  "
                f"Coh={fitness.coherence_score:.3f}  "
                f"WR={fitness.win_rate:.1%}  "
                f"Cards={fitness.viable_cards}/{fitness.unique_cards_acquired}  "
                f"Traps={fitness.trap_cards}  "
                f"({elapsed:.1f}s)"
            )

        return fitness
