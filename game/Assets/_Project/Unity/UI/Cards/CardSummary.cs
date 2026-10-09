using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Game.Core.Cards;
using Game.Core.Effects;

namespace Game.Unity.UI.Cards
{
    /// <summary>
    /// What a card shows wherever it is displayed (run screen line and reserve, preparation, recap, level-up, #123): a
    /// pure view-model, no Unity type and no rule, so every screen says the same thing and it is tested without a
    /// scene. It describes the card <b>at the stage it is given</b> (<see cref="CardDefinition.Stage"/>): pass
    /// <c>CardInstance.CurrentDefinition</c> so the amounts are the ones of the card's current evolution stage.
    /// </summary>
    /// <remarks>Texts are English placeholders (localisation is not decided).</remarks>
    public sealed class CardSummary
    {
        private CardSummary(CardDefinition card)
        {
            CardId = card.Id;
            Stage = card.Stage;
            CastTime = card.CastTime;

            var effects = new List<string>();
            foreach (var effect in card.Effects)
            {
                effects.Add(effect is IAmountEffect amount ? DescribeAmount(amount.Kind, amount.Amount) : effect.GetType().Name);
            }

            var full = new List<string>();
            var shortLines = new List<string>();
            foreach (var modifier in card.NeighbourModifiers)
            {
                var target = modifier.Direction == NeighbourDirection.Next ? "next" : "previous";
                var shortTarget = modifier.Direction == NeighbourDirection.Next ? "Next" : "Prev";
                full.Add($"Gives the {target} card +{modifier.Amount} {KindWord(modifier.Kind)}");
                shortLines.Add($"{shortTarget} card +{modifier.Amount} {KindWord(modifier.Kind)}");
            }

            EffectLines = new ReadOnlyCollection<string>(effects);
            NeighbourLines = new ReadOnlyCollection<string>(full);
            NeighbourShortLines = new ReadOnlyCollection<string>(shortLines);
        }

        /// <summary>Builds the summary of <paramref name="card"/> at its own stage.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is null.</exception>
        public static CardSummary From(CardDefinition card)
        {
            return new CardSummary(card ?? throw new ArgumentNullException(nameof(card)));
        }

        /// <summary>The card's id (cards have no display name yet).</summary>
        public string CardId { get; }

        /// <summary>Evolution stage described (0 = base form, ADR 0013).</summary>
        public int Stage { get; }

        /// <summary>Cast time in ticks.</summary>
        public int CastTime { get; }

        /// <summary>"Cast time: 3".</summary>
        public string CastTimeText => $"Cast time: {CastTime}";

        /// <summary>"Cast 3": the compact form for a slot.</summary>
        public string CastTimeShortText => $"Cast {CastTime}";

        /// <summary>The effects with their amounts at this stage, one line each ("Deal 4 damage"), in resolution order.</summary>
        public IReadOnlyList<string> EffectLines { get; }

        /// <summary>The neighbour bonuses the card gives, one line each ("Gives the next card +2 damage").</summary>
        public IReadOnlyList<string> NeighbourLines { get; }

        /// <summary>The neighbour bonuses in a compact form ("Next card +2 damage").</summary>
        public IReadOnlyList<string> NeighbourShortLines { get; }

        /// <summary>True when the card has neither an effect nor a neighbour bonus.</summary>
        public bool HasNothing => EffectLines.Count == 0 && NeighbourLines.Count == 0;

        /// <summary>"*" at stage 1, "**" at stage 2, empty at stage 0. Only a mark: no progress toward the next stage.</summary>
        public string StageMarkText => Stage > 0 ? new string('*', Stage) : string.Empty;

        public bool HasStageMark => Stage > 0;

        /// <summary>"Base form", "Evolved (stage 1)"...</summary>
        public string StageText => Stage == 0 ? "Base form" : $"Evolved (stage {Stage})";

        /// <summary>Cast time, effects and neighbour bonuses, one line each (the level-up screen's card text).</summary>
        public string DetailedText
        {
            get
            {
                var text = new StringBuilder(CastTimeText);
                foreach (var line in EffectLines)
                {
                    text.Append('\n').Append(line);
                }

                foreach (var line in NeighbourLines)
                {
                    text.Append('\n').Append(line);
                }

                return text.ToString();
            }
        }

        /// <summary>
        /// The compact lines of a slot: cast time first, then the effects, then the neighbour bonuses; "No direct
        /// effect" stands for a card that does nothing by itself and gives nothing.
        /// </summary>
        public IReadOnlyList<string> CompactLines
        {
            get
            {
                var lines = new List<string> { CastTimeShortText };
                lines.AddRange(EffectLines);
                lines.AddRange(NeighbourShortLines);
                if (HasNothing)
                {
                    lines.Add("No direct effect");
                }

                return lines.AsReadOnly();
            }
        }

        /// <summary>The compact lines in one text (one line each).</summary>
        public string CompactText => string.Join("\n", CompactLines);

        /// <summary>The full details shown on hover: id and stage, cast time, effects, neighbour bonuses and what they mean.</summary>
        public string TooltipText
        {
            get
            {
                var text = new StringBuilder();
                text.Append(CardId);
                if (HasStageMark)
                {
                    text.Append(' ').Append(StageMarkText);
                }

                text.Append('\n').Append(StageText);
                text.Append('\n').Append(CastTimeText).Append(" ticks");
                if (EffectLines.Count > 0)
                {
                    text.Append("\nWhen cast:");
                    foreach (var line in EffectLines)
                    {
                        text.Append("\n  ").Append(line);
                    }
                }

                if (NeighbourLines.Count > 0)
                {
                    text.Append("\nNeighbour bonus (each cast, used up by one cast of the neighbour):");
                    foreach (var line in NeighbourLines)
                    {
                        text.Append("\n  ").Append(line);
                    }
                }

                if (HasNothing)
                {
                    text.Append("\nNo direct effect.");
                }

                return text.ToString();
            }
        }

        internal static string KindWord(BonusKind kind)
        {
            return kind == BonusKind.Damage ? "damage" : kind == BonusKind.Heal ? "healing" : "shield";
        }

        private static string DescribeAmount(BonusKind kind, int amount)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return $"Deal {amount} damage";
                case BonusKind.Heal:
                    return $"Heal {amount}";
                default:
                    return $"Gain {amount} shield";
            }
        }
    }
}
