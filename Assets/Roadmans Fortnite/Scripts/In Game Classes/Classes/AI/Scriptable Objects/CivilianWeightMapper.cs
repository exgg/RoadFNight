using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects
{
    [CreateAssetMenu(menuName = "Pedestrian/WeightMap")]
    public class CivilianWeightMapper : ScriptableObject
    {
        [Range(0.01f, 1)] public float FakeRange;
    }
}
