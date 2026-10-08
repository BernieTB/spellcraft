namespace Game.Core.Combat
{
    /// <summary>
    /// Where a combatant is in its spell line during a fight advanced tick by tick: the card being cast and how far
    /// along it is, or the position that will be cast next. A read-only snapshot for the run screen (#73); it holds
    /// no rule.
    /// </summary>
    public readonly struct CastProgress
    {
        /// <param name="isCasting">True while a card is being cast.</param>
        /// <param name="position">The position being cast, or the position the line will cast next.</param>
        /// <param name="cardId">The id of the card being cast, or null when none is.</param>
        /// <param name="elapsedTicks">Ticks of the cast done so far. Zero when none is being cast.</param>
        /// <param name="castTime">The cast time of the card being cast. Zero when none is being cast.</param>
        public CastProgress(bool isCasting, int position, string cardId, int elapsedTicks, int castTime)
        {
            IsCasting = isCasting;
            Position = position;
            CardId = cardId;
            ElapsedTicks = elapsedTicks;
            CastTime = castTime;
        }

        /// <summary>True while a card is being cast (between two casts, nothing is).</summary>
        public bool IsCasting { get; }

        /// <summary>The line position being cast (the slot it left, if the card went to the reserve) or, between casts, the next one.</summary>
        public int Position { get; }

        /// <summary>The id of the card being cast, or null between casts.</summary>
        public string CardId { get; }

        /// <summary>Ticks of the current cast already simulated.</summary>
        public int ElapsedTicks { get; }

        /// <summary>The cast time of the card being cast.</summary>
        public int CastTime { get; }
    }
}
