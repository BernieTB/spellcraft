using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Randomness;
using Game.Core.Runs;
using Game.Core.SpellLines;
using Game.Core.Upgrades;

namespace Game.Unity.Tests.Balance
{
    /// <summary>How well the bot plays (#125).</summary>
    public enum BotKind
    {
        /// <summary>Never touches the line and always takes the first level-up package.</summary>
        Naive,

        /// <summary>Orders the line with the bonus arithmetic and picks the packages that raise the line's output.</summary>
        Intermediate,

        /// <summary>Optimises the line (order, reserve swaps) and the packages by simulating the next fights.</summary>
        Expert,
    }

    /// <summary>Which steps the bot plays.</summary>
    public enum StepPolicy
    {
        /// <summary>Opens both secret rooms, then faces the professor.</summary>
        FullBiome,

        /// <summary>Plays the regular fights the professor needs and goes straight to him, with no room and no revelation.</summary>
        ShortPath,
    }

    /// <summary>The content a balance run is played on: the MVP class, the biome and the passive pool.</summary>
    public sealed class BalanceContent
    {
        public ClassDefinition HeroClass { get; set; }

        public BiomeDefinition Biome { get; set; }

        public RunRules Rules { get; set; }

        public IReadOnlyList<PassiveUpgrade> Passives { get; set; }
    }

    /// <summary>What one simulated run gave.</summary>
    public sealed class BotRunResult
    {
        public ulong Seed { get; set; }

        public bool Won { get; set; }

        /// <summary>"Regular", "MiniBoss ROOM_xx", "Professor", or null on a victory.</summary>
        public string DeathStage { get; set; }

        public int Fights { get; set; }

        public int RegularFights { get; set; }

        public int Ticks { get; set; }

        /// <summary>True when the run ended on a fight that hit the time limit.</summary>
        public bool TimedOut { get; set; }

        /// <summary>Ticks of the longest fight of the run.</summary>
        public int LongestFight { get; set; }

        public int Level { get; set; }

        public int RoomsCleared { get; set; }

        public int PreparedFights { get; set; }

        /// <summary>Ticks of the professor fight, 0 when it was not reached.</summary>
        public int ProfessorTicks { get; set; }

        public int RegularTicks { get; set; }

        public int MiniBossTicks { get; set; }

        public int MiniBossFights { get; set; }

        /// <summary>Estimated play time at speed x1 (see <see cref="BalanceBot.EstimateSeconds"/>).</summary>
        public double Seconds { get; set; }
    }

    /// <summary>
    /// Plays complete runs of the biome without any screen, as a bot (#125). Uses the real <see cref="Run"/>, so the
    /// numbers measured are those of the game; the bots only choose: the steps, the level-up packages and the order of the line.
    /// </summary>
    public sealed class BalanceBot
    {
        // Presentation assumptions of the estimate (RunScreenSettings and a player's reading time), not balance numbers.
        public const double TicksPerSecond = 4d;
        public const double PauseSecondsBetweenRegularFights = 1.5d;
        public const double LevelUpSeconds = 15d;
        public const double PreparationSeconds = 30d;

        /// <summary>Regular fights won before the bot opens its first secret room (a player does not enter one with the starting line).</summary>
        public int RoomMinimumRegularFights { get; set; } = 6;

        /// <summary>Further regular fights won before each next room.</summary>
        public int RoomFightsStep { get; set; } = 2;

        /// <summary>Receives a line per decision when set (debugging a bot).</summary>
        public Action<string> Trace { get; set; }

        /// <summary>Called just before a step is played, with the run (diagnostics).</summary>
        public Action<Run, RunStep> BeforeFight { get; set; }

        /// <summary>
        /// How much the order of the current line matters against an encounter: over every order of the line, the fights
        /// won, the extremes of the length and of the health left (diagnostics).
        /// </summary>
        public string OrderSensitivity(Run run, EncounterDefinition encounter)
        {
            var cards = run.Line.Select(c => c.CurrentDefinition).ToList();
            var wins = 0;
            var total = 0;
            int bestTicks = int.MaxValue, worstTicks = 0, bestHp = -1, worstHp = int.MaxValue;
            foreach (var permutation in Permutations(cards))
            {
                var line = new SpellLine<CardDefinition>(cards.Count);
                foreach (var card in permutation)
                {
                    line.Add(card);
                }

                var hero = run.Upgrades.ApplyTo(new FightParticipant(
                    new Combatant(_content.HeroClass.MaxHealth, _content.HeroClass.StartingShield), line));
                var result = new Fight(hero, encounter.CreateParticipants(), _content.Rules.FightTimeLimit, new Pcg32Random(1UL, 0UL)).Run();
                total++;
                if (result.Winner == FightWinner.Hero)
                {
                    wins++;
                    bestTicks = Math.Min(bestTicks, result.Ticks);
                    worstTicks = Math.Max(worstTicks, result.Ticks);
                    bestHp = Math.Max(bestHp, hero.Combatant.CurrentHealth);
                    worstHp = Math.Min(worstHp, hero.Combatant.CurrentHealth);
                }
            }

            return $"{wins}/{total} orders win; ticks {bestTicks}-{worstTicks}; hp left {worstHp}-{bestHp}";
        }

        private readonly BalanceContent _content;
        private readonly BotKind _kind;
        private readonly StepPolicy _policy;
        private readonly Dictionary<string, double> _simCache;
        private readonly Dictionary<string, double> _analyticCache = new Dictionary<string, double>();
        private readonly Dictionary<string, int[]> _permutationCache = new Dictionary<string, int[]>();
        private readonly List<EncounterDefinition> _regularTargets;

        public BalanceBot(BalanceContent content, BotKind kind, StepPolicy policy, Dictionary<string, double> simCache = null)
        {
            _content = content;
            _kind = kind;
            _policy = policy;
            _simCache = simCache ?? new Dictionary<string, double>();
            _regularTargets = content.Biome.RegularEncounters.GroupBy(e => e.Id).Select(g => g.First()).ToList();
        }

        public static double EstimateSeconds(int ticks, int regularFights, int levelUps, int preparations)
        {
            return (ticks / TicksPerSecond) + (regularFights * PauseSecondsBetweenRegularFights)
                + (levelUps * LevelUpSeconds) + (preparations * PreparationSeconds);
        }

        public BotRunResult Play(ulong seed)
        {
            var run = new Run(_content.HeroClass, _content.Biome, _content.Rules, seed);
            var result = new BotRunResult { Seed = seed };
            var signature = string.Empty;
            var preparations = 0;
            var levelUps = 0;
            while (run.IsInProgress)
            {
                while (run.HasPendingChoice)
                {
                    TakePackage(run);
                    levelUps++;
                }

                var step = ChooseStep(run);
                signature = ArrangeLine(run, step, signature);

                Trace?.Invoke($"line [{string.Join(" ", run.Line.Select(c => c.Definition.Id + (c.Stage > 0 ? "*" + c.Stage : string.Empty)))}] reserve [{string.Join(" ", run.Reserve.Select(c => c.Definition.Id))}] hp+{run.Upgrades.MaxHealthBonus} step {step}");
                BeforeFight?.Invoke(run, step);
                var report = run.Play(step);
                Trace?.Invoke($"  {report.Encounter.Id}: {(report.HeroWon ? "won" : "LOST")} in {report.Log.Ticks}");
                result.Ticks += report.Log.Ticks;
                result.LongestFight = Math.Max(result.LongestFight, report.Log.Ticks);
                switch (step.Kind)
                {
                    case RunStepKind.RegularFight:
                        result.RegularFights++;
                        result.RegularTicks += report.Log.Ticks;
                        break;
                    case RunStepKind.SecretRoom:
                        preparations++;
                        result.MiniBossFights++;
                        result.MiniBossTicks += report.Log.Ticks;
                        break;
                    default:
                        preparations++;
                        result.ProfessorTicks = report.Log.Ticks;
                        break;
                }

                if (!report.HeroWon)
                {
                    result.TimedOut = report.TimedOut;
                    result.DeathStage = step.Kind == RunStepKind.RegularFight ? "Regular"
                        : step.Kind == RunStepKind.SecretRoom ? "MiniBoss " + step.SecretRoomId : "Professor";
                }

                if (run.FightsPlayed > 300)
                {
                    throw new InvalidOperationException($"The run of seed {seed} does not end.");
                }
            }

            result.Won = run.Outcome == RunOutcome.Victory;
            result.Fights = run.FightsPlayed;
            result.Level = run.Level;
            result.RoomsCleared = run.SecretRooms.Count(room => room.IsCleared);
            result.PreparedFights = preparations;
            result.Seconds = EstimateSeconds(result.Ticks, result.RegularFights, run.Level - 1, preparations);
            return result;
        }

        /// <summary>
        /// Orders the line (and the reserve swaps) for the next step like this bot would, unless nothing changed since
        /// <paramref name="signature"/> (the value returned by the previous call, empty at first). The naive bot never does.
        /// </summary>
        public string ArrangeLine(Run run, RunStep step, string signature = "")
        {
            if (_kind == BotKind.Naive)
            {
                return signature;
            }

            var target = TargetsFor(run, step, out var targetKey);
            var informed = targetKey != "professor-unknown";
            if (StateSignature(run) + "|" + targetKey == signature)
            {
                return signature;
            }

            Arrange(run, target, informed);
            return StateSignature(run) + "|" + targetKey;
        }

        private RunStep ChooseStep(Run run)
        {
            if (_policy == StepPolicy.FullBiome)
            {
                // A player does not open a room with the starting line: the bots wait for a few more fights per room cleared.
                var cleared = run.SecretRooms.Count(status => status.IsCleared);
                var ready = run.RegularFightsWon >= RoomMinimumRegularFights + (RoomFightsStep * cleared);
                var room = !ready ? null : run.AvailableSteps.FirstOrDefault(
                    step => step.Kind == RunStepKind.SecretRoom
                        && !run.SecretRooms.Single(status => status.Definition.Id == step.SecretRoomId).IsCleared);
                if (room != null)
                {
                    return room;
                }

                return run.SecretRooms.All(status => status.IsCleared) && run.IsProfessorAvailable
                    ? RunStep.Professor
                    : RunStep.RegularFight;
            }

            return run.IsProfessorAvailable ? RunStep.Professor : RunStep.RegularFight;
        }

        // ---------------------------------------------------------------- level-up

        private void TakePackage(Run run)
        {
            var offer = run.GetLevelUpOffer(_content.Passives);
            if (_kind == BotKind.Naive)
            {
                run.TakeLevelUpPackage(0);
                return;
            }

            var bestPackage = 0;
            int? bestReplace = null;
            var bestScore = double.NegativeInfinity;
            for (var p = 0; p < offer.Packages.Count; p++)
            {
                var package = offer.Packages[p];
                var set = run.Upgrades.With(package.Passive);
                foreach (var option in Options(run))
                {
                    var line = LineDefinitions(run, option, package.Card);
                    var score = _kind == BotKind.Intermediate
                        ? BestAnalytic(line, set)
                        : BestSimulated(run, line, set, PlanningTargets(run));
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        bestPackage = p;
                        bestReplace = option;
                    }
                }
            }

            var chosen = offer.Packages[bestPackage];
            Trace?.Invoke($"level-up -> {chosen.Card.Id} + {chosen.Passive.Id} (replace {bestReplace})");
            run.TakeLevelUpPackage(bestPackage, bestReplace >= 0 ? bestReplace : null);
        }

        // The ways to take a card: into the free slot, instead of each line card, or into the reserve (null).
        private static IEnumerable<int?> Options(Run run)
        {
            if (run.Line.Count < run.LineCapacity)
            {
                yield return null;
                yield break;
            }

            yield return -1; // reserve: the line is unchanged
            for (var i = 0; i < run.Line.Count; i++)
            {
                yield return i;
            }
        }

        private static List<CardDefinition> LineDefinitions(Run run, int? option, CardDefinition added)
        {
            var line = run.Line.Select(card => card.CurrentDefinition).ToList();
            if (option == null)
            {
                line.Add(added);
            }
            else if (option >= 0)
            {
                line[option.Value] = added;
            }

            return line;
        }

        // ---------------------------------------------------------------- targets and signature

        private List<EncounterDefinition> TargetsFor(Run run, RunStep step, out string key)
        {
            switch (step.Kind)
            {
                case RunStepKind.SecretRoom:
                    var room = run.SecretRooms.Single(status => status.Definition.Id == step.SecretRoomId);
                    key = room.Definition.MiniBossEncounter.Id;
                    return new List<EncounterDefinition> { room.Definition.MiniBossEncounter };
                case RunStepKind.Professor:
                    // The professor's line is only known once both mini-bosses revealed it (ADR 0010).
                    if (run.SecretRooms.All(status => status.IsCleared))
                    {
                        key = "professor";
                        return new List<EncounterDefinition> { RevealedProfessor(run) };
                    }

                    // Without the revelations the expert prepares for a boss in general: the mini-bosses are his model.
                    key = "professor-unknown";
                    return run.Biome.SecretRooms.Select(room => room.MiniBossEncounter).ToList();
                default:
                    key = "regular";
                    return _regularTargets;
            }
        }

        // The professor as the revelations of the mini-bosses show him (ADR 0010): his health and shield, and the cards at
        // the revealed positions. A card nobody revealed is guessed as a copy of the strongest revealed attack.
        private static EncounterDefinition RevealedProfessor(Run run)
        {
            var professor = run.Biome.Professor;
            var known = new HashSet<int>(run.SecretRooms.SelectMany(status => status.Definition.Revelation.CardPositions));
            var strongest = known.Where(position => position < professor.SpellLine.Count)
                .Select(position => professor.SpellLine[position])
                .OrderByDescending(card => card.Effects.OfType<DealDamageEffect>().Sum(effect => effect.Amount) / (double)card.CastTime)
                .First();
            var line = new List<CardDefinition>();
            for (var position = 0; position < professor.SpellLine.Count; position++)
            {
                line.Add(known.Contains(position) ? professor.SpellLine[position] : strongest);
            }

            var model = new EnemyDefinition(professor.Id, professor.MaxHealth, professor.Shield, line, professor.XpReward);
            return new EncounterDefinition(run.Biome.ProfessorEncounter.Id + "-revealed", new[] { model });
        }

        // What the expert builds for when he takes a card: the regular fights, the mini-bosses and, once known, the professor.
        private List<EncounterDefinition> PlanningTargets(Run run)
        {
            var targets = new List<EncounterDefinition>(_regularTargets);
            targets.AddRange(run.Biome.SecretRooms.Select(room => room.MiniBossEncounter));
            if (run.SecretRooms.All(status => status.IsCleared))
            {
                targets.Add(run.Biome.ProfessorEncounter);
            }

            return targets;
        }

        private static string StateSignature(Run run)
        {
            return string.Join(",", run.Line.Select(c => c.Definition.Id + ":" + c.Stage))
                + "/" + string.Join(",", run.Reserve.Select(c => c.Definition.Id + ":" + c.Stage))
                + "/" + run.LineCapacity + "/" + run.Upgrades.Upgrades.Count;
        }

        // ---------------------------------------------------------------- arranging the line

        // Without a model of the fight (the professor before his revelations) the expert orders like the intermediate player.
        private void Arrange(Run run, List<EncounterDefinition> targets, bool informed)
        {
            var pool = run.Line.Concat(run.Reserve).ToList();
            var size = Math.Min(run.LineCapacity, pool.Count);
            var order = run.Line.ToList();
            foreach (var spare in run.Reserve)
            {
                if (order.Count < size)
                {
                    order.Add(spare);
                }
            }

            if (_kind == BotKind.Intermediate || !informed)
            {
                order = ClimbAnalytic(order, pool, run.Upgrades);
            }
            else
            {
                order = HillClimb(run, order, pool, targets);
            }

            Apply(run, order);
        }

        private List<CardInstance> BestPermutation(List<CardInstance> order, PassiveUpgradeSet set)
        {
            var key = string.Join(",", order.Select(c => c.Definition.Id + ":" + c.Stage))
                + "|" + string.Join(",", set.Upgrades.Select(u => u.Id));
            if (!_permutationCache.TryGetValue(key, out var best))
            {
                var bestScore = double.NegativeInfinity;
                best = Enumerable.Range(0, order.Count).ToArray();
                var prepared = order.Select(c => Prepare(set.ApplyTo(c.CurrentDefinition))).ToList();
                foreach (var permutation in Permutations(Enumerable.Range(0, order.Count).ToList()))
                {
                    var score = AnalyticOf(prepared, permutation.ToArray(), set);
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        best = permutation.ToArray();
                    }
                }

                _permutationCache[key] = best;
            }

            return best.Select(i => order[i]).ToList();
        }

        // The order, and the cards of the line, that raise the output of the loop the most (bonus arithmetic only).
        private List<CardInstance> ClimbAnalytic(List<CardInstance> start, List<CardInstance> pool, PassiveUpgradeSet set)
        {
            var current = BestPermutation(start, set);
            var currentScore = AnalyticScore(current, set);
            for (var round = 0; round < 12; round++)
            {
                List<CardInstance> best = null;
                var bestScore = currentScore;
                foreach (var candidate in Neighbours(current, pool))
                {
                    var score = AnalyticScore(candidate, set);
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }

                if (best == null)
                {
                    break;
                }

                current = BestPermutation(best, set);
                currentScore = AnalyticScore(current, set);
            }

            return current;
        }

        private double AnalyticScore(List<CardInstance> order, PassiveUpgradeSet set)
        {
            var cards = order.Select(c => Prepare(set.ApplyTo(c.CurrentDefinition))).ToList();
            return AnalyticOf(cards, Enumerable.Range(0, cards.Count).ToArray(), set);
        }

        private List<CardInstance> HillClimb(Run run, List<CardInstance> start, List<CardInstance> pool, List<EncounterDefinition> targets)
        {
            var current = start;
            var currentScore = Simulated(run, current.Select(c => c.CurrentDefinition).ToList(), run.Upgrades, targets);
            for (var round = 0; round < 12; round++)
            {
                List<CardInstance> best = null;
                var bestScore = currentScore;
                foreach (var candidate in Neighbours(current, pool))
                {
                    var score = Simulated(run, candidate.Select(c => c.CurrentDefinition).ToList(), run.Upgrades, targets);
                    if (score > bestScore + 1e-9)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }

                if (best == null)
                {
                    break;
                }

                current = best;
                currentScore = bestScore;
            }

            return current;
        }

        private static IEnumerable<List<CardInstance>> Neighbours(List<CardInstance> order, List<CardInstance> pool)
        {
            for (var i = 0; i < order.Count; i++)
            {
                for (var j = 0; j < order.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (i < j)
                    {
                        var swapped = new List<CardInstance>(order);
                        (swapped[i], swapped[j]) = (swapped[j], swapped[i]);
                        yield return swapped;
                    }

                    var moved = new List<CardInstance>(order);
                    var card = moved[i];
                    moved.RemoveAt(i);
                    moved.Insert(j, card);
                    yield return moved;
                }

                foreach (var spare in pool)
                {
                    if (!order.Contains(spare))
                    {
                        var replaced = new List<CardInstance>(order) { [i] = spare };
                        yield return replaced;
                    }
                }
            }
        }

        // Makes the run's line exactly "order", with the run's own editing methods.
        private static void Apply(Run run, List<CardInstance> order)
        {
            foreach (var card in order)
            {
                var reserveIndex = IndexOf(run.Reserve, card);
                if (reserveIndex < 0)
                {
                    continue;
                }

                if (run.Line.Count < run.LineCapacity)
                {
                    run.MoveFromReserve(reserveIndex);
                    continue;
                }

                var out_ = -1;
                for (var i = 0; i < run.Line.Count; i++)
                {
                    if (!order.Contains(run.Line[i]))
                    {
                        out_ = i;
                        break;
                    }
                }

                run.SwapWithReserve(out_, reserveIndex);
            }

            for (var i = 0; i < order.Count; i++)
            {
                var at = IndexOf(run.Line, order[i]);
                if (at != i)
                {
                    run.MoveInLine(at, i);
                }
            }
        }

        private static int IndexOf(IReadOnlyList<CardInstance> cards, CardInstance card)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (ReferenceEquals(cards[i], card))
                {
                    return i;
                }
            }

            return -1;
        }

        private static IEnumerable<List<T>> Permutations<T>(List<T> items)
        {
            var indexes = Enumerable.Range(0, items.Count).ToArray();
            var c = new int[items.Count];
            yield return indexes.Select(i => items[i]).ToList();
            var n = 0;
            while (n < items.Count)
            {
                if (c[n] < n)
                {
                    var k = n % 2 == 0 ? 0 : c[n];
                    (indexes[k], indexes[n]) = (indexes[n], indexes[k]);
                    yield return indexes.Select(i => items[i]).ToList();
                    c[n]++;
                    n = 0;
                }
                else
                {
                    c[n] = 0;
                    n++;
                }
            }
        }

        // ---------------------------------------------------------------- scoring

        // Intermediate player: the best order of these cards by the bonus arithmetic, plus a little for the hero's health.
        private double BestAnalytic(List<CardDefinition> line, PassiveUpgradeSet set)
        {
            var key = string.Join(",", line.Select(c => c.Id + ":" + c.Stage).OrderBy(x => x, StringComparer.Ordinal))
                + "|" + string.Join(",", set.Upgrades.Select(u => u.Id).OrderBy(x => x, StringComparer.Ordinal));
            if (_analyticCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var prepared = line.Select(set.ApplyTo).Select(Prepare).ToList();
            var best = double.NegativeInfinity;
            foreach (var permutation in Permutations(Enumerable.Range(0, line.Count).ToList()))
            {
                best = Math.Max(best, AnalyticOf(prepared, permutation.ToArray(), set));
            }

            _analyticCache[key] = best;
            return best;
        }

        private sealed class Prepared
        {
            public int Cast;
            public double[] Total = new double[3];
            public bool[] Present = new bool[3];
            public int[] NextBonus = new int[3];
            public int[] PreviousBonus = new int[3];
        }

        private static Prepared Prepare(CardDefinition card)
        {
            var prepared = new Prepared { Cast = card.CastTime };
            foreach (var effect in card.Effects.OfType<IAmountEffect>())
            {
                prepared.Total[(int)effect.Kind] += effect.Amount;
                prepared.Present[(int)effect.Kind] = true;
            }

            foreach (var modifier in card.NeighbourModifiers)
            {
                var target = modifier.Direction == NeighbourDirection.Next ? prepared.NextBonus : prepared.PreviousBonus;
                target[(int)modifier.Kind] += modifier.Amount;
            }

            return prepared;
        }

        // Fight length the intermediate player has in mind when he counts the shield and the healing as health.
        private const int ReferenceFightTicks = 100;

        // The cautious expert prefers a win with health to spare over a quicker one.
        private const double HealthMarginWeight = 3d;

        // Output per tick of one loop of the line, as damage x (health + the shield and healing the fight brings).
        private double AnalyticOf(List<Prepared> cards, int[] order, PassiveUpgradeSet set)
        {
            double damage = 0;
            double defence = 0;
            var ticks = 0;
            var n = order.Length;
            for (var i = 0; i < n; i++)
            {
                var card = cards[order[i]];
                var previous = cards[order[(i + n - 1) % n]];
                var next = cards[order[(i + 1) % n]];
                for (var k = 0; k < 3; k++)
                {
                    if (!card.Present[k])
                    {
                        continue;
                    }

                    var amount = card.Total[k] + previous.NextBonus[k] + next.PreviousBonus[k];
                    if (k == (int)BonusKind.Damage)
                    {
                        damage += amount;
                    }
                    else
                    {
                        defence += amount;
                    }
                }

                ticks += card.Cast;
            }

            ticks = Math.Max(1, ticks);
            var health = _content.HeroClass.MaxHealth + _content.HeroClass.StartingShield + set.MaxHealthBonus + set.StartingShieldBonus;
            return (damage / ticks) * (health + (ReferenceFightTicks * defence / ticks));
        }

        // Expert player: the best order found by hill climbing on the simulated score.
        private double BestSimulated(Run run, List<CardDefinition> line, PassiveUpgradeSet set, List<EncounterDefinition> targets)
        {
            var current = line;
            var currentScore = Simulated(run, current, set, targets);
            for (var round = 0; round < 6; round++)
            {
                List<CardDefinition> best = null;
                var bestScore = currentScore;
                for (var i = 0; i < current.Count; i++)
                {
                    for (var j = i + 1; j < current.Count; j++)
                    {
                        var candidate = new List<CardDefinition>(current);
                        (candidate[i], candidate[j]) = (candidate[j], candidate[i]);
                        var score = Simulated(run, candidate, set, targets);
                        if (score > bestScore + 1e-9)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }

                if (best == null)
                {
                    break;
                }

                current = best;
                currentScore = bestScore;
            }

            return currentScore;
        }

        private double Simulated(Run run, List<CardDefinition> line, PassiveUpgradeSet set, List<EncounterDefinition> targets)
        {
            var total = 0d;
            foreach (var encounter in targets)
            {
                var key = encounter.Id + "|" + set.Upgrades.Count + ":" + string.Join(",", set.Upgrades.Select(u => u.Id))
                    + "|" + run.LineCapacity + "|" + string.Join(",", line.Select(c => c.Id + ":" + c.Stage));
                if (!_simCache.TryGetValue(key, out var value))
                {
                    value = Simulate(line, set, encounter);
                    _simCache[key] = value;
                }

                total += value;
            }

            return total;
        }

        private double Simulate(List<CardDefinition> line, PassiveUpgradeSet set, EncounterDefinition encounter)
        {
            var spellLine = new SpellLine<CardDefinition>(Math.Max(line.Count, 1));
            foreach (var card in line)
            {
                spellLine.Add(card);
            }

            var hero = set.ApplyTo(new FightParticipant(
                new Combatant(_content.HeroClass.MaxHealth, _content.HeroClass.StartingShield), spellLine));
            var enemies = encounter.CreateParticipants();
            var result = new Fight(hero, enemies, _content.Rules.FightTimeLimit, new Pcg32Random(1UL, 0UL)).Run();
            if (result.Winner == FightWinner.Hero)
            {
                return 1000d - result.Ticks + (HealthMarginWeight * hero.Combatant.CurrentHealth);
            }

            return -500d - enemies.Sum(e => e.Combatant.CurrentHealth);
        }
    }
}
