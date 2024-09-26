using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States
{
    public abstract class BaseState : MonoBehaviour
    {
        public abstract BaseState Tick(StateHandler stateHandler, AIStats aiStats, AIAnimationHandler animationHandler);
    }
}
