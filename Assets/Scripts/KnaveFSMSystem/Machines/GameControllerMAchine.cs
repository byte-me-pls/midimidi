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
                ButterFlyStates.none, 
                ButterFlyStates.bir,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex == 1, 
                priority: 1);
            
            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.bir, 
                ButterFlyStates.iki,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 2, 
                priority: 3);
            
            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.iki, 
                ButterFlyStates.uc,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 4, 
                priority: 3);

            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.uc, 
                ButterFlyStates.dort,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 6, 
                priority: 3);

            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.dort, 
                ButterFlyStates.bes,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 8, 
                priority: 3);

            yield return new Transition<PlayerControllerMachine, ButterFlyStates>(
                "Bad Accuracy -> Stumble", 
                ButterFlyStates.bes, 
                ButterFlyStates.altı,
                // CodeDrawnAccuracyBar scriptine ulaşıp indexi kontrol ediyoruz
                ctx =>  _CodeDrawnAccuracyBar.lowAccuracyIndex > 10, 
                priority: 3);

            
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
            Debug.Log(currentState);
        }

        public override void LateUpdate()
        {
        }
    }
}