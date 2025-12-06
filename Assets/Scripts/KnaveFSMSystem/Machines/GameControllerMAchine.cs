using System.Collections.Generic;
using System.Linq; 
using KnaveFSMSystem.Core;
using KnaveFSMSystem.StateSystem.Interfaces;
using KnaveFSMSystem.Transitions;
using UnityEngine;

namespace KnaveFSMSystem.Machines
{
    [AddComponentMenu("Knave/Knave Player Controller")]
    public class PlayerControllerMachine
        : KnaveMachineController<PlayerControllerMachine, ButterFlyStates>
    {
        [Header("Dependencies")] 
        [SerializeField] private CodeDrawnAccuracyBar _CodeDrawnAccuracyBar;
        
        public CodeDrawnAccuracyBar CodeDrawnAccuracyBar => _CodeDrawnAccuracyBar;

        [HideInInspector] public bool IsDodgeFinished;

        protected override IEnumerable<ITransition<PlayerControllerMachine, ButterFlyStates>> CreateTransitions()
        {

            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.Walk, 
                ButterFlyStates.Walk,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 0, 
                priority: 1);
            
            // ALTERNATİF: Eğer "Son 3 vuruş Miss ise" gibi bir şey istersen:
            /*
            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
               "Stumble Trigger",
               ButterFlyStates.Idle,
               ButterFlyStates.Stumble,
               ctx => _midiGameManager.hitHistory.TakeLast(3).All(x => x == HitResult.Miss),
               priority: 1);
            */
        }

        public override void Update()
        {
        }

        public override void LateUpdate()
        {
        }
    }
}