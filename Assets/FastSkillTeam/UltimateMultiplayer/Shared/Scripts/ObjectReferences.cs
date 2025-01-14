/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace FastSkillTeam.UltimateMultiplayer.Shared.Demo.References
{
    using UnityEngine;

    /// <summary>
    /// Helper class which references the objects.
    /// </summary>
    public class ObjectReferences : MonoBehaviour
    {
        [Tooltip("A reference to the first person objects.")]
        [SerializeField] protected Object[] m_FirstPersonObjects;
        [Tooltip("A reference to the third person objects.")]
        [SerializeField] protected Object[] m_ThirdPersonObjects;
        [Tooltip("A reference to the shooter objects.")]
        [SerializeField] protected Object[] m_ShooterObjects;
        [Tooltip("A reference to the melee objects.")]
        [SerializeField] protected Object[] m_MeleeObjects;
        [Tooltip("Any object that should always be removed.")]
        [SerializeField] protected Object[] m_RemoveObjects;
        [Tooltip("Objects that should use the shadow caster while in a first person only perspective.")]
        [SerializeField] protected GameObject[] m_ShadowCasterObjects;
        [Tooltip("A reference to other Object References that should be checked.")]
        [SerializeField] protected ObjectReferences[] m_NestedReferences;
        [Tooltip("A reference to the first person door objects.")]
        [SerializeField] protected GameObject[] m_FirstPersonDoors;
        [Tooltip("A reference to the third person door objects.")]
        [SerializeField] protected GameObject[] m_ThirdPersonDoors;
        [Tooltip("A reference to the RCC objects.")]
        [SerializeField] protected Object[] m_RealisticCarControllerObjects;
        [Tooltip("A reference to the non RCC usc objects.")]
        [SerializeField] protected Object[] m_StandardCarControllerObjects;
        [Tooltip("A reference to the heli objects.")]
        [SerializeField] protected Object[] m_HelicopterSystemObjects;
        [SerializeField] protected GameObject m_EventSystemGameObject;
        [SerializeField] protected GameObject[] m_MissingScripts;
        public GameObject[] MissingScripts { get { return m_MissingScripts; } set { m_MissingScripts = value; } }
        public GameObject EventSystemGameObject { get { return m_EventSystemGameObject; } set { m_EventSystemGameObject = value; } }
        public Object[] StandardCarControllerObjects { get { return m_StandardCarControllerObjects; } set { m_StandardCarControllerObjects = value; } }
        public Object[] HelicopterSystemObjects { get { return m_HelicopterSystemObjects; } set { m_HelicopterSystemObjects = value; } }
        public Object[] RealisticCarControllerObjects { get { return m_RealisticCarControllerObjects; } set { m_RealisticCarControllerObjects = value; } }
        public Object[] FirstPersonObjects { get { return m_FirstPersonObjects; } set { m_FirstPersonObjects = value; } }
        public Object[] ThirdPersonObjects { get { return m_ThirdPersonObjects; } set { m_ThirdPersonObjects = value; } }
        public Object[] ShooterObjects { get { return m_ShooterObjects; } set { m_ShooterObjects = value; } }
        public Object[] MeleeObjects { get { return m_MeleeObjects; } set { m_MeleeObjects = value; } }
        public Object[] RemoveObjects { get { return m_RemoveObjects; } set { m_RemoveObjects = value; } }
        public GameObject[] ShadowCasterObjects { get { return m_ShadowCasterObjects; } set { m_ShadowCasterObjects = value; } }
        public ObjectReferences[] NestedReferences { get { return m_NestedReferences; } set { m_NestedReferences = value; } }
        public GameObject[] FirstPersonDoors { get { return m_FirstPersonDoors; } set { m_FirstPersonDoors = value; } }
        public GameObject[] ThirdPersonDoors { get { return m_ThirdPersonDoors; } set { m_ThirdPersonDoors = value; } }
    }
}