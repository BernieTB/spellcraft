using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Effects;

namespace Game.Core.Cards
{
    /// <summary>
    /// Immutable description of a card, built from data: a stable id, a cast time, an ordered list of effects
    /// and the neighbour modifiers it grants. The card's word is not modelled yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A card can have up to <see cref="MaxEvolutions"/> evolution stages (<see cref="CardEvolution"/>,
    /// <c>docs/adr/0013-card-evolution.md</c>). Each stage replaces the effects and neighbour modifiers; the id and
    /// the cast time never change. A definition built from data is the card at stage 0; <see cref="AtStage"/> gives
    /// the card at another stage, with the same id and cast time. Every stage knows the same list of
    /// <see cref="Evolutions"/>, so the card at any stage can tell which stage a number of casts reaches
    /// (<see cref="StageForCasts"/>). Counting the casts of a card copy is not done here: a run keeps the count of
    /// each copy, and a fight counts the casts of the hero's cards.
    /// </para>
    /// </remarks>
    public sealed class CardDefinition
    {
        /// <summary>
        /// Most evolution stages a card can have: two (<c>docs/adr/0013-card-evolution.md</c>). A card is a base form
        /// plus up to this many evolved forms.
        /// </summary>
        public const int MaxEvolutions = 2;

        private readonly CardDefinition[] _forms;

        /// <summary>Creates a card with no neighbour modifiers.</summary>
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="effects"/> or one of its items is null.</exception>
        public CardDefinition(string id, int castTime, IEnumerable<IEffect> effects)
            : this(id, castTime, effects, Array.Empty<NeighbourModifier>())
        {
        }

        /// <summary>Creates a card that grants neighbour modifiers.</summary>
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <param name="neighbourModifiers">
        /// Bonuses granted to neighbours each time the card resolves, applied by the combat loop. May be empty.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effects"/>, <paramref name="neighbourModifiers"/> or one of their items is null.
        /// </exception>
        public CardDefinition(
            string id,
            int castTime,
            IEnumerable<IEffect> effects,
            IEnumerable<NeighbourModifier> neighbourModifiers)
            : this(id, castTime, effects, neighbourModifiers, Array.Empty<CardEvolution>())
        {
        }

        /// <summary>Creates a card that grants neighbour modifiers and can evolve.</summary>
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <param name="neighbourModifiers">
        /// Bonuses granted to neighbours each time the card resolves, applied by the combat loop. May be empty.
        /// </param>
        /// <param name="evolutions">
        /// The evolution stages, in order, at most <see cref="MaxEvolutions"/>; each needs strictly more casts than
        /// the previous one. May be empty (the card does not evolve).
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, there are more than <see cref="MaxEvolutions"/>
        /// evolutions, or their casts required do not strictly increase.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effects"/>, <paramref name="neighbourModifiers"/>, <paramref name="evolutions"/> or one of
        /// their items is null.
        /// </exception>
        public CardDefinition(
            string id,
            int castTime,
            IEnumerable<IEffect> effects,
            IEnumerable<NeighbourModifier> neighbourModifiers,
            IEnumerable<CardEvolution> evolutions)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id cannot be null, empty or whitespace.", nameof(id));
            }

            if (castTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(castTime), castTime, "Cast time must be greater than zero ticks.");
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var copy = new List<IEffect>(effects);
            if (copy.Contains(null))
            {
                throw new ArgumentNullException(nameof(effects), "A card cannot contain a null effect.");
            }

            if (neighbourModifiers == null)
            {
                throw new ArgumentNullException(nameof(neighbourModifiers));
            }

            var modifiers = new List<NeighbourModifier>(neighbourModifiers);
            if (modifiers.Contains(null))
            {
                throw new ArgumentNullException(nameof(neighbourModifiers), "A card cannot contain a null neighbour modifier.");
            }

            if (evolutions == null)
            {
                throw new ArgumentNullException(nameof(evolutions));
            }

            var stages = new List<CardEvolution>(evolutions);
            if (stages.Contains(null))
            {
                throw new ArgumentNullException(nameof(evolutions), "A card cannot contain a null evolution.");
            }

            if (stages.Count > MaxEvolutions)
            {
                throw new ArgumentException(
                    $"A card has at most {MaxEvolutions} evolution stages, not {stages.Count}.", nameof(evolutions));
            }

            for (var i = 1; i < stages.Count; i++)
            {
                if (stages[i].CastsRequired <= stages[i - 1].CastsRequired)
                {
                    throw new ArgumentException(
                        $"Evolution {i + 1} needs {stages[i].CastsRequired} casts, which is not more than evolution {i} "
                        + $"({stages[i - 1].CastsRequired}).",
                        nameof(evolutions));
                }
            }

            Id = id;
            CastTime = castTime;
            Effects = new ReadOnlyCollection<IEffect>(copy);
            NeighbourModifiers = new ReadOnlyCollection<NeighbourModifier>(modifiers);
            Evolutions = new ReadOnlyCollection<CardEvolution>(stages);

            // Every stage of the card shares this array, so the card at any stage reaches the others.
            _forms = new CardDefinition[stages.Count + 1];
            _forms[0] = this;
            Stage = 0;
            for (var i = 0; i < stages.Count; i++)
            {
                _forms[i + 1] = new CardDefinition(this, i + 1, stages[i]);
            }
        }

        // The card at an evolved stage: same id, cast time, evolutions and forms as the base card.
        private CardDefinition(CardDefinition baseCard, int stage, CardEvolution evolution)
        {
            Id = baseCard.Id;
            CastTime = baseCard.CastTime;
            Effects = evolution.Effects;
            NeighbourModifiers = evolution.NeighbourModifiers;
            Evolutions = baseCard.Evolutions;
            Stage = stage;
            _forms = baseCard._forms;
        }

        /// <summary>
        /// Stable identifier, unique among card definitions.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Simulation ticks needed to cast the card. Used by the combat loop, not by the card itself.
        /// </summary>
        public int CastTime { get; }

        /// <summary>
        /// Effects of the card, in resolution order.
        /// </summary>
        public IReadOnlyList<IEffect> Effects { get; }

        /// <summary>
        /// Bonuses this card grants to its neighbours in the spell line each time it resolves. Applied by the combat
        /// loop, not by <see cref="Resolve"/>: they belong to positions in a spell line, which a card does not know.
        /// </summary>
        public IReadOnlyList<NeighbourModifier> NeighbourModifiers { get; }

        /// <summary>
        /// The evolution stages of the card, in order (the same list at every stage). Empty when the card does not
        /// evolve.
        /// </summary>
        public IReadOnlyList<CardEvolution> Evolutions { get; }

        /// <summary>
        /// The stage this definition is at: 0 for the card as authored, up to <see cref="Evolutions"/>.Count for
        /// its last evolution.
        /// </summary>
        public int Stage { get; }

        /// <summary>
        /// The card at <paramref name="stage"/>: same id and cast time, with the effects and neighbour modifiers of
        /// that stage. Stage 0 is the card as authored.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="stage"/> is negative or greater than the number of <see cref="Evolutions"/>.
        /// </exception>
        public CardDefinition AtStage(int stage)
        {
            if (stage < 0 || stage >= _forms.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stage), stage, $"Card '{Id}' has stages 0 to {_forms.Length - 1}.");
            }

            return _forms[stage];
        }

        /// <summary>
        /// The stage a card copy has reached after <paramref name="casts"/> casts: the number of evolutions whose
        /// casts required are at most <paramref name="casts"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="casts"/> is negative.</exception>
        public int StageForCasts(int casts)
        {
            if (casts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(casts), casts, "Casts cannot be negative.");
            }

            var stage = 0;
            while (stage < Evolutions.Count && Evolutions[stage].CastsRequired <= casts)
            {
                stage++;
            }

            return stage;
        }

        /// <summary>
        /// Rebuilds the card with <paramref name="map"/> applied to the card at every stage, so a change to a card's
        /// numbers (for example a passive upgrade) also reaches its evolutions. Each stage is given to
        /// <paramref name="map"/> as a card of its own, without evolutions; the mapped card must keep the id and
        /// the cast time. The result is at the same stage as this card and keeps the same casts required.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is null, or returns null.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="map"/> changes the id or the cast time of a stage.
        /// </exception>
        public CardDefinition MapForms(Func<CardDefinition, CardDefinition> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            CardDefinition MapForm(CardDefinition form)
            {
                var flat = new CardDefinition(Id, CastTime, form.Effects, form.NeighbourModifiers);
                var mapped = map(flat) ?? throw new ArgumentNullException(nameof(map), "The map returned a null card.");
                if (mapped.Id != Id || mapped.CastTime != CastTime)
                {
                    throw new InvalidOperationException(
                        $"A card map cannot change the id or the cast time of '{Id}' (stage {form.Stage}).");
                }

                return mapped;
            }

            var baseForm = MapForm(_forms[0]);
            var stages = new List<CardEvolution>(Evolutions.Count);
            for (var i = 0; i < Evolutions.Count; i++)
            {
                var mapped = MapForm(_forms[i + 1]);
                stages.Add(new CardEvolution(Evolutions[i].CastsRequired, mapped.Effects, mapped.NeighbourModifiers));
            }

            var rebuilt = new CardDefinition(Id, CastTime, baseForm.Effects, baseForm.NeighbourModifiers, stages);
            return rebuilt.AtStage(Stage);
        }

        /// <summary>
        /// Applies every effect of the card, in order, to the combatants of <paramref name="context"/>.
        /// </summary>
        /// <returns>The outcome of each effect, in the same order as <see cref="Effects"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public IReadOnlyList<EffectOutcome> Resolve(EffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var outcomes = new EffectOutcome[Effects.Count];
            for (var i = 0; i < Effects.Count; i++)
            {
                outcomes[i] = Effects[i].Apply(context);
            }

            return new ReadOnlyCollection<EffectOutcome>(outcomes);
        }
    }
}
