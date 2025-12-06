using KnaveFSMSystem.Core;
using KnaveFSMSystem.StateSystem.Abstracts;
using UnityEngine;

namespace KnaveFSMSystem.StateSystem.States
{
    [CreateAssetMenu(fileName = "none", menuName = "Knave/States/none")]
    public class none : GameState
    {
        public override ButterFlyStates Tag => ButterFlyStates.none;

        public override void EnterState()
        {
        }

        public override void FixedUpdateState()
        {

        }

        public override void UpdateState()
        {

        }

        public override void ExitState()
        {

        }

        public override void OnAnimationFinished()
        {

        }
    }
}