using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Meta;
using Game.Core.Runs;
using Game.Unity.Flow;
using Game.Unity.UI;

namespace Game.Unity.Tests.Flow
{
    /// <summary>An in-memory bestiary save.</summary>
    public sealed class MemoryBestiaryStore : IBestiaryStore
    {
        public string Text { get; private set; }

        public int Writes { get; private set; }

        public bool TryRead(out string text)
        {
            text = Text;
            return Text != null;
        }

        public void Write(string text)
        {
            Text = text;
            Writes++;
        }
    }

    /// <summary>
    /// Plays a <see cref="GameFlow"/> through its public methods only, as the player's clicks would: it asks for
    /// secret rooms not yet cleared, then the professor, and otherwise lets the regular fights chain, always takes the first level-up package and starts
    /// every preparation as it is.
    /// </summary>
    public static class FlowBot
    {
        /// <summary>Plays from the title screen until the flow is back on it. Returns the screens visited, in order.</summary>
        public static List<GameScreen> PlayRun(GameFlow flow, int maxActions = 20000, Action onScreen = null)
        {
            var visited = new List<GameScreen> { flow.Screen };
            flow.StartRun();
            for (var action = 0; action < maxActions; action++)
            {
                if (visited[visited.Count - 1] != flow.Screen)
                {
                    visited.Add(flow.Screen);
                }

                onScreen?.Invoke();
                if (flow.Screen == GameScreen.Title)
                {
                    return visited;
                }

                Act(flow);
            }

            throw new InvalidOperationException($"The run did not end in {maxActions} actions; screens: {string.Join(", ", visited.Take(40))}.");
        }

        private static void Act(GameFlow flow)
        {
            switch (flow.Screen)
            {
                case GameScreen.Run:
                    var controller = flow.RunController;
                    var wanted = PickStep(flow.Run);
                    if (controller.Phase == Game.Unity.UI.RunScreen.RunScreenPhase.Fighting)
                    {
                        // Asks for a room or the professor during the fight, as the always-visible buttons allow.
                        if (wanted.RequiresPreparation && controller.PendingStep == null)
                        {
                            controller.RequestStep(wanted);
                        }

                        controller.StepOneTick();
                        return;
                    }

                    if (wanted.RequiresPreparation)
                    {
                        controller.RequestStep(wanted);
                    }
                    else
                    {
                        // Lets the loop's pause run out: the next regular fight starts by itself.
                        controller.Advance(Game.Unity.UI.RunScreen.RunScreenSettings.DefaultNextFightDelaySeconds + 1d);
                    }

                    return;
                case GameScreen.LevelUp:
                    var model = flow.LevelUp;
                    model.SelectPackage(0);
                    if (model.LineIsFull)
                    {
                        model.SelectReserve();
                    }

                    flow.TakeLevelUp(model.Confirm());
                    return;
                case GameScreen.Preparation:
                    flow.Preparation.Start();
                    flow.PreparationStarted();
                    return;
                case GameScreen.Recap:
                    flow.ContinueFromRecap();
                    return;
                default:
                    flow.ReturnToTitle();
                    return;
            }
        }

        private static RunStep PickStep(Run run)
        {
            var steps = run.AvailableSteps;
            var rooms = run.SecretRooms;
            var cleared = rooms.Where(room => room.IsCleared).Select(room => room.Definition.Id).ToList();
            var room = steps.FirstOrDefault(step => step.Kind == RunStepKind.SecretRoom && !cleared.Contains(step.SecretRoomId));
            if (room != null)
            {
                return room;
            }

            // Keep fighting regular fights while a room is still locked, so the secret rooms are played before the professor.
            var roomStillLocked = rooms.Any(status => !status.IsUnlocked);
            return roomStillLocked
                ? RunStep.RegularFight
                : steps.FirstOrDefault(step => step.Kind == RunStepKind.Professor) ?? RunStep.RegularFight;
        }
    }
}
