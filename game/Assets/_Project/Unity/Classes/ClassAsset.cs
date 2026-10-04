using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Unity.Cards;
using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Classes
{
    /// <summary>
    /// Authoring asset for a playable class: the hero's base stats, the starting spell line and the card pool of
    /// level-up offers (ADR 0007, ADR 0009, ADR 0012). <see cref="ToDefinition"/> converts it to a Core
    /// <see cref="ClassDefinition"/>. Every value comes from the asset; ids are placeholders until the owner writes
    /// content.
    /// </summary>
    [CreateAssetMenu(fileName = "NewClass", menuName = "Game/Class")]
    public sealed class ClassAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among classes (test ids such as test_class_01; the MVP class's working name is CLASS_A).")]
        private string _id;

        [SerializeField]
        [Min(1)]
        [Tooltip("The hero's max health at the start of every fight.")]
        private int _maxHealth = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("The hero's shield at the start of every fight.")]
        private int _startingShield;

        [SerializeField]
        [Min(1)]
        [Tooltip("Number of spell line slots at the start of a run. The starting deck must fit in it.")]
        private int _startingLineCapacity = 1;

        [SerializeField]
        [Tooltip("The starting cards, in line order. They form the starting spell line. At least one.")]
        private List<CardAsset> _startingDeck = new List<CardAsset>();

        [SerializeField]
        [Tooltip("Cards that level-up offers can draw from. The same card may appear twice.")]
        private List<CardAsset> _cardPool = new List<CardAsset>();

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <summary>Stable identifier of the class.</summary>
        public string Id => _id;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>Converts the asset to an immutable Core <see cref="ClassDefinition"/>.</summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public ClassDefinition ToDefinition()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    throw new InvalidOperationException("The class has no id.");
                }

                if (_startingDeck.Count == 0)
                {
                    throw new InvalidOperationException("The class's starting deck has no card.");
                }

                return new ClassDefinition(
                    _id,
                    _maxHealth,
                    _startingShield,
                    _startingLineCapacity,
                    ConvertCards(_startingDeck, "starting deck"),
                    ConvertCards(_cardPool, "card pool"));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidOperationException($"Class asset '{name}' is invalid: {exception.Message}", exception);
            }
        }

        private static List<CardDefinition> ConvertCards(List<CardAsset> cards, string label)
        {
            var definitions = new List<CardDefinition>(cards.Count);
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                {
                    throw new InvalidOperationException($"The class's {label} has an empty slot at position {i}.");
                }

                definitions.Add(card.ToDefinition());
            }

            return definitions;
        }
    }
}
