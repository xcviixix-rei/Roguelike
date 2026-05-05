# NSGA-II Optimization with PPO Agents — Setup Guide

## Overview

This system uses trained PPO agents to play the roguelike game while NSGA-II optimizes ~40 game parameters for balanced gameplay. Everything runs in Python — no C# runtime needed.

```
┌─────────────────────────────────────────────┐
│           NSGA-II (pymoo)                   │
│  Population of genomes (40 parameters)      │
│  SBX crossover + Polynomial mutation        │
│  3 objectives: Balance, Engagement,         │
│                Coherence                    │
└──────────────────┬──────────────────────────┘
                   │  For each genome:
                   ▼
┌─────────────────────────────────────────────┐
│     Apply genome → modify game data         │
│  (card damage, enemy HP, mana costs, etc.)  │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│    Run N games per agent (4 PPO agents)     │
│    Aggressive / Defensive / Balanced /      │
│    Adaptive                                 │
│                                             │
│    Collect: win/loss, HP%, floor reached,   │
│             deck composition, card picks    │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│         Calculate 3 fitness scores          │
│  Balance:    WR→45%, HP→30%, Floor→10       │
│  Engagement: Card diversity + Build variety │
│  Coherence:  No traps + WR consistency      │
└─────────────────────────────────────────────┘
```

---

## 1. Prerequisites

- **Python 3.10+** (3.11 or 3.12 recommended)
- **pip** (comes with Python)
- A GPU is recommended for faster PPO inference but not required

---

## 2. Installation

### Step 1: Create a virtual environment

```powershell
cd e:\Unity_Projects\Roguelike
python -m venv .venv
.venv\Scripts\activate
```

### Step 2: Install PyTorch

Install PyTorch first (pick the right command for your system from https://pytorch.org/get-started/locally/):

```powershell
# CPU only (works everywhere, recommended — PPO inference is fast on CPU)
pip install torch --index-url https://download.pytorch.org/whl/cpu

# Or CUDA 12.1 (NVIDIA GPU — large 2.4 GB download, use --timeout 300)
pip install torch --index-url https://download.pytorch.org/whl/cu121 --timeout 300
```

### Step 3: Install dependencies

```powershell
pip install -r python_roguelike/requirements.txt
```

This installs:
- `gymnasium` — game environment interface
- `numpy` — array operations
- `pymoo` — NSGA-II multi-objective optimization
- `stable-baselines3` — PPO model loading
- `sb3-contrib` — MaskablePPO (if your models use action masking)

---

## 3. Organize Your PPO Models

Place your 4 trained model `.zip` files in a `models/` folder:

```
Roguelike/
├── models/
│   ├── aggressive.zip       # or whatever your files are named
│   ├── defensive.zip
│   ├── balanced.zip
│   └── adaptive.zip
├── python_roguelike/
│   ├── GameData.json
│   ├── optimization/        # ← the new files
│   │   ├── __init__.py
│   │   ├── hierarchical_genome.py
│   │   ├── hierarchical_applicator.py
│   │   ├── fitness.py
│   │   ├── simulation_stats.py
│   │   ├── simulation_runner.py
│   │   ├── nsga2_problem.py
│   │   └── run_optimization.py
│   └── ...
└── ...
```

> **Note:** The model files are the `.zip` files saved by `model.save("name")` in stable-baselines3. If your training notebooks saved models differently (e.g. as `.pt` files or directories), adjust the paths accordingly.

---

## 4. Run the Optimization

### Basic run

```powershell
python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip
```

### Full options

```powershell
python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip --pop-size 50 --generations 30 --runs-per-genome 20 --seed 42 --output optimization_results --verbose
```

### Quick test run (verify everything works)

```powershell
python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip --pop-size 5 --generations 2 --runs-per-genome 3 --output test_results
```

### If your models DON'T use action masking

```powershell
python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip --no-masking
```

---

## 5. Command-Line Options Reference

| Flag | Default | Description |
|------|---------|-------------|
| `--models` | *required* | Paths to PPO `.zip` model files |
| `--game-data` | `python_roguelike/GameData.json` | Path to base game data |
| `--pop-size` | 50 | Population size per generation |
| `--generations` | 30 | Number of NSGA-II generations |
| `--runs-per-genome` | 20 | Games per genome **per agent** |
| `--seed` | 42 | Random seed for reproducibility |
| `--output` | `optimization_results` | Output directory |
| `--no-masking` | False | Use regular PPO (not MaskablePPO) |
| `--eta-crossover` | 20.0 | SBX distribution index |
| `--eta-mutation` | 20.0 | Polynomial mutation distribution index |
| `--mutation-prob` | 1/n_var | Per-variable mutation probability |
| `--crossover-prob` | 0.9 | Crossover probability |
| `--quiet` | False | Suppress per-evaluation output |

---

## 6. Output Files

After the run completes, the output directory will contain:

```
optimization_results/
├── pareto_front_20260322_143000.json     # All Pareto-optimal solutions
├── best_compromise_20260322_143000.json  # Single best trade-off solution
├── GameData_balanced_20260322_143000.json # Ready-to-use balanced game data
└── run_metadata_20260322_143000.json     # Run configuration & summary
```

### `pareto_front_*.json`
Array of all non-dominated solutions, each containing:
- `balance_score`, `engagement_score`, `coherence_score`
- All 40 genome parameters

### `best_compromise_*.json`
The solution closest to the ideal point (best overall trade-off).

### `GameData_balanced_*.json`
**This is the key output** — a modified `GameData.json` with the best genome applied. You can copy this directly into your C# game:
```powershell
Copy-Item optimization_results\GameData_balanced_*.json src\Roguelike\GameData.json
```

---

## 7. Understanding the Output

### Three Objectives (all 0–1, higher is better)

| Objective | What It Measures | Perfect Score |
|-----------|------------------|--------------|
| **Balance** | Win rate near 45%, victory HP near 30%, avg death floor near 10 | 1.0 |
| **Engagement** | 18+ viable cards, diverse winning decks | 1.0 |
| **Coherence** | No trap cards, win rate between 35%–55% | 1.0 |

### Constraints (hard limits)

| Constraint | Requirement |
|-----------|-------------|
| Win rate | 25% – 65% |
| Viable cards | ≥ 10 (cards with >10% pick rate) |
| Trap cards | ≤ 2 (high pick rate, low win rate) |

---

## 8. Performance Tuning

### Speed vs. Quality

The bottleneck is game simulations. With 4 agents × 20 runs/genome × 50 population:

| Setting | Evaluations/gen | Games/gen | Tradeoff |
|---------|----------------|-----------|----------|
| Quick test | 5 pop × 3 runs × 1 agent = 15 | 15 | Fast but noisy |
| Development | 20 pop × 10 runs × 4 agents = 800 | 800 | Good for testing |
| Production | 50 pop × 20 runs × 4 agents = 4000 | 4000 | Best results |

### Recommended workflow

1. **Quick test**: Verify everything loads and runs
   ```powershell
   python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip --pop-size 5 --generations 2 --runs-per-genome 3 --output test_results
   ```

2. **Dev iteration**: Test with reduced settings
   ```powershell
   python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip --pop-size 20 --generations 10 --runs-per-genome 10 --output dev_results
   ```

3. **Production run**: Full optimization
   ```powershell
   python -m python_roguelike.optimization.run_optimization --models models/aggressive.zip models/defensive.zip models/balanced.zip models/adaptive.zip --pop-size 50 --generations 30 --runs-per-genome 20 --output optimization_results
   ```

---

## 9. Troubleshooting

### "ModuleNotFoundError: No module named 'sb3_contrib'"
Your models might not use action masking. Either:
- Install it: `pip install sb3-contrib`
- Or use `--no-masking` flag

### "ModuleNotFoundError: No module named 'pymoo'"
```powershell
pip install "pymoo>=0.6.0"
```

### "Model file not found"
Check that your model paths are correct. SB3 models are saved as `.zip` files:
```python
# During training:
model.save("models/aggressive")  # creates models/aggressive.zip
```

### "No feasible solutions found"
- Increase `--generations` (try 50+)
- Increase `--pop-size` (try 100)
- Check that your PPO agents actually play reasonably (win some games)

### Models trained with different observation/action spaces
The simulation runner uses `RoguelikeEnv` with 143-dim observations and 71 actions. If your PPO models were trained with a different env wrapper (e.g., the `AdaptiveEnv` from notebooks), you may need to wrap the env similarly. See the `SimulationRunner._run_single()` method.

---

## 10. Architecture Summary

### Files created in `python_roguelike/optimization/`

| File | Purpose | C# Equivalent |
|------|---------|---------------|
| `hierarchical_genome.py` | 40-parameter genome with bounds + vector conversion | `HierarchicalGenome.cs` |
| `hierarchical_applicator.py` | Applies genome multipliers to game data | `HierarchicalApplicator.cs` |
| `fitness.py` | 3-objective fitness evaluation + card diversity analysis | `MultiObjectiveFitness.cs` |
| `simulation_stats.py` | Per-game telemetry data structure | `SimulationStats.cs` |
| `simulation_runner.py` | Runs games with PPO agents, collects stats | `HierarchicalSimulationRunner.cs` |
| `nsga2_problem.py` | pymoo `Problem` class bridging genome ↔ fitness | Custom (replaces `NSGA2Optimizer.cs`) |
| `run_optimization.py` | CLI entry point, saves results | `Program.cs` optimization mode |

### Key differences from C#

1. **PPO agents replace HeuristicPlayerAI** — learned behaviour instead of hand-crafted rules
2. **pymoo handles NSGA-II** instead of custom implementation — same SBX/PM operators, same constraint-domination
3. **Output is a balanced GameData.json** that can be copied back to the C# project
4. **Multi-agent evaluation** — each genome is tested against all 4 agent play styles for robustness
