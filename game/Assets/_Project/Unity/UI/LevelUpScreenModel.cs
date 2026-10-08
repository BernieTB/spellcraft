using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Runs;
using Game.Core.Upgrades;

namespace Game.Unity.UI
{
    /// <summary>The player's decision on the level-up screen, ready to give to <c>Run.TakeLevelUpPackage</c>.</summary>
    public readonly struct LevelUpChoice
    {
        public LevelUpChoice(int packageIndex, int? replacedLinePosition)
        {
            PackageIndex = packageIndex;
            ReplacedLinePosition = replacedLinePosition;
        }

        /// <summary>Index in <see cref="LevelUpOffer.Packages"/>.</summary>
        public int PackageIndex { get; }

        /// <summary>Line position of the card the new card replaces, or null (end of the line, or reserve if full).</summary>
        public int? ReplacedLinePosition { get; }
    }

    /// <summary>What the screen shows for one offered package: the card and the passive upgrade.</summary>
    public sealed class LevelUpPackageDisplay
    {
        public LevelUpPackageDisplay(int index, string cardName, string cardSummary, string passiveText)
        {
            Index = index;
            CardName = cardName;
            CardSummary = cardSummary;
            PassiveText = passiveText;
        }

        public int Index { get; }

        public string CardName { get; }

        /// <summary>Cast time, effects and neighbour bonuses of the card, one line each.</summary>
        public string CardSummary { get; }

        /// <summary>What the passive upgrade changes ("+5 max health").</summary>
        public string PassiveText { get; }
    }

    /// <summary>A card of the spell line that the new card can replace.</summary>
    public sealed class LevelUpLineCardDisplay
    {
        public LevelUpLineCardDisplay(int position, string cardName)
        {
            Position = position;
            CardName = cardName;
        }

        /// <summary>Position in the line, from zero (the view shows it from 1).</summary>
        public int Position { get; }

        public string CardName { get; }
    }

    /// <summary>
    /// View-model of the level-up choice screen (#77, ADR 0012): plain C#, no Unity type, so it is testable. It
    /// describes the offered packages, tracks the selection and, when the spell line is full, whether the new card
    /// goes to the reserve or replaces a line card. It makes no rule: Core validates the choice when it is applied
    /// (<c>Run.TakeLevelUpPackage</c>); the model only mirrors the ADR 0009 / 0012 options the player has.
    /// </summary>
    /// <remarks>Texts are English placeholders (localisation is not decided).</remarks>
    public sealed class LevelUpScreenModel
    {
        private readonly int _packageCount;

        /// <param name="offer">The offer from <c>Run.GetLevelUpOffer</c>.</param>
        /// <param name="line">The cards of the spell line, in order.</param>
        /// <param name="lineCapacity">Capacity of the spell line.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public LevelUpScreenModel(LevelUpOffer offer, IReadOnlyList<CardInstance> line, int lineCapacity)
        {
            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            Packages = new ReadOnlyCollection<LevelUpPackageDisplay>(
                offer.Packages.Select((package, index) => Describe(index, package)).ToList());
            LineCards = new ReadOnlyCollection<LevelUpLineCardDisplay>(
                line.Select((card, position) => new LevelUpLineCardDisplay(position, card.CurrentDefinition.Id)).ToList());
            LineIsFull = line.Count >= lineCapacity;
            _packageCount = Packages.Count;
        }

        /// <summary>Raised after the selection changed.</summary>
        public event Action Changed;

        public IReadOnlyList<LevelUpPackageDisplay> Packages { get; }

        public IReadOnlyList<LevelUpLineCardDisplay> LineCards { get; }

        /// <summary>True when there is no free slot: the player then chooses reserve or replacement.</summary>
        public bool LineIsFull { get; }

        /// <summary>Index of the selected package, or null.</summary>
        public int? SelectedPackage { get; private set; }

        /// <summary>Line position the new card replaces, or null (reserve when the line is full).</summary>
        public int? SelectedReplacement { get; private set; }

        public bool CanConfirm => SelectedPackage.HasValue;

        /// <summary>Where the selected card goes, as text for the screen.</summary>
        public string DestinationText
        {
            get
            {
                if (!LineIsFull)
                {
                    return "The card goes to the end of your spell line.";
                }

                return SelectedReplacement.HasValue
                    ? $"The card replaces slot {SelectedReplacement.Value + 1}; the replaced card goes to the reserve."
                    : "The card goes to the reserve.";
            }
        }

        /// <exception cref="ArgumentOutOfRangeException">Not a package index.</exception>
        public void SelectPackage(int index)
        {
            if (index < 0 || index >= _packageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Not a package of the offer.");
            }

            SelectedPackage = index;
            Changed?.Invoke();
        }

        /// <summary>Sends the new card to the reserve (full line only; the default).</summary>
        /// <exception cref="InvalidOperationException">The line is not full.</exception>
        public void SelectReserve()
        {
            RequireFullLine();
            SelectedReplacement = null;
            Changed?.Invoke();
        }

        /// <summary>Makes the new card replace the line card at <paramref name="position"/> (full line only).</summary>
        /// <exception cref="InvalidOperationException">The line is not full.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Not a position of the line.</exception>
        public void SelectReplacement(int position)
        {
            RequireFullLine();
            if (position < 0 || position >= LineCards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "Not a position of the spell line.");
            }

            SelectedReplacement = position;
            Changed?.Invoke();
        }

        /// <exception cref="InvalidOperationException">No package is selected.</exception>
        public LevelUpChoice Confirm()
        {
            if (!SelectedPackage.HasValue)
            {
                throw new InvalidOperationException("Select a package first.");
            }

            return new LevelUpChoice(SelectedPackage.Value, LineIsFull ? SelectedReplacement : null);
        }

        private void RequireFullLine()
        {
            if (!LineIsFull)
            {
                throw new InvalidOperationException("The spell line has a free slot: the card is added at its end.");
            }
        }

        private static LevelUpPackageDisplay Describe(int index, LevelUpPackage package)
        {
            return new LevelUpPackageDisplay(
                index, package.Card.Id, DescribeCard(package.Card), DescribePassive(package.Passive));
        }

        private static string DescribeCard(CardDefinition card)
        {
            var text = new StringBuilder($"Cast time: {card.CastTime}");
            foreach (var effect in card.Effects)
            {
                text.Append('\n').Append(effect is IAmountEffect amount
                    ? DescribeAmount(amount.Kind, amount.Amount)
                    : effect.GetType().Name);
            }

            foreach (var modifier in card.NeighbourModifiers)
            {
                var target = modifier.Direction == NeighbourDirection.Next ? "next" : "previous";
                text.Append($"\nGives the {target} card +{modifier.Amount} {KindWord(modifier.Kind)}");
            }

            return text.ToString();
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

        private static string KindWord(BonusKind kind)
        {
            return kind == BonusKind.Damage ? "damage" : kind == BonusKind.Heal ? "healing" : "shield";
        }

        private static string DescribePassive(PassiveUpgrade passive)
        {
            switch (passive.Kind)
            {
                case PassiveUpgradeKind.MaxHealth:
                    return $"+{passive.Amount} max health";
                case PassiveUpgradeKind.StartingShield:
                    return $"+{passive.Amount} starting shield each fight";
                case PassiveUpgradeKind.EffectAmount:
                    return $"+{passive.Amount} to every {KindWord(passive.EffectKind)} effect you cast";
                default:
                    return $"+{passive.Amount} to the neighbour bonuses your cards give";
            }
        }
    }
}
