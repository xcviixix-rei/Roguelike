using System;
using System.Collections.Generic;
using System.Linq;
using Roguelike.Data;

namespace Roguelike.Optimization
{


    public class Individual
    {
        public HierarchicalGenome Genome { get; set; }
        public MultiObjectiveFitness Fitness { get; set; }

        public Individual(HierarchicalGenome genome)
        {
            Genome = genome;
        }
    }


    public class NSGA2Optimizer
    {
        private readonly Random _rng;
        private readonly CardPool _cardPool;
        private readonly EnemyPool _enemyPool;

        public int PopulationSize { get; set; } = 100;
        public float MutationRate { get; set; } = 0.15f;
        public float MutationStrength { get; set; } = 0.10f;

        public NSGA2Optimizer(Random rng, CardPool cardPool = null, EnemyPool enemyPool = null)
        {
            _rng = rng;
            _cardPool = cardPool;
            _enemyPool = enemyPool;
        }


        public List<Individual> InitializePopulation()
        {
            var population = new List<Individual>();
            for (int i = 0; i < PopulationSize; i++)
            {
                var genome = new HierarchicalGenome();
                genome.Randomize(_rng);
                population.Add(new Individual(genome));
            }
            return population;
        }


        public List<Individual> EvolveGeneration(List<Individual> population)
        {
            var offspring = CreateOffspring(population);

            return offspring;
        }


        public List<List<Individual>> FastNonDominatedSort(List<Individual> population)
        {
            var fronts = new List<List<Individual>>();
            var dominationCount = new Dictionary<Individual, int>();
            var dominatedSolutions = new Dictionary<Individual, List<Individual>>();


            foreach (var p in population)
            {
                dominationCount[p] = 0;
                dominatedSolutions[p] = new List<Individual>();
            }


            var firstFront = new List<Individual>();

            foreach (var p in population)
            {
                foreach (var q in population)
                {
                    if (p == q) continue;

                    if (p.Fitness.Dominates(q.Fitness))
                    {
                        dominatedSolutions[p].Add(q);
                    }
                    else if (q.Fitness.Dominates(p.Fitness))
                    {
                        dominationCount[p]++;
                    }
                }

                if (dominationCount[p] == 0)
                {
                    p.Fitness.Rank = 1;
                    firstFront.Add(p);
                }
            }

            fronts.Add(firstFront);


            int currentRank = 1;
            while (fronts[currentRank - 1].Any())
            {
                var nextFront = new List<Individual>();

                foreach (var p in fronts[currentRank - 1])
                {
                    foreach (var q in dominatedSolutions[p])
                    {
                        dominationCount[q]--;
                        if (dominationCount[q] == 0)
                        {
                            q.Fitness.Rank = currentRank + 1;
                            nextFront.Add(q);
                        }
                    }
                }

                if (nextFront.Any())
                {
                    fronts.Add(nextFront);
                    currentRank++;
                }
                else
                {
                    break;
                }
            }

            return fronts;
        }


        public void CalculateCrowdingDistance(List<Individual> front)
        {
            int n = front.Count;
            if (n == 0) return;


            foreach (var ind in front)
                ind.Fitness.CrowdingDistance = 0;


            var objectives = new Func<MultiObjectiveFitness, float>[]
            {
                f => f.BalanceScore,
                f => f.EngagementScore,
                f => f.CoherenceScore
            };

            foreach (var objective in objectives)
            {

                var sorted = front.OrderBy(ind => objective(ind.Fitness)).ToList();


                sorted[0].Fitness.CrowdingDistance = float.MaxValue;
                sorted[n - 1].Fitness.CrowdingDistance = float.MaxValue;


                float minObj = objective(sorted[0].Fitness);
                float maxObj = objective(sorted[n - 1].Fitness);
                float range = maxObj - minObj;

                if (range < 1e-6) continue;


                for (int i = 1; i < n - 1; i++)
                {
                    float distance = (objective(sorted[i + 1].Fitness) -
                                     objective(sorted[i - 1].Fitness)) / range;
                    sorted[i].Fitness.CrowdingDistance += distance;
                }
            }
        }


        private List<Individual> CreateOffspring(List<Individual> population)
        {
            var offspring = new List<Individual>();

            while (offspring.Count < PopulationSize)
            {

                var parent1 = TournamentSelect(population);
                var parent2 = TournamentSelect(population);


                var child = Crossover(parent1, parent2);


                Mutate(child);

                offspring.Add(new Individual(child));
            }

            return offspring;
        }


        private Individual TournamentSelect(List<Individual> population)
        {
            var candidate1 = population[_rng.Next(population.Count)];
            var candidate2 = population[_rng.Next(population.Count)];


            if (candidate1.Fitness.Rank < candidate2.Fitness.Rank)
                return candidate1;
            if (candidate2.Fitness.Rank < candidate1.Fitness.Rank)
                return candidate2;


            return candidate1.Fitness.CrowdingDistance > candidate2.Fitness.CrowdingDistance
                ? candidate1
                : candidate2;
        }


        private HierarchicalGenome Crossover(Individual parent1, Individual parent2)
        {
            var p1 = parent1.Genome;
            var p2 = parent2.Genome;
            var child = new HierarchicalGenome();

            float eta = 20.0f;


            float SBX(float val1, float val2, float min, float max)
            {
                if (_rng.NextDouble() > 0.5) return val1;

                float y1 = Math.Min(val1, val2);
                float y2 = Math.Max(val1, val2);

                if (Math.Abs(y2 - y1) < 1e-6) return y1;

                float beta;
                float u = (float)_rng.NextDouble();

                if (u <= 0.5)
                    beta = (float)Math.Pow(2.0 * u, 1.0 / (eta + 1.0));
                else
                    beta = (float)Math.Pow(1.0 / (2.0 * (1.0 - u)), 1.0 / (eta + 1.0));

                float child_val = 0.5f * ((y1 + y2) - beta * (y2 - y1));
                return Math.Clamp(child_val, min, max);
            }


            child.GlobalDamageMultiplier = SBX(p1.GlobalDamageMultiplier, p2.GlobalDamageMultiplier, 0.7f, 1.3f);
            child.GlobalHealthMultiplier = SBX(p1.GlobalHealthMultiplier, p2.GlobalHealthMultiplier, 0.7f, 1.3f);
            child.GlobalBlockMultiplier = SBX(p1.GlobalBlockMultiplier, p2.GlobalBlockMultiplier, 0.7f, 1.3f);
            child.GlobalManaCostMultiplier = SBX(p1.GlobalManaCostMultiplier, p2.GlobalManaCostMultiplier, 0.85f, 1.15f);
            child.GlobalGoldMultiplier = SBX(p1.GlobalGoldMultiplier, p2.GlobalGoldMultiplier, 0.7f, 1.3f);

            child.EarlyGameDamageScaling = SBX(p1.EarlyGameDamageScaling, p2.EarlyGameDamageScaling, 0.7f, 1.2f);
            child.MidGameDamageScaling = SBX(p1.MidGameDamageScaling, p2.MidGameDamageScaling, 0.9f, 1.4f);
            child.LateGameDamageScaling = SBX(p1.LateGameDamageScaling, p2.LateGameDamageScaling, 1.1f, 1.6f);

            child.EarlyGameHealthScaling = SBX(p1.EarlyGameHealthScaling, p2.EarlyGameHealthScaling, 0.7f, 1.2f);
            child.MidGameHealthScaling = SBX(p1.MidGameHealthScaling, p2.MidGameHealthScaling, 0.9f, 1.4f);
            child.LateGameHealthScaling = SBX(p1.LateGameHealthScaling, p2.LateGameHealthScaling, 1.1f, 1.6f);

            child.EarlyGameBlockScaling = SBX(p1.EarlyGameBlockScaling, p2.EarlyGameBlockScaling, 0.8f, 1.1f);
            child.MidGameBlockScaling = SBX(p1.MidGameBlockScaling, p2.MidGameBlockScaling, 0.9f, 1.2f);
            child.LateGameBlockScaling = SBX(p1.LateGameBlockScaling, p2.LateGameBlockScaling, 0.9f, 1.2f);


            foreach (var key in p1.CardTypeScalars.Keys)
                child.CardTypeScalars[key] = _rng.NextDouble() < 0.5 ? p1.CardTypeScalars[key] : p2.CardTypeScalars[key];

            foreach (var key in p1.CardStarScalars.Keys)
                child.CardStarScalars[key] = _rng.NextDouble() < 0.5 ? p1.CardStarScalars[key] : p2.CardStarScalars[key];

            foreach (var key in p1.EnemyStarScalars.Keys)
                child.EnemyStarScalars[key] = _rng.NextDouble() < 0.5 ? p1.EnemyStarScalars[key] : p2.EnemyStarScalars[key];


            foreach (var key in p1.RoomTypeWeights.Keys)
                child.RoomTypeWeights[key] = _rng.NextDouble() < 0.5 ? p1.RoomTypeWeights[key] : p2.RoomTypeWeights[key];

            child.MonsterStarRatio = SBX(p1.MonsterStarRatio, p2.MonsterStarRatio, 0.2f, 0.8f);
            child.EliteStarRatio = SBX(p1.EliteStarRatio, p2.EliteStarRatio, 0.2f, 0.8f);
            child.RestHealingScalar = SBX(p1.RestHealingScalar, p2.RestHealingScalar, 0.5f, 1.5f);

            child.HeroHealthScalar = SBX(p1.HeroHealthScalar, p2.HeroHealthScalar, 0.7f, 1.3f);
            child.HeroStartGoldScalar = SBX(p1.HeroStartGoldScalar, p2.HeroStartGoldScalar, 0.8f, 1.2f);
            child.HeroManaOffset = _rng.NextDouble() < 0.5 ? p1.HeroManaOffset : p2.HeroManaOffset;

            return child;
        }


        private void Mutate(HierarchicalGenome genome)
        {
            float eta = 20.0f;

            float MutateValue(float value, float min, float max)
            {
                if (_rng.NextDouble() > MutationRate) return value;

                float delta = max - min;
                float u = (float)_rng.NextDouble();
                float delta_q;

                if (u < 0.5)
                    delta_q = (float)(Math.Pow(2.0 * u, 1.0 / (eta + 1.0)) - 1.0);
                else
                    delta_q = (float)(1.0 - Math.Pow(2.0 * (1.0 - u), 1.0 / (eta + 1.0)));

                value += delta_q * MutationStrength * delta;
                return Math.Clamp(value, min, max);
            }


            genome.GlobalDamageMultiplier = MutateValue(genome.GlobalDamageMultiplier, 0.7f, 1.3f);
            genome.GlobalHealthMultiplier = MutateValue(genome.GlobalHealthMultiplier, 0.7f, 1.3f);
            genome.GlobalBlockMultiplier = MutateValue(genome.GlobalBlockMultiplier, 0.7f, 1.3f);
            genome.GlobalManaCostMultiplier = MutateValue(genome.GlobalManaCostMultiplier, 0.85f, 1.15f);
            genome.GlobalGoldMultiplier = MutateValue(genome.GlobalGoldMultiplier, 0.7f, 1.3f);

            genome.EarlyGameDamageScaling = MutateValue(genome.EarlyGameDamageScaling, 0.7f, 1.2f);
            genome.MidGameDamageScaling = MutateValue(genome.MidGameDamageScaling, 0.9f, 1.4f);
            genome.LateGameDamageScaling = MutateValue(genome.LateGameDamageScaling, 1.1f, 1.6f);

            genome.EarlyGameHealthScaling = MutateValue(genome.EarlyGameHealthScaling, 0.7f, 1.2f);
            genome.MidGameHealthScaling = MutateValue(genome.MidGameHealthScaling, 0.9f, 1.4f);
            genome.LateGameHealthScaling = MutateValue(genome.LateGameHealthScaling, 1.1f, 1.6f);

            genome.EarlyGameBlockScaling = MutateValue(genome.EarlyGameBlockScaling, 0.8f, 1.1f);
            genome.MidGameBlockScaling = MutateValue(genome.MidGameBlockScaling, 0.9f, 1.2f);
            genome.LateGameBlockScaling = MutateValue(genome.LateGameBlockScaling, 0.9f, 1.2f);


            foreach (var key in genome.CardTypeScalars.Keys.ToList())
            {
                genome.CardTypeScalars[key] = MutateValue(genome.CardTypeScalars[key], 0.85f, 1.15f);
            }

            foreach (var key in genome.CardStarScalars.Keys.ToList())
            {
                genome.CardStarScalars[key] = MutateValue(genome.CardStarScalars[key], 0.9f, 1.1f);
            }

            foreach (var key in genome.EnemyStarScalars.Keys.ToList())
            {
                genome.EnemyStarScalars[key] = MutateValue(genome.EnemyStarScalars[key], 0.9f, 1.1f);
            }


            foreach (var key in genome.RoomTypeWeights.Keys.ToList())
            {
                if (_rng.NextDouble() < MutationRate)
                {
                    genome.RoomTypeWeights[key] += (float)(_rng.NextDouble() * 10 - 5);
                    genome.RoomTypeWeights[key] = Math.Max(5f, genome.RoomTypeWeights[key]);
                }
            }

            genome.MonsterStarRatio = MutateValue(genome.MonsterStarRatio, 0.2f, 0.8f);
            genome.EliteStarRatio = MutateValue(genome.EliteStarRatio, 0.2f, 0.8f);
            genome.RestHealingScalar = MutateValue(genome.RestHealingScalar, 0.5f, 1.5f);

            genome.HeroHealthScalar = MutateValue(genome.HeroHealthScalar, 0.7f, 1.3f);
            genome.HeroStartGoldScalar = MutateValue(genome.HeroStartGoldScalar, 0.8f, 1.2f);
            genome.DifficultyProgressionRate = MutateValue(genome.DifficultyProgressionRate, 0.0f, 0.06f);

            if (_rng.NextDouble() < MutationRate * 0.1)
                genome.HeroManaOffset = Math.Clamp(genome.HeroManaOffset + _rng.Next(-1, 2), -1, 1);


            MutateOverrides(genome);
        }


        private void MutateOverrides(HierarchicalGenome genome)
        {

            float overrideChance = MutationRate * 0.3f;


            if (_rng.NextDouble() < overrideChance)
            {
                if (genome.CardDamageOverrides.Count > 0 && _rng.NextDouble() < 0.4)
                {

                    var keys = genome.CardDamageOverrides.Keys.ToList();
                    genome.CardDamageOverrides.Remove(keys[_rng.Next(keys.Count)]);
                }
                else if (genome.CardDamageOverrides.Count < 5 && _cardPool != null)
                {

                    var availableCards = _cardPool.CardsById.Keys.ToList();
                    if (availableCards.Any())
                    {
                        string cardId = availableCards[_rng.Next(availableCards.Count)];
                        genome.CardDamageOverrides[cardId] = (float)(0.7 + _rng.NextDouble() * 0.6);
                    }
                }
            }


            foreach (var key in genome.CardDamageOverrides.Keys.ToList())
            {
                if (_rng.NextDouble() < MutationRate)
                {
                    float current = genome.CardDamageOverrides[key];
                    float delta = (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.CardDamageOverrides[key] = Math.Clamp(current + delta, 0.5f, 1.5f);
                }
            }


            if (_rng.NextDouble() < overrideChance)
            {
                if (genome.CardManaCostOverrides.Count > 0 && _rng.NextDouble() < 0.4)
                {
                    var keys = genome.CardManaCostOverrides.Keys.ToList();
                    genome.CardManaCostOverrides.Remove(keys[_rng.Next(keys.Count)]);
                }
                else if (genome.CardManaCostOverrides.Count < 3 && _cardPool != null)
                {
                    var availableCards = _cardPool.CardsById.Keys.ToList();
                    if (availableCards.Any())
                    {
                        string cardId = availableCards[_rng.Next(availableCards.Count)];
                        genome.CardManaCostOverrides[cardId] = (float)(0.8 + _rng.NextDouble() * 0.4);
                    }
                }
            }

            foreach (var key in genome.CardManaCostOverrides.Keys.ToList())
            {
                if (_rng.NextDouble() < MutationRate)
                {
                    float current = genome.CardManaCostOverrides[key];
                    float delta = (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.CardManaCostOverrides[key] = Math.Clamp(current + delta, 0.7f, 1.3f);
                }
            }


            if (_rng.NextDouble() < overrideChance)
            {
                if (genome.EnemyHealthOverrides.Count > 0 && _rng.NextDouble() < 0.4)
                {
                    var keys = genome.EnemyHealthOverrides.Keys.ToList();
                    genome.EnemyHealthOverrides.Remove(keys[_rng.Next(keys.Count)]);
                }
                else if (genome.EnemyHealthOverrides.Count < 5 && _enemyPool != null)
                {
                    var availableEnemies = _enemyPool.EnemiesById.Keys.ToList();
                    if (availableEnemies.Any())
                    {
                        string enemyId = availableEnemies[_rng.Next(availableEnemies.Count)];
                        genome.EnemyHealthOverrides[enemyId] = (float)(0.7 + _rng.NextDouble() * 0.6);
                    }
                }
            }

            foreach (var key in genome.EnemyHealthOverrides.Keys.ToList())
            {
                if (_rng.NextDouble() < MutationRate)
                {
                    float current = genome.EnemyHealthOverrides[key];
                    float delta = (float)(_rng.NextDouble() * 0.2 - 0.1);
                    genome.EnemyHealthOverrides[key] = Math.Clamp(current + delta, 0.5f, 1.5f);
                }
            }
        }


        public List<Individual> GetParetoFront(List<Individual> population)
        {
            return population.Where(ind => ind.Fitness.Rank == 1).ToList();
        }
    }
}
