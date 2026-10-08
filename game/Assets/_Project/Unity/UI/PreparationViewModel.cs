using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Enemies;
using Game.Core.Runs;

namespace Game.Unity.UI
{
    /// <summary>One card shown on the preparation screen.</summary>
    public readonly struct PreparationCard
    {
        public PreparationCard(string id, int castTime, int stage)
        {
            Id = id;
            CastTime = castTime;
            Stage = stage;
        }

        /// <summary>Id of the card's definition.</summary>
        public string Id { get; }

        /// <summary>Cast time in ticks.</summary>
        public int CastTime { get; }

        /// <summary>Evolution stage reached by this copy.</summary>
        public int Stage { get; }
    }

    /// <summary>What the player may know about one enemy of the fight: a null value or a null card is unknown.</summary>
    public sealed class PreparationEnemyInfo
    {
        public PreparationEnemyInfo(string id, int? maxHealth, int? shield, IReadOnlyList<string> cardIds)
        {
            Id = id;
            MaxHealth = maxHealth;
            Shield = shield;
            CardIds = cardIds;
        }

        public string Id { get; }

        /// <summary>Max health, or null while unknown.</summary>
        public int? MaxHealth { get; }

        /// <summary>Starting shield, or null while unknown.</summary>
        public int? Shield { get; }

        /// <summary>One entry per card of the enemy line; an entry is null while that card is unknown.</summary>
        public IReadOnlyList<string> CardIds { get; }
    }

    /// <summary>
    /// The state and the actions of the preparation screen (#84, ADR 0014), between <see cref="BossPreparation"/>
    /// (the rules, in Core) and <see cref="PreparationScreenView"/> (only displays and forwards clicks). It owns
    /// what is only about the screen: which line card and which reserve card are selected. Every action is guarded
    /// by a <c>Can...</c> property and does nothing when it is false, so a view never has to know a rule.
    /// </summary>
    public sealed class PreparationViewModel
    {
        private readonly BossPreparation _preparation;

        /// <exception cref="ArgumentNullException"><paramref name="preparation"/> is null.</exception>
        public PreparationViewModel(BossPreparation preparation)
        {
            _preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
        }

        /// <summary>Raised after anything the screen shows changed.</summary>
        public event Action Changed;

        /// <summary>True for the professor, false for a mini-boss.</summary>
        public bool IsProfessor => _preparation.Step.Kind == RunStepKind.Professor;

        /// <summary>True once the fight has started: nothing can change any more.</summary>
        public bool IsStarted => _preparation.IsStarted;

        /// <summary>Position of the selected line card, or null.</summary>
        public int? SelectedLine { get; private set; }

        /// <summary>Index of the selected reserve card, or null.</summary>
        public int? SelectedReserve { get; private set; }

        /// <summary>How many cards the line can hold.</summary>
        public int LineCapacity => _preparation.LineCapacity;

        public IReadOnlyList<PreparationCard> Line => _preparation.Line.Select(ToCard).ToList();

        public IReadOnlyList<PreparationCard> Reserve => _preparation.Reserve.Select(ToCard).ToList();

        /// <summary>What the player knows of the enemies; the professor hidden parts are null.</summary>
        public IReadOnlyList<PreparationEnemyInfo> Enemies
        {
            get
            {
                var knowledge = _preparation.ProfessorKnowledge;
                if (knowledge != null)
                {
                    return new[]
                    {
                        new PreparationEnemyInfo(
                            knowledge.ProfessorId,
                            knowledge.MaxHealth,
                            knowledge.Shield,
                            knowledge.SpellLine.Select(card => card?.Id).ToList()),
                    };
                }

                return _preparation.MiniBossEncounter.Enemies.Select(Known).ToList();
            }
        }

        public bool CanMoveLeft => CanEdit && SelectedLine > 0;

        public bool CanMoveRight => CanEdit && SelectedLine < _preparation.Line.Count - 1;

        /// <summary>The line keeps at least one card.</summary>
        public bool CanMoveToReserve => CanEdit && SelectedLine.HasValue && _preparation.Line.Count > 1;

        public bool CanMoveToLine =>
            CanEdit && SelectedReserve.HasValue && _preparation.Line.Count < _preparation.LineCapacity;

        public bool CanSwap => CanEdit && SelectedLine.HasValue && SelectedReserve.HasValue;

        public bool CanStart => !IsStarted;

        private bool CanEdit => !IsStarted;

        /// <summary>Selects a line card, or unselects it when it already is. Out of range does nothing.</summary>
        public void SelectLine(int position)
        {
            if (IsStarted || position < 0 || position >= _preparation.Line.Count)
            {
                return;
            }

            SelectedLine = SelectedLine == position ? (int?)null : position;
            Changed?.Invoke();
        }

        /// <summary>Selects a reserve card, or unselects it when it already is. Out of range does nothing.</summary>
        public void SelectReserve(int index)
        {
            if (IsStarted || index < 0 || index >= _preparation.Reserve.Count)
            {
                return;
            }

            SelectedReserve = SelectedReserve == index ? (int?)null : index;
            Changed?.Invoke();
        }

        /// <summary>Moves the selected line card one position towards the start of the line.</summary>
        public void MoveLeft()
        {
            if (CanMoveLeft)
            {
                MoveSelected(SelectedLine.Value - 1);
            }
        }

        /// <summary>Moves the selected line card one position towards the end of the line.</summary>
        public void MoveRight()
        {
            if (CanMoveRight)
            {
                MoveSelected(SelectedLine.Value + 1);
            }
        }

        /// <summary>Exchanges the selected line card with the selected reserve card; both stay selected.</summary>
        public void Swap()
        {
            if (!CanSwap)
            {
                return;
            }

            _preparation.SwapWithReserve(SelectedLine.Value, SelectedReserve.Value);
            Changed?.Invoke();
        }

        /// <summary>Sends the selected line card to the end of the reserve.</summary>
        public void MoveToReserve()
        {
            if (!CanMoveToReserve)
            {
                return;
            }

            _preparation.MoveToReserve(SelectedLine.Value);
            SelectedLine = null;
            SelectedReserve = null;
            Changed?.Invoke();
        }

        /// <summary>Puts the selected reserve card at the end of the line.</summary>
        public void MoveToLine()
        {
            if (!CanMoveToLine)
            {
                return;
            }

            _preparation.MoveFromReserve(SelectedReserve.Value);
            SelectedReserve = null;
            Changed?.Invoke();
        }

        /// <summary>
        /// Begins the fight with the line as arranged; the line is fixed afterwards. Returns null if the fight
        /// had already started.
        /// </summary>
        public RunFightSession Start()
        {
            if (!CanStart)
            {
                return null;
            }

            var session = _preparation.Start();
            SelectedLine = null;
            SelectedReserve = null;
            Changed?.Invoke();
            return session;
        }

        private void MoveSelected(int target)
        {
            _preparation.MoveInLine(SelectedLine.Value, target);
            SelectedLine = target;
            Changed?.Invoke();
        }

        private static PreparationCard ToCard(CardInstance card)
        {
            return new PreparationCard(card.Definition.Id, card.Definition.CastTime, card.Stage);
        }

        private static PreparationEnemyInfo Known(EnemyDefinition enemy)
        {
            return new PreparationEnemyInfo(
                enemy.Id, enemy.MaxHealth, enemy.Shield, enemy.SpellLine.Select(card => card.Id).ToList());
        }
    }
}
