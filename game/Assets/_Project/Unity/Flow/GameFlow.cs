using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Combat.Recap;
using Game.Core.Meta;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.UI;
using Game.Unity.UI.RunScreen;

namespace Game.Unity.Flow
{
    /// <summary>
    /// The state machine that chains the screens of the game (#88): title, run, level-up choice, preparation, recap,
    /// end of run, back to the title. Plain C# with no Unity type and no rule of its own: what may happen comes
    /// from <see cref="Core.Runs.Run"/> (pending level-up, available steps, outcome) and
    /// <see cref="RunScreenController"/>; this class only decides which screen follows which event, and keeps the
    /// bestiary (loaded by the caller, saved after every reveal, ADR 0014).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A thin presenter (<see cref="GameFlowPresenter"/>) shows <see cref="Screen"/> through the screen host each time
    /// <see cref="ScreenChanged"/> is raised, and calls the methods of this class from the player's clicks. The event
    /// is raised for every transition, also from a screen to a new instance of itself (a second level-up in a row).
    /// </para>
    /// <para>
    /// The seed of a run comes from the caller (<c>seedSource</c>): the game uses the clock, the one non-deterministic
    /// source allowed, outside Core. The run is then fully determined by that seed and the player's choices.
    /// </para>
    /// </remarks>
    public sealed class GameFlow : IDisposable
    {
        private readonly Func<ulong, Run> _createRun;
        private readonly IReadOnlyList<PassiveUpgrade> _passivePool;
        private readonly IBestiaryStore _store;
        private readonly Func<ulong> _seedSource;
        private readonly Action<string> _warn;
        private readonly RunScreenSettings _settings;
        private PreparationViewModel _preparation;
        private bool _disposed;

        /// <param name="createRun">Creates the run for a seed (class, biome and rules come from the game's content).</param>
        /// <param name="passivePool">The passive upgrades the level-up offers draw from.</param>
        /// <param name="bestiary">What the player knows, as loaded at startup. Kept between runs.</param>
        /// <param name="store">Where the bestiary is saved after every reveal. May be null (nothing is saved).</param>
        /// <param name="seedSource">Gives the seed of each new run.</param>
        /// <param name="warn">Receives a message when the bestiary cannot be saved. May be null.</param>
        /// <param name="settings">Pacing of the fights; null for the defaults.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        public GameFlow(
            Func<ulong, Run> createRun,
            IReadOnlyList<PassiveUpgrade> passivePool,
            Bestiary bestiary,
            IBestiaryStore store,
            Func<ulong> seedSource,
            Action<string> warn = null,
            RunScreenSettings settings = null)
        {
            _createRun = createRun ?? throw new ArgumentNullException(nameof(createRun));
            _passivePool = passivePool ?? throw new ArgumentNullException(nameof(passivePool));
            Bestiary = bestiary ?? throw new ArgumentNullException(nameof(bestiary));
            _store = store;
            _seedSource = seedSource ?? throw new ArgumentNullException(nameof(seedSource));
            _warn = warn;
            _settings = settings;
        }

        /// <summary>Raised after every change of <see cref="Screen"/>, including a screen replaced by a new one of the same kind.</summary>
        public event Action ScreenChanged;

        /// <summary>Raised when the player asks to quit the game from the title screen.</summary>
        public event Action QuitRequested;

        /// <summary>The screen to show.</summary>
        public GameScreen Screen { get; private set; } = GameScreen.Title;

        /// <summary>What the player knows about the professors; lives as long as the flow, across runs.</summary>
        public Bestiary Bestiary { get; }

        /// <summary>The run being played, or null on the title screen.</summary>
        public Run Run { get; private set; }

        /// <summary>The controller of the run screen for <see cref="Run"/>, or null on the title screen.</summary>
        public RunScreenController RunController { get; private set; }

        /// <summary>The level-up choice to show (<see cref="GameScreen.LevelUp"/>), otherwise null.</summary>
        public LevelUpScreenModel LevelUp { get; private set; }

        /// <summary>The preparation to show (<see cref="GameScreen.Preparation"/>), otherwise null.</summary>
        public PreparationViewModel Preparation => Screen == GameScreen.Preparation ? _preparation : null;

        /// <summary>The report of the fight that just ended, from the end of the fight until the next fight starts.</summary>
        public RunFightReport LastReport { get; private set; }

        /// <summary>The recap of <see cref="LastReport"/> (<see cref="GameScreen.Recap"/>), otherwise null.</summary>
        public FightRecap Recap { get; private set; }

        /// <summary>Whether the last attempt to save the bestiary failed (the message went to the warning callback).</summary>
        public bool LastSaveFailed { get; private set; }

        /// <summary>Starts a new run with the seed of the seed source and shows the run screen.</summary>
        /// <exception cref="InvalidOperationException">The flow is not on the title screen.</exception>
        public void StartRun()
        {
            Require(GameScreen.Title);
            var run = _createRun(_seedSource());
            Run = run;
            AttachController(new RunScreenController(run, _settings));
            SetScreen(GameScreen.Run);
        }

        /// <summary>Gives the player's level-up choice to the run; shows the next pending one, or the run screen.</summary>
        /// <exception cref="InvalidOperationException">The flow is not on the level-up screen.</exception>
        public void TakeLevelUp(LevelUpChoice choice)
        {
            Require(GameScreen.LevelUp);
            Run.TakeLevelUpPackage(choice.PackageIndex, choice.ReplacedLinePosition);
            RunController.ChoiceResolved();
            if (Run.HasPendingChoice)
            {
                OpenLevelUp();
                return;
            }

            LevelUp = null;
            SetScreen(GameScreen.Run);
        }

        /// <summary>
        /// Tells the flow the player started the fight from the preparation screen (the view-model's
        /// <see cref="PreparationViewModel.Start"/> was called): the run screen takes the fight over.
        /// </summary>
        /// <exception cref="InvalidOperationException">The flow is not on the preparation screen, or the fight did not start.</exception>
        public void PreparationStarted()
        {
            Require(GameScreen.Preparation);
            if (Run.CurrentFight == null)
            {
                throw new InvalidOperationException("The preparation did not start a fight.");
            }

            _preparation = null;
            AttachController(new RunScreenController(Run, _settings));
            SetScreen(GameScreen.Run);
        }

        /// <summary>
        /// Leaves the recap: the end of the run if the run is over, the level-up choice if one waits, otherwise the
        /// run screen.
        /// </summary>
        /// <exception cref="InvalidOperationException">The flow is not on the recap screen.</exception>
        public void ContinueFromRecap()
        {
            Require(GameScreen.Recap);
            Recap = null;
            RunController.Continue();
            if (Screen == GameScreen.Recap)
            {
                SetScreen(GameScreen.Run);
            }
        }

        /// <summary>Leaves the end of the run for the title screen; the bestiary is kept.</summary>
        /// <exception cref="InvalidOperationException">The flow is not on the end screen.</exception>
        public void ReturnToTitle()
        {
            Require(GameScreen.RunEnd);
            DetachController();
            Run = null;
            LastReport = null;
            SetScreen(GameScreen.Title);
        }

        /// <summary>Asks the host to quit the game.</summary>
        /// <exception cref="InvalidOperationException">The flow is not on the title screen.</exception>
        public void Quit()
        {
            Require(GameScreen.Title);
            QuitRequested?.Invoke();
        }

        /// <summary>Releases the run screen controller (cancels a fight in progress).</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            DetachController();
        }

        private void Require(GameScreen expected)
        {
            if (Screen != expected)
            {
                throw new InvalidOperationException($"This is only possible on the {expected} screen, not on {Screen}.");
            }
        }

        private void SetScreen(GameScreen screen)
        {
            Screen = screen;
            ScreenChanged?.Invoke();
        }

        private void AttachController(RunScreenController controller)
        {
            DetachController();
            RunController = controller;
            controller.PendingChoiceRequested += OnPendingChoiceRequested;
            controller.PreparationRequested += OnPreparationRequested;
            controller.FightCompleted += OnFightCompleted;
            controller.RunEnded += OnRunEnded;
        }

        private void DetachController()
        {
            var controller = RunController;
            if (controller == null)
            {
                return;
            }

            RunController = null;
            controller.PendingChoiceRequested -= OnPendingChoiceRequested;
            controller.PreparationRequested -= OnPreparationRequested;
            controller.FightCompleted -= OnFightCompleted;
            controller.RunEnded -= OnRunEnded;
            controller.Dispose();
        }

        private void OnPendingChoiceRequested()
        {
            if (Run.HasPendingChoice)
            {
                OpenLevelUp();
            }
        }

        private void OpenLevelUp()
        {
            var offer = Run.GetLevelUpOffer(_passivePool);
            LevelUp = new LevelUpScreenModel(offer, Run.Line, Run.LineCapacity);
            SetScreen(GameScreen.LevelUp);
        }

        private void OnPreparationRequested(RunStep step)
        {
            _preparation = new PreparationViewModel(Run.BeginPreparation(step, Bestiary));
            SetScreen(GameScreen.Preparation);
        }

        private void OnFightCompleted(RunFightReport report)
        {
            LastReport = report;
            Recap = FightRecapBuilder.Build(report.Log);
            if (report.SecretRoomRewards != null && report.SecretRoomRewards.RevealTo(Bestiary))
            {
                SaveBestiary();
            }

            SetScreen(GameScreen.Recap);
        }

        private void OnRunEnded(RunOutcome outcome)
        {
            SetScreen(GameScreen.RunEnd);
        }

        private void SaveBestiary()
        {
            LastSaveFailed = false;
            if (_store == null)
            {
                return;
            }

            try
            {
                BestiaryStorage.Save(_store, Bestiary);
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException || exception is InvalidOperationException)
            {
                LastSaveFailed = true;
                _warn?.Invoke("[Bestiary] The bestiary could not be saved: " + exception.Message);
            }
        }
    }
}
