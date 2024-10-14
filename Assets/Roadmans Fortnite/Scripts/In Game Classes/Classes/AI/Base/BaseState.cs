using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base
{
    public abstract class BaseState : MonoBehaviour
    {
        public abstract BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler);
    }
}
