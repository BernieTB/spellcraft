namespace Game.Unity.Flow
{
    /// <summary>The screens of the game, in the order a run goes through them (#88).</summary>
    public enum GameScreen
    {
        /// <summary>The title screen: start a new run or quit.</summary>
        Title = 0,

        /// <summary>The run screen: choose the next step, watch the fight, edit the spell line.</summary>
        Run = 1,

        /// <summary>The level-up choice, mandatory before the next fight.</summary>
        LevelUp = 2,

        /// <summary>The preparation before a mini-boss or the professor.</summary>
        Preparation = 3,

        /// <summary>The recap of the fight that just ended.</summary>
        Recap = 4,

        /// <summary>The end of the run (victory or defeat), with the way back to the title.</summary>
        RunEnd = 5,
    }
}
