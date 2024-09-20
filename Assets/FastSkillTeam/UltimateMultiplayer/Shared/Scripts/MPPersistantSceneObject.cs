namespace FastSkillTeam.UltimateMultiplayer.Shared
{
    using UnityEngine;
    /// <summary>
    /// Add this component to any object that should not be destroyed during round changes. Especially useful if the layer is 'VisualEffect' as these objects are destroyed in general.
    /// </summary>
    public class MPPersistantSceneObject : MonoBehaviour
    {
        [Tooltip("Specifies if this component should be automatically added to all children upon Awake().")]
        [SerializeField] protected bool m_AutoAddToChildren = false;

        private void Awake()
        {
            if (m_AutoAddToChildren)
            {
                foreach (Transform t in transform)
                {
                    if (t.gameObject.GetComponent<MPPersistantSceneObject>())
                        continue;
                    t.gameObject.AddComponent<MPPersistantSceneObject>();
                }
            }
        }
    }
}
