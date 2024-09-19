/////////////////////////////////////////////////////////////////////////////////
//
//  MPDamageZone.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//  Original code copyright (c) Opsive. https://www.opsive.com
//
//	Description:	Continuously applies damage to the character while the
//	                character is within the trigger. Syncs the operation for MP
//	                without sending extra data.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Traits.Damage;
    using Opsive.UltimateCharacterController.Utility;
    using UnityEngine;
    using Random = UnityEngine.Random;

    /// <summary>
    /// Continuously applies damage to the character while the character is within the trigger.
    /// </summary>
    public class MPDamageZone : MonoBehaviour
    {
        [Tooltip("The delay until the damage is started to be applied.")]
        [SerializeField] protected float m_InitialDamageDelay = 0.5f;
        [Tooltip("The amount of damage to apply during each damage event.")]
        [SerializeField] protected float m_DamageAmount = 10;
        [Tooltip("The interval between damage events.")]
        [SerializeField] protected float m_DamageInterval = 0.2f;

        ScheduledEventBase m_ScheduledEvent;
        private IDamageTarget m_Health;

        /// <summary>
        /// An object has entered the trigger.
        /// </summary>
        /// <param name="other">The object that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (m_Health != null)
                return;

            // A main character collider is required.
            if (!MathUtility.InLayerMask(other.gameObject.layer, 1 << LayerManager.Character))
            {
                return;
            }

            // The object must be a local character.
            MPLocalPlayer localPlayer = other.gameObject.GetCachedParentComponent<MPLocalPlayer>();
            if (localPlayer == null)
            {
                return;
            }

            // With a health component.
            m_Health = localPlayer.PlayerHealth;
            if (m_Health == null)
                return;

            // That is alive.
            if (m_Health.IsAlive() == false)
            {
                m_Health = null;
                return;
            }
            m_ScheduledEvent = Scheduler.Schedule(m_InitialDamageDelay, Damage);
        }

        /// <summary>
        /// Apply damage to the health component.
        /// </summary>
        private void Damage()
        {
            if (m_Health.IsAlive() == false)
            {
                m_Health = null;
                return;
            }

            Vector3 pos = m_Health.Owner.transform.position + Random.insideUnitSphere;

            // Apply the damage.
            // NOTE: DamageZones are a special case, self harm on a client will only show the effects as it only happens locally.
            //       Actual damage will not be applied and the stats will not be synced.
            //       Therefore we use the MPLocalPlayer to handle it as it contains the logic to keep it synced.
            MPLocalPlayer.Damage(m_DamageAmount, pos, Vector3.zero, 0, m_Health.Owner);

            // Apply the damage again if the object still has health remaining.
            if (m_Health.IsAlive())
                m_ScheduledEvent = Scheduler.Schedule(m_DamageInterval, Damage);
            else m_Health = null;
        }

        /// <summary>
        /// An object has exited the trigger.
        /// </summary>
        /// <param name="other">The collider that exited the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            // A main character collider is required.
            if (!MathUtility.InLayerMask(other.gameObject.layer, 1 << LayerManager.Character))
            {
                return;
            }

            // Only perform locally, or health will be cleared by others when they die.
            if (other.gameObject.GetCachedParentComponent<MPLocalPlayer>() == null)
                return;

            if (m_Health == null)
                return;

            // The object has left the trigger - stop applying damage.
            Scheduler.Cancel(m_ScheduledEvent);
            m_Health = null;
        }

        /// <summary>
        /// Draw a gizmo showing the damage zone.
        /// </summary>
        private void OnDrawGizmos()
        {
            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                var color = Color.red;
                color.a = 0.5f;
                Gizmos.color = color;
                var meshTransform = meshCollider.transform;
                Gizmos.DrawMesh(meshCollider.sharedMesh, meshTransform.position, meshTransform.rotation);
            }
        }

        /// <summary>
        /// The object was disabled
        /// </summary>
        private void OnDisable()
        {
            m_Health = null;
        }
    }
}