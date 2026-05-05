# Thesis Outline

**Title:** Optimizing Game Balance in Roguelike Deckbuilders via Multi-Objective Evolutionary Algorithms and Reinforcement Learning  
**Student:** Nguyễn Nhật Phong — 22028272  
**Supervisor:** PGS. TS. Nguyễn Trí Thành  
**Year:** 2026

---

## Introduction

### 1. Motivation
Roguelike Deckbuilders (e.g., *Slay the Spire*, *Monster Train*, *Inscryption*) are a hugely popular sub-genre generating billions in revenue. They are notoriously hard to balance because designers must satisfy three competing goals simultaneously: the game must be *challenging but not punishing*, the card pool must offer *multiple viable strategies*, and the *difficulty curve must scale smoothly* across 15+ floors. Manual playtesting is slow and subjective. This thesis proposes replacing it with automated simulation + evolutionary optimization.

### 2. Problem Statement
Formally states the problem: find game parameters θ* such that the resulting game simultaneously satisfies **Balance** (win rate in [35%, 55%]), **Engagement** (high fraction of viable cards), and **Coherence** (no dominant trap cards, sensible parameter values). Since these three objectives conflict, this is framed as a **multi-objective optimization** over a Pareto front.

### 3. Research Objectives
Five concrete objectives: (1) build the C# game engine, (2) implement a rule-based Deterministic Heuristic Agent, (3) train 4 RL agent personas, (4) implement and compare GA vs. NSGA-II optimizers, (5) analyze and visualize results.

### 4. Scope and Methodology
Four-phase process: literature review → system construction → agent training → optimization + evaluation. Scope covers the full stack from C# engine to Python optimizer.

### 5. Main Contributions
- Open-source C# Roguelike Deckbuilder prototype with data-driven JSON architecture
- Cross-platform C#↔Python bridge via pythonnet (no network overhead)
- 4 trained RL personas covering distinct play styles (win rate 24–34%)
- Empirical comparison of GA vs. NSGA-II — NSGA-II converges 33% faster and wins on all 3 objectives
- Reusable framework applicable to other card/turn-based games

### 6. Thesis Structure
Brief roadmap of Chapters 1–5.

---

## Chapter 1 — Background and Related Work

### 1.1 Roguelike Deckbuilder Games

#### 1.1.1 Roguelike Games
Traces the genre back to *Rogue* (1980). Defines the two core traits: **procedural generation** (random dungeon/item layout each run) and **permadeath** (death resets everything). Covers how modern Roguelikes add resource management and meta-progression that rewards strategic mastery.

#### 1.1.2 Deckbuilding Mechanics
Origins in board games (*Dominion* 2008, *Thunderstone* 2009). Players start with a weak deck and upgrade it over time. Key concept: **card synergies** — combinations that are exponentially stronger together than apart. Players must weigh short-term combat value vs. long-term deck cohesion.

#### 1.1.3 Roguelike Deckbuilders
The merged genre: each run generates a unique map, cards are added to the deck after combat, death resets progress. *Slay the Spire* (2019) is credited with proving the genre's commercial viability and is the main inspiration for this thesis's prototype.

#### 1.1.4 Balancing Challenges
Why balancing is hard: (1) **2ⁿ possible deck subsets** for n cards; (2) **stochastic variance** from RNG card draws and enemy intents; (3) **emergent dominant strategies** designers didn't anticipate; (4) **player skill heterogeneity** — one balance configuration can't satisfy both experts and beginners.

---

### 1.2 Genetic Algorithms

#### 1.2.1 Biological Motivation
GAs abstract biological evolution: a population of candidate solutions (genomes) is evolved via **selection**, **crossover**, and **mutation** toward better fitness. Crossover lets good partial solutions combine; mutation prevents stagnation.

#### 1.2.2 Core Components
Covers all five GA components:
- **Genome** — real-valued vector of game parameter multipliers
- **Fitness function** — weighted sum of Balance + Engagement + Coherence scores (Eq. 1)
- **Tournament selection** — pick best from k random individuals
- **Uniform crossover** — each gene randomly inherited from one parent (Eq. 2)
- **Gaussian mutation** — add random noise to genes with probability p_m (Eq. 3)
- **Elitism** — top individuals carried forward unchanged

---

### 1.3 NSGA-II: Multi-Objective Evolutionary Optimization

#### 1.3.1 Multi-Objective Problem Formulation
Formally defines the problem: maximize F(x) = (f₁, f₂, ..., fₘ) simultaneously. Defines **Pareto Dominance** (x₁ dominates x₂ iff it's at least as good on all objectives and strictly better on one) and **Pareto Optimal Set** (no solution dominates it).

#### 1.3.2 The NSGA-II Algorithm
NSGA-II's three innovations over original NSGA:
1. **Fast non-dominated sorting** (O(MN²)) — partitions population into ranked fronts
2. **Crowding distance** — measures how isolated a solution is in objective space for diversity preservation (Eq. 5)
3. **Elitist selection** — prefers lower rank, breaks ties by crowding distance (Eq. 6)

Also covers SBX crossover and Polynomial Mutation as the preferred operators for real-valued chromosomes.

---

### 1.4 Reinforcement Learning

#### 1.4.1 Markov Decision Processes
Defines the MDP tuple (S, A, P, R, γ). Explains policy π(a|s) and the objective of maximizing expected discounted return J(π) (Eq. 7).

#### 1.4.2 Deep Reinforcement Learning
How DRL handles large state spaces (148-dim in this game) via neural network policies. Defines the Q-function, V-function, and advantage function A(s,a) = Q(s,a) - V(s).

#### 1.4.3 Proximal Policy Optimization (PPO)
PPO's clipped objective (Eq. 8) prevents destructively large policy updates by capping the probability ratio r_t(θ). Full objective (Eq. 9) combines policy loss + value regression + entropy bonus. Widely used for its simplicity and sample efficiency.

#### 1.4.4 Invalid Action Masking
The problem: many actions are illegal at any given game step (e.g., playing a card with insufficient mana). Vanilla PPO wastes training learning the constraints. **MaskablePPO** sets illegal action logits to -∞ before softmax (Eq. 10), guaranteeing valid policy gradients and dramatically accelerating learning in constraint-heavy environments.

#### 1.4.5 Domain Randomization
Originally developed for sim-to-real transfer in robotics. Here repurposed: because NSGA-II will test many different game parameter sets, the RL agents must work across all of them, not just the default. At each environment reset, uniform noise is added to game parameters (Eq. 11) so agents learn *relative* decision-making (e.g., "play highest damage relative to enemy HP") rather than memorizing absolute values.

---

### 1.5 Related Work

#### 1.5.1 Automated Game Balancing
Reviews prior work: Jaffe et al. (hill-climbing for board game balance), Nelson & Mateas (general game design automation), Beukman et al. (GA for CCG card costs). Distinguishes this thesis from the closest prior work (Mendes et al. for Roguelike platformer) by using a more complex genre, learned RL agents instead of scripted bots, and comparing GA vs. NSGA-II.

#### 1.5.2 RL for Card Games
AlphaStar, DQN for *Dominion*, PPO for simplified *Slay the Spire*. Prior work confirms PPO suits card-game action spaces but didn't connect agent training to game balancing — this thesis does.

#### 1.5.3 Procedural Content Generation
Survey of search-based PCG (Togelius et al.), multi-objective PCG (Preuss et al.). This thesis complements PCG by focusing on parameter *tuning* rather than content generation, and using RL agents as the evaluation oracle.

---

## Chapter 2 — System Analysis and Design

### 2.1 Requirements Analysis

#### 2.1.1 Functional Requirements
Six FRs: (FR1) complete C# game engine, (FR2) rule-based DHA baseline agent, (FR3) Gymnasium-compatible RL environment with action masking, (FR4) four MaskablePPO agents, (FR5) GA + NSGA-II optimizers, (FR6) result visualizations.

#### 2.1.2 Non-Functional Requirements
Four NFRs: (NFR1) cross-platform — engine must run on Kaggle Linux; (NFR2) simulation speed ≥ 1,000 runs/second; (NFR3) reproducibility via fixed seeds; (NFR4) extensibility — new content added via JSON only, no code change.

---

### 2.2 System Architecture
Three-layer architecture (with diagram):
- **Core Game Layer** — C# game engine, deterministic given seed + parameters
- **Simulation/Training Layer** — Python bridge exposing game as Gymnasium env; runs mass simulations for optimization
- **Optimization Layer** — Python GA + NSGA-II that mutate parameter vectors and receive fitness back
- **Data Layer** — JSON files defining all entities; separating data from logic lets the optimizer change parameters without recompiling

---

### 2.3 Core Game Engine Design

#### 2.3.1 Game Structure Overview
A run = 15 floors in 3 Acts. The player navigates a branching room map, fights enemies, collects cards and relics. Win by defeating Floor 15 boss; lose when HP hits 0. Details the **map generation** algorithm (guaranteed viable path, weighted room type distribution), **card system** (30+ cards, types: Attack/Skill/Power, star ratings 1–5, mana costs, hand of 5), and **relic system** (20+ passive items with trigger-based effects).

#### 2.3.2 Combat System Design
Turn structure: (1) Player draws 5 cards, plays any affordable cards; (2) Each enemy executes its scheduled action; (3) End-of-turn cleanup. Status effects (Strength, Weakness, Poison, Burn, Vulnerability, Thorns, Metallicize) tick per turn. References class diagram and combat sequence diagram.

---

### 2.4 Deterministic Heuristic Agent Design

#### 2.4.1 Purpose and Role
DHA serves two roles: (1) **playability validation** — confirms the game is winnable before optimization or training begins; (2) **optimization benchmark** — fast (no neural network inference), consistent, reproducible evaluation oracle for GA/NSGA-II runs.

#### 2.4.2 Decision Logic
Three-level priority hierarchy: **(1) Survival first** — if incoming damage exceeds HP, play defensive cards; **(2) Lethal second** — if any enemy can be killed this turn, do it; **(3) Efficient offense** — otherwise play highest damage-per-mana card, prefer multi-target attacks. For map navigation, seeks Elites at ≥60% HP, Rest sites at ≤40% HP, Monster rooms otherwise.

---

### 2.5 Reinforcement Learning Environment Design

#### 2.5.1 Observation Space
148-dimensional vector encoding the full game state: hero HP/block/mana/gold (4), active status effects (7), hand cards with 7 features each (70), deck composition (5), up to 4 enemies with 6 features each (24), equipped relics (10), map context (7), next room choices (14), plus 4 new v2 features (predicted incoming damage, max achievable damage, turn counter, deck attack ratio).

#### 2.5.2 Action Space
68 discrete actions: play card slot 0–9 (10 actions), play card targeting specific enemy 0–3 (40 targeted actions), end turn (1), navigate to map node (15), use health potion (1), pick card reward (1). At each step the engine returns a binary action mask — illegal actions have mask=0 and can't be selected.

#### 2.5.3 Episode Structure
One episode = one complete game run. `reset()` starts a new run (with optional Domain Randomization applied to parameters); `step(action)` advances one decision, returns (s', r, done, info).

---

### 2.6 RL Agent Persona Design

#### 2.6.1 Design Rationale
A single agent would only reveal balance issues relevant to one play style. Four personas give a richer evaluation signal approximating real player diversity. All share the same obs/action space and [512,512] MLP architecture but differ in reward function.

#### 2.6.2 Aggressive Agent
Goal: minimize turns to kill enemies, accept HP loss for faster kills. Reward: large bonus per enemy killed and floor cleared, step penalty to pressure speed, amplified HP-loss penalty below 25% HP as a "non-linear brake" to prevent suicidal play.

#### 2.6.3 Defensive Agent
Goal: survive as long as possible, prioritize healing and block. Reward: asymmetric HP weights (+0.30 for healed HP, -0.15 for damage taken), large bonus for winning with HP intact, nearly zero step penalty so slow play is acceptable. HP-loss capped at 20/step to prevent gradient instability.

#### 2.6.4 Balanced Agent
Goal: equal weight on offense and defense. Reward: symmetric HP weights, small gold reward, **quadratic terminal HP bonus** on victory (incentivizes winning with maximum HP remaining) — directly serves the balancing objective.

#### 2.6.5 Adaptive Agent
Goal: switch between aggressive and defensive dynamically based on current HP — but *without* breaking the stationarity assumption. An earlier version changed the reward formula at runtime based on HP; this caused the value function to fail to converge. **Revised design (v4)**: stationary reward + HP ratio included as observation[0]. The [512,512] network learns *emergent* strategy-switching independently: attack aggressively above 70% HP, switch to block-heavy defense below 30% HP. This emergent behavior is more robust than hand-coded switching.

---

### 2.7 Optimization Layer Design

#### 2.7.1 Data Flow
Optimizer creates a genome → scales the base GameData.json → simulation runner plays N games with the AI agent → statistics are computed → fitness scores feed back to the optimizer. (Diagram included.)

#### 2.7.2 Genome Representations
- **BalanceGenome (for GA)** — ~500 dimensions, one float multiplier per individual card action, card cost, enemy HP, and enemy attack value. Fine-grained but suffers from dimensionality and produces incoherent solutions.
- **HierarchicalGenome (for NSGA-II)** — ~50 dimensions, groups parameters into: global multipliers (5), per-act progression scaling (9), card/enemy category scaling (8), room type distribution (5), miscellaneous (8). Each gene corresponds to a meaningful design concept, enabling sensible mutations like "scale all early-game enemies up 20%."

#### 2.7.3 Fitness Functions
Three sub-objectives (computed separately for NSGA-II, aggregated for GA):
- **Balance Score** — Gaussian scoring around target win rate (45%), target victory HP (30%), target floor-on-death (10)
- **Engagement Score** — fraction of viable cards (pick rate > threshold AND conditional win rate > threshold) plus build diversity entropy
- **Coherence Score** — penalizes trap cards (frequently picked but below-average win rate) and parameter values that deviate far from baseline

---

## Chapter 3 — Implementation

### 3.1 Development Environment
Table of key technology choices: C# 13 / .NET 9 (game engine, 2,500 runs/second), Python 3.10, Stable-Baselines3 2.3.0 + MaskablePPO, PyTorch 2.2.0, pythonnet 3.0.3, Kaggle T4×2 GPUs for training.

### 3.2 Core Game Engine Implementation

#### 3.2.1 Project Structure
Full C# source tree layout: `Roguelike.Core/` (shared .dll with AI, Combat, Map, Data, Optimization modules), `Roguelike/` (console entry point), `Roguelike.Tests/` (xUnit suite).

#### 3.2.2 Game Controller
`GameController.cs` runs the game state machine (MapNavigation → Combat → CardReward → RelicReward → ShopVisit → EventRoom → RestSite). Passes immutable `GameRun` snapshots to agents so they can't mutate live state.

#### 3.2.3 Combat System
Step-by-step breakdown of `CombatManager.ExecuteTurn()`: mana reset → draw 5 cards → start-of-turn relics → agent decisions loop → card validation + execution → enemy actions → damage application (block before HP) → thorns/relic triggers → win/loss check. Status effect management via `ActiveEffect.cs` (intensity + duration, decremented each turn).

#### 3.2.4 Map Generation
DAG of 15 floors with 2–3 branches per floor. Generation proceeds floor 14→1, assigns room types via weighted categorical distribution, applies structural constraints (no 2 consecutive Elites, Rest in each Act, no Shop on Floor 1), then ensures full connectivity.

#### 3.2.5 Data-Driven Design
All game parameters live in `GameData.json`. The optimization layer writes a modified copy and passes it to the game; no recompilation required. Enables the optimizer to treat the entire parameter space as a black box.

### 3.3 Python-C# Integration
Explains **pythonnet** (direct CLR embedding in Python process) vs. the rejected alternative **gRPC** (separate process with socket calls). Benchmark: gRPC latency ~25 ms/call vs. pythonnet <1 µs/call — over 25,000× faster per simulation step. This difference makes population-based optimization tractable.

### 3.4 Gymnasium Environment Wrapper
The `DotNetGameEnv` class wraps the C# engine as a Gymnasium `Env`. `reset()` starts a new game run and optionally applies Domain Randomization. `step(action)` checks the mask, calls the C# engine, converts the game state to a 148-dim numpy array, computes the reward, and returns the tuple. Action masking is exposed via the `action_masks()` method as required by MaskablePPO.

### 3.5 Agent Training Pipeline
Two-phase training: **Phase 1** (0–4M steps) — standard game parameters, no DR, agents learn the base game; **Phase 2** (4M–8M steps) — DR enabled with a curriculum ramp (noise starts at 5%, increases to 15%) to avoid a sudden performance drop. MaskablePPO hyperparameter table: learning rate 3×10⁻⁴, n_steps 2048, batch_size 512, n_epochs 10, gamma 0.99, ent_coef 0.10, clip_range 0.20, network [512,512].

### 3.6 GA Implementation
Covers `GeneticAlgorithm.cs`: tournament selection, uniform crossover, Gaussian mutation, 10% elitism, random immigrant diversity, parallelized fitness evaluation across 8 cores.

### 3.7 NSGA-II Implementation
Covers `NSGA2Optimizer.cs`: fast non-dominated sorting (`FastNonDominatedSort` in C#), crowding distance calculation, combined parent+offspring selection (rank then crowding), SBX crossover, polynomial mutation. Fitness evaluation dispatches all 100 genomes in parallel.

---

## Chapter 4 — Experiments and Evaluation

### 4.1 Experimental Setup

#### 4.1.1 Hardware Configuration
AMD Ryzen 7 6800H (local, 8 cores), 24 GB DDR5 4800 MHz RAM; NVIDIA Tesla T4×2 16 GB VRAM on Kaggle for GPU training.

#### 4.1.2 Algorithm Parameters
Side-by-side table — GA: pop=100, 30 generations, ~500-dim genome, 200 sims/genome, tournament(k=3), uniform crossover p=0.8, Gaussian mutation p=0.02, 10% elitism, 73 min total. NSGA-II: pop=100, 30 generations, ~50-dim genome, 50–200 sims/genome (adaptive), binary tournament, SBX(η=20), polynomial mutation p=0.05, Pareto front elitism, 49 min total.

#### 4.1.3 Target Metrics
Win rate = 45%, victory HP = 30% remaining, floor-on-death = Floor 10 (player reaches ≥2/3 of game before losing).

---

### 4.2 RL Agent Training Results

#### 4.2.1 Final Win Rates
After 8M steps: **Aggressive 24.5%** (fastest wins, 412 turns avg, only 18.3% HP on win), **Defensive 31.0%** (most HP on win 56.2%, slowest 687 turns), **Balanced 29.5%** (moderate everywhere), **Adaptive 34.0%** (highest win rate, 29.8% HP — emergent strategy switching proved most effective). Average win rate ~30% confirms the game is challenging but learnable.

#### 4.2.2 Behavioral Differentiation
Analyzes what each agent's stats reveal: Aggressive sometimes dies to easily survivable enemies; Defensive occasionally over-prolongs combats; Balanced is a reasonable "average skilled player" proxy; Adaptive independently discovered the heuristic "attack when safe, defend when endangered" purely from reward signal.

#### 4.2.3 Training Convergence
Phase 1 converges stably, all agents 25–30% win rate by 3M steps. Abrupt DR introduction caused 5–8% temporary drop (reduced to 2–3% with curriculum ramp). Explained variance >0.85 after 1M steps indicates stable learning. Entropy collapse was prevented by raising ent_coef from 0.05 to 0.10 in early experiments.

---

### 4.3 Game Optimization Results

#### 4.3.1 Single-Objective GA Results
Fitness evolution over 30 generations: improved from **1.45 → 32.66** (+2,150%). Win rate moved from 36.0% → 38.3%; floor-on-death improved toward target. Convergence largely complete by generation 20; later generations show diminishing returns. Card viability analysis shows increased variety but some trap cards remain.

#### 4.3.2 NSGA-II Results
All three objectives improved substantially: **Balance 0.754 → 0.960**, **Engagement 0.655 → 0.900**, **Coherence 0.853 → 1.000** (no trap cards). Viable card count increased from 12 → 14 out of 30. Pareto front visualization (3D plot + three 2D projections) shows a well-spread front confirming the algorithm didn't collapse to a single solution.

#### 4.3.3 Comparative Analysis
NSGA-II beats GA on all three objectives simultaneously while running 33% faster (49 vs. 73 min), attributed to: (1) smaller 50-dim genome → lower-dimensional search; (2) hierarchical encoding produces semantically coherent mutations; (3) Pareto-based selection avoids the weight-sensitivity problem of the GA's weighted-sum objective.

#### 4.3.4 DHA vs. RL Agent Comparison
Final optimized configuration evaluated with both DHA and all 4 RL agents. The optimized game is fairer to learned agents than the baseline, with RL win rates improving by +3–5 percentage points post-optimization, confirming the optimizer found configurations that are genuinely better for diverse play styles.

---

## Chapter 5 — Conclusion

### 5.1 Summary of Achievements
All 5 objectives met: C# engine with 30+ cards / 20+ relics / 2,500 runs/sec ✓; DHA (survival > lethal > optimal) baseline ✓; 4 RL personas trained (win rate 24–34%) ✓; GA improved fitness +2,150% in 73 min ✓; NSGA-II achieved near-perfect Coherence (1.0), 0.960 Balance, 0.900 Engagement in 49 min ✓. NSGA-II with hierarchical genome identified as the clear winner.

### 5.2 Scientific and Practical Contributions
**Scientific:** (1) First end-to-end NSGA-II instantiation for Deckbuilder balancing with formalized Balance/Engagement/Coherence objectives; (2) Hierarchical genome design principle applicable to any JSON-parameterized game; (3) Multi-behavioral RL ensemble for richer game quality signal; (4) Empirical pythonnet vs. gRPC benchmark (25,000× latency difference). **Practical:** Open-source C#/.NET 9 engine; Kaggle-runnable Python training pipeline; game-engine-agnostic optimizer module; practical design guideline — target 30–35% RL win rate for human-accessible balance.

### 5.3 Limitations
- **Agent representativeness**: DHA and RL agents are proxies for humans, not humans. Optimized configs are necessary but not sufficient for human-perceived balance — a human playtest study is still needed.
- **Fixed hierarchy**: The 50-dim grouping was manually designed; if two "similar" parameters actually interact antagonistically, the NSGA-II may learn misleading patterns.
- **Content scope**: 30 cards / 15 floors is sufficient for research but far smaller than commercial games (*Slay the Spire* has 75+ cards/character). Scalability is unproven.
- **Single hero class**: Multi-character balance (equal win rates across characters) was not addressed.

### 5.4 Future Work
1. **RL-integrated optimization** — use RL agents (or a surrogate trained on DHA+RL pairs) directly inside the optimization loop.
2. **NSGA-III / MOEA/D** — for 4+ objectives where NSGA-II's crowding distance becomes less effective.
3. **Meta-learning for hierarchy discovery** — automatically cluster parameters by correlation to replace manual grouping.
4. **Human playtest integration** — inverse RL on recorded human sessions to learn a reward function targeting actual human fun.
5. **Expanded game content** — multiple hero classes, puzzle floors, larger card pool to stress-test scalability.
6. **Designer tool** — interactive GUI wrapping the optimizer for non-programmer game designers.
