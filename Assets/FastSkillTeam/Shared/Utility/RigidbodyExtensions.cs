/////////////////////////////////////////////////////////////////////////////////
//
//  RigidbodyExtensions.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A helper script with extensions for rigidbodies.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.Shared.Utility
{
    using UnityEngine;
    public static class RigidbodyExtensions
    {
        /// <summary>
        /// Rotates the Rigidbody about axis passing through point in world coordinates by angle degrees.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        public static void RotateAround(this Rigidbody r, Vector3 point, Vector3 axis, float angle)
        {
            Quaternion delta = Quaternion.AngleAxis(angle, axis);
            r.MovePosition(delta * (r.transform.position - point) + point);
            r.MoveRotation(r.transform.rotation * delta);
        }
        /// <summary>
        /// Resets the rigdbody intertia tensor and velocities
        /// </summary>
        /// <param name="resetCenterOfMass">Reset the center of mass?</param>
        public static void Reset(this Rigidbody r, bool resetCenterOfMass)
        {
            r.angularVelocity = r.velocity = Vector3.zero;
            r.ResetInertiaTensor();
            if (resetCenterOfMass)
                r.ResetCenterOfMass();
        }
    }
}
