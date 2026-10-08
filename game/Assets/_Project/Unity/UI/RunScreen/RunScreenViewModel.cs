using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Runs;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>One objective of a secret room with its progress.</summary>
    public sealed class ObjectiveViewModel
    {
        public ObjectiveViewModel(string text, string progressText, double fraction, bool isUnlocked, bool isCleared)
        {
            Text = text;
            ProgressText = progressText;
            Fraction = fraction;
            IsUnlocked = isUnlocked;
            IsCleared = isCleared;
        }

        /// <summary>What to do (for example "Defeat enemy_id x3").</summary>
        public string Text { get; }

        /// <summary>Progress, for example "2/3", or "Unlocked" / "Cleared".</summary>
        public string ProgressText { get; }

        /// <summary>Progress from 0 to 1.</summary>
        public double Fraction { get; }

        /// <summary>True once the room is open.</summary>
        public bool IsUnlocked { get; }

        /// <summary>True once the room's mini-boss was beaten.</summary>
        public bool IsCleared { get; }
    }

    /// <summary>The always-visible figures of the run: level, XP, objectives, line capacity, reserve.</summary>
    public sealed class RunHudViewModel
    {
        public RunHudViewModel(
            string levelText,
            string xpText,
            double xpFraction,
            string lineCapacityText,
            string reserveText,
            string pendingLevelUpText,
            IReadOnlyList<ObjectiveViewModel> objectives)
        {
            LevelText = levelText;
            XpText = xpText;
            XpFraction = xpFraction;
            LineCapacityText = lineCapacityText;
            ReserveText = reserveText;
            PendingLevelUpText = pendingLevelUpText;
            Objectives = objectives;
        }

        public string LevelText { get; }

        public string XpText { get; }

        /// <summary>XP into the current level over its cost, from 0 to 1.</summary>
        public double XpFraction { get; }

        /// <summary>Cards in the line over the line's capacity.</summary>
        public string LineCapacityText { get; }

        public string ReserveText { get; }

        /// <summary>Text shown while a level-up waits, otherwise null.</summary>
        public string PendingLevelUpText { get; }

        public IReadOnlyList<ObjectiveViewModel> Objectives { get; }
    }

    /// <summary>A card in the line or the reserve, as the screen draws it.</summary>
    public sealed class CardSlotViewModel
    {
        public CardSlotViewModel(
            int index,
            string cardId,
            string detailText,
            string pendingBonusText,
            bool isCasting,
            bool isNext,
            bool isSelected,
            double castFraction)
        {
            Index = index;
            CardId = cardId;
            DetailText = detailText;
            PendingBonusText = pendingBonusText;
            IsCasting = isCasting;
            IsNext = isNext;
            IsSelected = isSelected;
            CastFraction = castFraction;
        }

        /// <summary>Position in the line, or index in the reserve, from zero.</summary>
        public int Index { get; }

        /// <summary>The card's id (cards have no display name yet).</summary>
        public string CardId { get; }

        /// <summary>Cast time and effects, for example "t3: damage 4, shield 2".</summary>
        public string DetailText { get; }

        /// <summary>The neighbour bonus waiting on this slot ("+3 damage"), or null.</summary>
        public string PendingBonusText { get; }

        public bool HasPendingBonus => PendingBonusText != null;

        /// <summary>True for the slot being cast (the slot it left, if its card went to the reserve).</summary>
        public bool IsCasting { get; }

        /// <summary>True for the slot the line casts next.</summary>
        public bool IsNext { get; }

        /// <summary>True when the player selected it to move or swap it.</summary>
        public bool IsSelected { get; }

        /// <summary>Progress of the cast being made on this slot, 0 to 1 (0 when it is not casting).</summary>
        public double CastFraction { get; }
    }

    /// <summary>One side of the fight: a hero or an enemy.</summary>
    public sealed class CombatantViewModel
    {
        public CombatantViewModel(
            string name,
            int health,
            int maxHealth,
            int shield,
            bool isDead,
            string castText,
            double castFraction)
        {
            Name = name;
            Health = health;
            MaxHealth = maxHealth;
            Shield = shield;
            IsDead = isDead;
            CastText = castText;
            CastFraction = castFraction;
        }

        public string Name { get; }

        public int Health { get; }

        public int MaxHealth { get; }

        public int Shield { get; }

        public bool IsDead { get; }

        /// <summary>The card being cast and its progress ("card_id 1/3"), or an empty text between casts.</summary>
        public string CastText { get; }

        /// <summary>Progress of the cast being made, 0 to 1.</summary>
        public double CastFraction { get; }

        public double HealthFraction => MaxHealth <= 0 ? 0d : (double)Health / MaxHealth;

        public string HealthText => $"{Health}/{MaxHealth}" + (Shield > 0 ? $" +{Shield} shield" : string.Empty);
    }

    /// <summary>The fight on screen.</summary>
    public sealed class FightViewModel
    {
        public FightViewModel(
            CombatantViewModel hero,
            IReadOnlyList<CombatantViewModel> enemies,
            string tickText,
            string speedText,
            bool isPaused,
            bool canEditLine,
            string editHintText)
        {
            Hero = hero;
            Enemies = enemies;
            TickText = tickText;
            SpeedText = speedText;
            IsPaused = isPaused;
            CanEditLine = canEditLine;
            EditHintText = editHintText;
        }

        public CombatantViewModel Hero { get; }

        public IReadOnlyList<CombatantViewModel> Enemies { get; }

        public string TickText { get; }

        public string SpeedText { get; }

        public bool IsPaused { get; }

        /// <summary>True during a regular fight: the line can be rearranged now.</summary>
        public bool CanEditLine { get; }

        /// <summary>Explains the editing, or why the line is fixed.</summary>
        public string EditHintText { get; }
    }

    /// <summary>What the screen says when a fight just ended.</summary>
    public sealed class FightResultViewModel
    {
        public FightResultViewModel(string title, bool isVictory, IReadOnlyList<string> lines, string continueText)
        {
            Title = title;
            IsVictory = isVictory;
            Lines = lines;
            ContinueText = continueText;
        }

        public string Title { get; }

        public bool IsVictory { get; }

        /// <summary>Gains and unlocks, one line each.</summary>
        public IReadOnlyList<string> Lines { get; }

        public string ContinueText { get; }
    }

    /// <summary>
    /// Everything the run screen shows, built from Core state by <see cref="From"/>. A pure view model: no Unity type,
    /// no rule, so what the screen says is tested without a scene. Texts are English placeholders (localisation is
    /// not decided).
    /// </summary>
    public sealed class RunScreenViewModel
    {
        private RunScreenViewModel()
        {
        }

        public RunScreenPhase Phase { get; private set; }

        public RunHudViewModel Hud { get; private set; }

        /// <summary>The spell line, in order.</summary>
        public IReadOnlyList<CardSlotViewModel> Line { get; private set; }

        /// <summary>The reserve, in order.</summary>
        public IReadOnlyList<CardSlotViewModel> Reserve { get; private set; }

        /// <summary>The next-step choices (only between fights).</summary>
        public IReadOnlyList<StepChoice> Steps { get; private set; }

        /// <summary>The fight on screen, or null outside a fight.</summary>
        public FightViewModel Fight { get; private set; }

        /// <summary>The result of the fight that just ended, or null.</summary>
        public FightResultViewModel Result { get; private set; }

        /// <summary>The headline of the screen.</summary>
        public string StatusText { get; private set; }

        /// <summary>True when a level-up waits (the pending choice must be opened).</summary>
        public bool HasPendingChoice { get; private set; }

        /// <summary>Builds the view model of the controller's current state.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="controller"/> is null.</exception>
        public static RunScreenViewModel From(RunScreenController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            var run = controller.Run;
            var session = controller.Session;
            var model = new RunScreenViewModel
            {
                Phase = controller.Phase,
                Hud = BuildHud(run),
                HasPendingChoice = run.HasPendingChoice,
                Steps = controller.StepChoices,
            };

            if (session != null)
            {
                BuildFightLists(model, controller, session);
                model.Fight = BuildFight(controller, session);
                model.StatusText = $"Fighting: {EncounterName(session)}";
            }
            else
            {
                BuildRunLists(model, run);
                model.StatusText = StatusOutsideFight(controller);
            }

            if (controller.Phase == RunScreenPhase.FightResult || controller.Phase == RunScreenPhase.RunEnded)
            {
                model.Result = BuildResult(controller);
            }

            return model;
        }

        /// <summary>Describes a card: cast time and effects, for example "t3: damage 4, shield 2".</summary>
        public static string DescribeCard(CardDefinition card)
        {
            var text = new StringBuilder();
            text.Append('t').Append(card.CastTime).Append(": ");
            var first = true;
            foreach (var effect in card.Effects)
            {
                if (!first)
                {
                    text.Append(", ");
                }

                first = false;
                if (effect is IAmountEffect amount)
                {
                    text.Append(KindWord(amount.Kind)).Append(' ').Append(amount.Amount);
                }
                else
                {
                    text.Append(effect.GetType().Name);
                }
            }

            if (first)
            {
                text.Append("no direct effect");
            }

            foreach (var modifier in card.NeighbourModifiers)
            {
                text.Append(" | ")
                    .Append(modifier.Direction == NeighbourDirection.Next ? "next" : "previous")
                    .Append(" +")
                    .Append(modifier.Amount)
                    .Append(' ')
                    .Append(KindWord(modifier.Kind));
            }

            return text.ToString();
        }

        /// <summary>Describes a waiting neighbour bonus ("+3 damage, +2 shield"), or null when there is none.</summary>
        public static string DescribeBonus(EffectBonus bonus)
        {
            if (bonus.IsNone)
            {
                return null;
            }

            var parts = new List<string>();
            AddPart(parts, bonus.Damage, BonusKind.Damage);
            AddPart(parts, bonus.Heal, BonusKind.Heal);
            AddPart(parts, bonus.Shield, BonusKind.Shield);
            return string.Join(", ", parts);
        }

        private static void AddPart(List<string> parts, int amount, BonusKind kind)
        {
            if (amount != 0)
            {
                parts.Add($"+{amount} {KindWord(kind)}");
            }
        }

        private static string KindWord(BonusKind kind)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return "damage";
                case BonusKind.Heal:
                    return "heal";
                default:
                    return "shield";
            }
        }

        private static RunHudViewModel BuildHud(Run run)
        {
            var cost = run.XpForNextLevel;
            var into = run.XpIntoLevel;
            var fraction = cost <= 0 ? 0d : Math.Min(1d, (double)into / cost);
            var objectives = new List<ObjectiveViewModel>();
            foreach (var room in run.SecretRooms)
            {
                var target = room.Definition.Objective.Count;
                var progress = Math.Min(room.ObjectiveProgress, target);
                var text = $"Defeat {room.Definition.Objective.EnemyId} x{target}";
                var progressText = room.IsCleared ? "Cleared" : room.IsUnlocked ? "Unlocked" : $"{progress}/{target}";
                var roomFraction = room.IsUnlocked || room.IsCleared || target <= 0 ? 1d : (double)progress / target;
                objectives.Add(new ObjectiveViewModel(text, progressText, roomFraction, room.IsUnlocked, room.IsCleared));
            }

            var pending = run.PendingLevelUps;
            return new RunHudViewModel(
                $"Level {run.Level}",
                $"XP {into}/{cost}",
                fraction,
                $"Line {run.Line.Count}/{run.LineCapacity}",
                $"Reserve {run.Reserve.Count}",
                pending > 0 ? $"Level-up waiting ({pending})" : null,
                objectives.AsReadOnly());
        }

        private static void BuildRunLists(RunScreenViewModel model, Run run)
        {
            var line = new List<CardSlotViewModel>(run.Line.Count);
            for (var i = 0; i < run.Line.Count; i++)
            {
                line.Add(Slot(i, run.Line[i].CurrentDefinition, null, false, false, false, 0d));
            }

            var reserve = new List<CardSlotViewModel>(run.Reserve.Count);
            for (var i = 0; i < run.Reserve.Count; i++)
            {
                reserve.Add(Slot(i, run.Reserve[i].CurrentDefinition, null, false, false, false, 0d));
            }

            model.Line = line.AsReadOnly();
            model.Reserve = reserve.AsReadOnly();
        }

        private static void BuildFightLists(RunScreenViewModel model, RunScreenController controller, RunFightSession session)
        {
            var cast = session.HeroCast;
            var line = new List<CardSlotViewModel>(session.HeroLine.Count);
            for (var i = 0; i < session.HeroLine.Count; i++)
            {
                var casting = cast.IsCasting && cast.Position == i;
                var next = !cast.IsCasting && cast.Position == i;
                var fraction = casting && cast.CastTime > 0 ? (double)cast.ElapsedTicks / cast.CastTime : 0d;
                line.Add(Slot(
                    i,
                    session.HeroLine[i],
                    DescribeBonus(session.HeroPendingBonus(i)),
                    casting,
                    next,
                    controller.SelectedLinePosition == i,
                    fraction));
            }

            var reserve = new List<CardSlotViewModel>(session.HeroReserve.Count);
            for (var i = 0; i < session.HeroReserve.Count; i++)
            {
                reserve.Add(Slot(i, session.HeroReserve[i], null, false, false, controller.SelectedReserveIndex == i, 0d));
            }

            model.Line = line.AsReadOnly();
            model.Reserve = reserve.AsReadOnly();
        }

        private static CardSlotViewModel Slot(
            int index,
            CardDefinition card,
            string pendingBonus,
            bool isCasting,
            bool isNext,
            bool isSelected,
            double castFraction)
        {
            return new CardSlotViewModel(index, card.Id, DescribeCard(card), pendingBonus, isCasting, isNext, isSelected, castFraction);
        }

        private static FightViewModel BuildFight(RunScreenController controller, RunFightSession session)
        {
            var heroCast = session.HeroCast;
            var hero = new CombatantViewModel(
                "Hero",
                session.HeroHealth,
                session.HeroMaxHealth,
                session.HeroShield,
                session.HeroHealth <= 0,
                CastText(heroCast),
                CastFraction(heroCast));

            var enemies = new List<CombatantViewModel>(session.EnemyCount);
            for (var i = 0; i < session.EnemyCount; i++)
            {
                var cast = session.EnemyCast(i);
                enemies.Add(new CombatantViewModel(
                    session.Encounter.Enemies[i].Id,
                    session.EnemyHealth(i),
                    session.EnemyMaxHealth(i),
                    session.EnemyShield(i),
                    session.EnemyHealth(i) <= 0,
                    CastText(cast),
                    CastFraction(cast)));
            }

            var speed = controller.Pacer.Speed;
            var canEdit = controller.CanEditLine;
            string hint;
            if (!session.LineEditsAllowed)
            {
                hint = "The line is fixed in this fight.";
            }
            else if (controller.SelectedLinePosition >= 0)
            {
                hint = "Pick another slot to move the card there, or a reserve card to swap.";
            }
            else if (controller.SelectedReserveIndex >= 0)
            {
                hint = "Pick the line slot to swap it with.";
            }
            else
            {
                hint = "Pick a line card, then a slot or a reserve card, to change the line now.";
            }

            return new FightViewModel(
                hero,
                enemies.AsReadOnly(),
                $"Tick {session.Tick}",
                $"Speed x{speed:0.##}",
                controller.Pacer.IsPaused,
                canEdit,
                hint);
        }

        private static string CastText(CastProgress cast)
        {
            return cast.IsCasting ? $"{cast.CardId} {cast.ElapsedTicks}/{cast.CastTime}" : string.Empty;
        }

        private static double CastFraction(CastProgress cast)
        {
            return cast.IsCasting && cast.CastTime > 0 ? (double)cast.ElapsedTicks / cast.CastTime : 0d;
        }

        private static string EncounterName(RunFightSession session)
        {
            switch (session.Step.Kind)
            {
                case RunStepKind.RegularFight:
                    return "regular fight";
                case RunStepKind.SecretRoom:
                    return "mini-boss";
                default:
                    return "professor";
            }
        }

        private static string StatusOutsideFight(RunScreenController controller)
        {
            switch (controller.Phase)
            {
                case RunScreenPhase.ChoosingStep:
                    return controller.Run.HasPendingChoice ? "Choose your level-up before the next fight." : "Choose the next step.";
                case RunScreenPhase.FightResult:
                    return "Fight over.";
                default:
                    return controller.Run.Outcome == RunOutcome.Victory ? "Biome cleared." : "The run is over.";
            }
        }

        private static FightResultViewModel BuildResult(RunScreenController controller)
        {
            var report = controller.LastReport;
            var run = controller.Run;
            var lines = new List<string>();
            string title;
            if (report == null)
            {
                title = "Run over";
            }
            else if (report.HeroWon)
            {
                title = run.Outcome == RunOutcome.Victory ? "Biome cleared!" : "Victory";
                lines.Add($"+{report.XpGained} XP");
                if (report.LevelsGained > 0)
                {
                    lines.Add($"Level up: now level {run.Level}");
                }

                foreach (var room in report.UnlockedRoomIds)
                {
                    lines.Add($"Secret room unlocked: {room}");
                }

                if (report.SecretRoomRewards != null)
                {
                    lines.Add("Mini-boss reward received");
                }
            }
            else
            {
                title = report.TimedOut ? "Out of time" : "Defeat";
                lines.Add("The run is over.");
            }

            string continueText;
            if (!run.IsInProgress)
            {
                continueText = "Continue";
            }
            else if (run.HasPendingChoice)
            {
                continueText = "Choose level-up";
            }
            else
            {
                continueText = "Next step";
            }

            return new FightResultViewModel(title, report != null && report.HeroWon, lines.AsReadOnly(), continueText);
        }
    }
}
