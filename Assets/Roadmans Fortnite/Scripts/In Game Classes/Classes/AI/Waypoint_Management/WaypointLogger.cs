using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management
{
    public class WaypointLogger : MonoBehaviour
    {
        public List<GameObject> waypoints = new List<GameObject>();
        public District _district;
        public bool isRoadPoint;


        private void OnDrawGizmos()
        {
            switch (waypoints.Count)
            {
                case 0:
                    Gizmos.color = Color.red;
                    break;
                case 1:
                    Gizmos.color = Color.yellow;
                    break;
                case 2:
                    Gizmos.color = Color.green;
                    break;
                case 3:
                    Gizmos.color = Color.magenta;
                    break;
                case 4:
                case 5:
                case 6:
                    Gizmos.color = Color.blue;
                    break;
            }
            
            Gizmos.DrawSphere(transform.position, 1f);
            
            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.color = waypoint.GetComponent<WaypointLogger>().waypoints.Contains(gameObject) ? Color.green : Color.red;

                    Gizmos.DrawLine(transform.position, waypoint.transform.position);
                }
            }
        }
    }
}
