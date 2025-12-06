using KnaveFSMSystem.Core;
using KnaveFSMSystem.Machines;

namespace KnaveFSMSystem.StateSystem.Abstracts
{
    public abstract class GameState : BaseState<PlayerControllerMachine, ButterFlyStates>
    {
        protected MidiGameManager MidiGameManager => Context.MidiGameManager;
    }
}