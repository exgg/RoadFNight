using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WaitingState : BaseState
    {
        public PathfinderState pathfinderState;
        public InitialPathfinderState initialPathfinderState;

        public bool _isWaiting;// Controlled by waiting signal e.g. traffic light

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("There is no path point setup");
                return initialPathfinderState;
            }

            if (_isWaiting)
            {
                Debug.Log("Waiting");
                animationHandler.SetWaitingAnimation(aiStats.myGender);
                return this;
            }
            else
            {
                return pathfinderState; // Move to next state to find a new path
            }
        }
    }
}
