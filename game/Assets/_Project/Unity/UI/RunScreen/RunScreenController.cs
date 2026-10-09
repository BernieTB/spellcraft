using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Runs;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>
    /// The flow of the run screen (#73, #124), in plain C# so it is tested without a scene: it chains the regular
    /// fights by itself, drives the current fight tick by tick from Core at the pace of a <see cref="FightPacer"/>,
    /// passes the player's line edits to Core and hands over to other screens through events. It holds no game rule:
    /// availability of steps, pending choices, who may edit the line, and every fight outcome come from
    /// <see cref="Run"/> and <see cref="RunFightSession"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loop (ADR 0016): a won regular fight is followed, after a short pause
    /// (<see cref="RunScreenSettings.NextFightDelaySeconds"/>), by the next regular fight, with no click. A level-up
    /// stops the loop and raises <see cref="PendingChoiceRequested"/>; once the choice is taken
    /// (<see cref="ChoiceResolved"/>) the loop goes on. A secret room or the professor is asked for with
    /// <see cref="RequestStep"/> at any time; it is entered when the current fight ends (through
    /// <see cref="PreparationRequested"/>), otherwise the loop goes on. Pause also stops the loop's pause timer.
    /// A mini-boss, a professor fight or a defeat ends on <see cref="RunScreenPhase.FightResult"/>, for the recap.
    /// </para>
    /// <para>
    /// Entry points for the screens built next to this one (#77 level-up choice, #84 boss preparation): the events
    /// <see cref="PendingChoiceRequested"/> (a level-up is waiting: open the choice, then call
    /// <see cref="ChoiceResolved"/>) and <see cref="PreparationRequested"/> (the player picked a mini-boss or the
    /// professor: open the preparation, then call <see cref="StartFight"/>). <see cref="FightCompleted"/> and
    /// <see cref="RunEnded"/> hand over the report for the recap.
    /// </para>
    /// <para>
    /// A pending level-up is enforced here (<see cref="Run.HasPendingChoice"/>): no step can start while one waits.
    /// </para>
    /// <para>
    /// Leaving a fight that did not end (<see cref="Leave"/>, <see cref="Dispose"/>) calls
    /// <see cref="RunFightSession.Cancel"/>, as the session requires: an open session blocks the whole run.
    /// </para>
    /// </remarks>
    public sealed class RunScreenController : IDisposable
    {
        private RunFightSession _session;
        private int _selectedLine = -1;
        private int _selectedReserve = -1;
        private readonly double _nextFightDelay;
        private double _delayLeft;
        private bool _awaitingPreparation;

        /// <param name="run">The run to play. If it already has an open fight session, the screen takes it over.</param>
        /// <param name="settings">Pacing settings; null for the defaults.</param>
        /// <exception cref="ArgumentNullException"><paramref name="run"/> is null.</exception>
        public RunScreenController(Run run, RunScreenSettings settings = null)
        {
            Run = run ?? throw new ArgumentNullException(nameof(run));
            var used = settings ?? new RunScreenSettings();
            Pacer = new FightPacer(used.TicksPerSecond, used.Speeds);
            _nextFightDelay = used.NextFightDelaySeconds;
            _delayLeft = _nextFightDelay;
            _session = run.CurrentFight;
            Phase = _session != null
                ? RunScreenPhase.Fighting
                : run.IsInProgress ? RunScreenPhase.BetweenFights : RunScreenPhase.RunEnded;
        }

        /// <summary>Raised when a level-up waits at the end of a fight (or a step is tried): open the choice (#77).</summary>
        public event Action PendingChoiceRequested;

        /// <summary>Raised when a requested or picked mini-boss or professor is due: open the preparation (#84).</summary>
        public event Action<RunStep> PreparationRequested;

        /// <summary>Raised when a fight ends and was counted by the run, with its report (for the recap).</summary>
        public event Action<RunFightReport> FightCompleted;

        /// <summary>Raised once the player continues after the fight that ended the run.</summary>
        public event Action<RunOutcome> RunEnded;

        /// <summary>The run being played.</summary>
        public Run Run { get; }

        /// <summary>Real time to ticks, speed and pause.</summary>
        public FightPacer Pacer { get; }

        /// <summary>What the screen is doing now.</summary>
        public RunScreenPhase Phase { get; private set; }

        /// <summary>The open fight session while <see cref="Phase"/> is <see cref="RunScreenPhase.Fighting"/>, otherwise null.</summary>
        public RunFightSession Session => Phase == RunScreenPhase.Fighting ? _session : null;

        /// <summary>The report of the last fight that ended, or null before any.</summary>
        public RunFightReport LastReport { get; private set; }

        /// <summary>The selected line position (the next click on a slot or a reserve card moves or swaps it), or -1.</summary>
        public int SelectedLinePosition => _selectedLine;

        /// <summary>The selected reserve card index, or -1.</summary>
        public int SelectedReserveIndex => _selectedReserve;

        /// <summary>True when the player can rearrange the line now: a regular fight in progress.</summary>
        public bool CanEditLine => Session != null && Session.LineEditsAllowed && !Session.IsOver;

        /// <summary>True when a step can be started now (between fights, no level-up waiting, run going on).</summary>
        public bool CanStartStep => Phase == RunScreenPhase.BetweenFights && !Run.HasPendingChoice;

        /// <summary>The secret room or professor the player asked for, entered after the current fight; null if none.</summary>
        public RunStep PendingStep { get; private set; }

        /// <summary>
        /// Seconds (at speed x1) left before the next regular fight starts by itself, while
        /// <see cref="Phase"/> is <see cref="RunScreenPhase.BetweenFights"/>; zero otherwise.
        /// </summary>
        public double SecondsUntilNextFight => Phase == RunScreenPhase.BetweenFights ? Math.Max(0d, _delayLeft) : 0d;

        /// <summary>
        /// True while the loop is stopped by a level-up that waits for its choice or by a preparation that is open.
        /// </summary>
        public bool IsLoopHeld => Phase == RunScreenPhase.BetweenFights && (Run.HasPendingChoice || _awaitingPreparation);

        /// <summary>The next-step choices with their availability, in a fixed order.</summary>
        public IReadOnlyList<StepChoice> StepChoices
        {
            get
            {
                var choices = new List<StepChoice>();
                if ((Phase != RunScreenPhase.BetweenFights && Phase != RunScreenPhase.Fighting) || !Run.IsInProgress)
                {
                    return choices.AsReadOnly();
                }

                var blocked = Run.HasPendingChoice ? "Choose your level-up first." : null;
                foreach (var step in Run.AvailableSteps)
                {
                    choices.Add(new StepChoice(step, LabelOf(step), blocked == null, blocked, step.Equals(PendingStep)));
                }

                if (!Run.IsProfessorAvailable)
                {
                    var missing = Run.Biome.MinimumRegularFights - Run.RegularFightsWon;
                    choices.Add(new StepChoice(
                        RunStep.Professor,
                        LabelOf(RunStep.Professor),
                        false,
                        $"Win {missing} more regular {(missing == 1 ? "fight" : "fights")}."));
                }

                return choices.AsReadOnly();
            }
        }

        /// <summary>
        /// Starts the next step now, without waiting for the pause of the loop (used by tests and tools; the screen
        /// asks for rooms with <see cref="RequestStep"/>). A regular fight starts at once; a mini-boss or the professor raises
        /// <see cref="PreparationRequested"/> instead (the preparation screen then calls <see cref="StartFight"/>).
        /// While a level-up waits, raises <see cref="PendingChoiceRequested"/> and starts nothing.
        /// </summary>
        /// <returns>True when the step was accepted (started, or handed to the preparation).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        public bool ChooseStep(RunStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (Phase != RunScreenPhase.BetweenFights)
            {
                return false;
            }

            if (Run.HasPendingChoice)
            {
                PendingChoiceRequested?.Invoke();
                return false;
            }

            if (!Run.AvailableSteps.Contains(step))
            {
                return false;
            }

            if (step.RequiresPreparation)
            {
                RaisePreparation(step);
                return true;
            }

            StartFight(step);
            return true;
        }

        /// <summary>
        /// Asks to enter a secret room or the professor after the current fight (the player's button, always visible
        /// while the step is available). Asking again for the same step withdraws the request; asking for another
        /// replaces it. Between fights there is nothing to wait for: the preparation is requested at once (after the
        /// level-up choice if one waits). A regular fight cannot be requested: the loop chains them.
        /// </summary>
        /// <returns>True when the step is now requested or was handed to the preparation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        public bool RequestStep(RunStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (!step.RequiresPreparation
                || (Phase != RunScreenPhase.Fighting && Phase != RunScreenPhase.BetweenFights)
                || !Run.AvailableSteps.Contains(step))
            {
                return false;
            }

            if (Phase == RunScreenPhase.Fighting || Run.HasPendingChoice)
            {
                PendingStep = step.Equals(PendingStep) ? null : step;
                return PendingStep != null;
            }

            if (_awaitingPreparation)
            {
                return false;
            }

            RaisePreparation(step);
            return true;
        }

        /// <summary>Withdraws the request made with <see cref="RequestStep"/>, if any.</summary>
        public void CancelRequest()
        {
            PendingStep = null;
        }

        /// <summary>
        /// Starts the fight of a step, with no preparation: used for regular fights and, after the preparation
        /// screen, for mini-boss and professor fights.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The screen is not between fights, a level-up waits, or the step is not available.
        /// </exception>
        public void StartFight(RunStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (Phase != RunScreenPhase.BetweenFights)
            {
                throw new InvalidOperationException("A fight can only start between fights.");
            }

            if (Run.HasPendingChoice)
            {
                throw new InvalidOperationException("A level-up choice is pending: take it before the next fight.");
            }

            _session = Run.BeginFight(step);
            _awaitingPreparation = false;
            if (step.Equals(PendingStep))
            {
                PendingStep = null;
            }

            ClearSelection();
            Pacer.Reset();
            Phase = RunScreenPhase.Fighting;
        }

        /// <summary>
        /// Raises <see cref="PendingChoiceRequested"/> when a level-up waits and the screen is between fights.
        /// </summary>
        /// <returns>True when the event was raised.</returns>
        public bool OpenPendingChoiceIfAny()
        {
            if (Phase != RunScreenPhase.BetweenFights || !Run.HasPendingChoice)
            {
                return false;
            }

            PendingChoiceRequested?.Invoke();
            return true;
        }

        /// <summary>
        /// Tells the screen the pending level-up choice was taken, so the loop goes on: a requested room or professor
        /// is handed to the preparation, otherwise the pause timer runs again. Does nothing while another level-up
        /// still waits (the caller shows it).
        /// </summary>
        public void ChoiceResolved()
        {
            if (!Run.HasPendingChoice)
            {
                ResolveBetweenFights();
            }
        }

        /// <summary>
        /// Plays the ticks due after <paramref name="seconds"/> of real time. When the fight ends it is completed
        /// (counted by the run) and the loop goes on (see the class remarks). Between fights it counts down the pause
        /// before the next regular fight (faster with the speed; stopped by pause, a level-up or an open preparation).
        /// </summary>
        public void Advance(double seconds)
        {
            if (Phase == RunScreenPhase.BetweenFights)
            {
                AdvanceLoop(seconds);
                return;
            }

            if (Phase != RunScreenPhase.Fighting)
            {
                return;
            }

            var ticks = Math.Min(Pacer.TicksDue(seconds), RunScreenSettings.MaxTicksPerAdvance);
            for (var i = 0; i < ticks && !_session.IsOver; i++)
            {
                _session.Advance();
            }

            if (_session.IsOver)
            {
                Finish();
            }
        }

        /// <summary>Plays one tick regardless of pause and speed (step-by-step watching, tests).</summary>
        public void StepOneTick()
        {
            if (Phase != RunScreenPhase.Fighting)
            {
                return;
            }

            _session.Advance();
            if (_session.IsOver)
            {
                Finish();
            }
        }

        /// <summary>
        /// Leaves the fight without finishing it: the session is cancelled (the fight does not count, the line edits
        /// stay) and the screen goes back to the step choice.
        /// </summary>
        public void Leave()
        {
            if (Phase != RunScreenPhase.Fighting)
            {
                return;
            }

            CancelSession();
            Phase = RunScreenPhase.BetweenFights;
            _delayLeft = _nextFightDelay;

            // A room or the professor asked for during this fight is due now, not after another regular fight.
            ResolveBetweenFights();
        }

        /// <summary>
        /// Leaves the result of the last fight: ends the run screen if the run is over, otherwise the loop goes on
        /// (pending level-up choice, requested room or professor, or the next regular fight after the pause).
        /// </summary>
        public void Continue()
        {
            if (Phase != RunScreenPhase.FightResult)
            {
                return;
            }

            if (!Run.IsInProgress)
            {
                Phase = RunScreenPhase.RunEnded;
                RunEnded?.Invoke(Run.Outcome);
                return;
            }

            Phase = RunScreenPhase.BetweenFights;
            _delayLeft = _nextFightDelay;
            ResolveBetweenFights();
        }

        /// <summary>Cancels an open fight, if any. The screen calls it when it is closed (screen host hide callback).</summary>
        public void Dispose()
        {
            if (Phase == RunScreenPhase.Fighting)
            {
                CancelSession();
                Phase = RunScreenPhase.BetweenFights;
                _delayLeft = _nextFightDelay;
            }
        }

        /// <summary>Selects or acts on a line slot: moves the selected card there or swaps in the selected reserve card.</summary>
        public void ClickLineSlot(int position)
        {
            if (!CanEditLine)
            {
                return;
            }

            if (_selectedReserve >= 0)
            {
                _session.SwapWithReserve(position, _selectedReserve);
                ClearSelection();
            }
            else if (_selectedLine < 0)
            {
                _selectedLine = position;
            }
            else if (_selectedLine == position)
            {
                _selectedLine = -1;
            }
            else
            {
                _session.MoveCard(_selectedLine, position);
                ClearSelection();
            }
        }

        /// <summary>Selects or acts on a reserve card: swaps it with the selected line card.</summary>
        public void ClickReserveCard(int index)
        {
            if (!CanEditLine)
            {
                return;
            }

            if (_selectedLine >= 0)
            {
                _session.SwapWithReserve(_selectedLine, index);
                ClearSelection();
            }
            else
            {
                _selectedReserve = _selectedReserve == index ? -1 : index;
            }
        }

        /// <summary>Drops the current selection.</summary>
        public void ClearSelection()
        {
            _selectedLine = -1;
            _selectedReserve = -1;
        }

        /// <summary>Selects the next faster speed, or the slowest after the fastest.</summary>
        public void CycleSpeed()
        {
            Pacer.SpeedIndex = (Pacer.SpeedIndex + 1) % Pacer.Speeds.Length;
        }

        private void Finish()
        {
            var session = _session;
            _session = null;
            ClearSelection();
            LastReport = session.Complete();

            // A won regular fight chains into the next one; the others (mini-boss, professor, defeat) show the recap.
            var chains = Run.IsInProgress && LastReport.HeroWon && LastReport.Step.Kind == RunStepKind.RegularFight;
            Phase = chains ? RunScreenPhase.BetweenFights : RunScreenPhase.FightResult;
            _delayLeft = _nextFightDelay;
            if (!Run.IsInProgress)
            {
                PendingStep = null;
            }

            FightCompleted?.Invoke(LastReport);
            if (chains)
            {
                ResolveBetweenFights();
            }
        }

        // The loop is between two fights: a waiting level-up first, then a requested room or professor. Otherwise
        // nothing happens here: the pause timer starts the next regular fight.
        private void ResolveBetweenFights()
        {
            if (Phase != RunScreenPhase.BetweenFights || _awaitingPreparation)
            {
                return;
            }

            if (Run.HasPendingChoice)
            {
                PendingChoiceRequested?.Invoke();
                return;
            }

            var step = PendingStep;
            if (step == null)
            {
                return;
            }

            PendingStep = null;
            if (Run.AvailableSteps.Contains(step))
            {
                RaisePreparation(step);
            }
        }

        private void RaisePreparation(RunStep step)
        {
            _awaitingPreparation = true;
            PreparationRequested?.Invoke(step);
        }

        private void AdvanceLoop(double seconds)
        {
            if (Pacer.IsPaused || Run.HasPendingChoice || _awaitingPreparation || !Run.IsInProgress || Run.CurrentFight != null)
            {
                return;
            }

            _delayLeft -= seconds * Pacer.Speed;
            if (_delayLeft <= 0d)
            {
                StartFight(RunStep.RegularFight);
            }
        }

        private void CancelSession()
        {
            _session?.Cancel();
            _session = null;
            ClearSelection();
        }

        private string LabelOf(RunStep step)
        {
            switch (step.Kind)
            {
                case RunStepKind.RegularFight:
                    return "Regular fight";
                case RunStepKind.SecretRoom:
                    return $"Secret room: {step.SecretRoomId} (mini-boss)";
                default:
                    return $"Professor: {Run.Biome.Professor.Id}";
            }
        }
    }
}
