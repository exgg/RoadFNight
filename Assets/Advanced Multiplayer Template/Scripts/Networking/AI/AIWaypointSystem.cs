using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Mirror;

public class AIWaypointSystem : MonoBehaviour
{
    public Transform[] nextWaypoints;
    int nextWaypointIndex;

    public bool isCarWaypoint = true;

    public bool drawGizmos = true;

    private void OnTriggerEnter(Collider other)
    {
        if (isCarWaypoint)
        {
            if (other.GetComponent<CarAI>() != null)
            {
                nextWaypointIndex = Random.Range(0, nextWaypoints.Length);
                
                // if nextWaypoint != previous waypoint cont. if it does, repeat random range till it 
                // finds one that is not the previous waypoint 
                // once it finds the next waypoint, set the previous waypoint to this waypoint
                // then proceed to set the waypoint
                
                // this will predominantly work, however, I will also need to include a workout for direction
                // checking, as currently this will still sometimes cause a U turn to occur since if a AI starts 
                // at the first location and it is a cross roads, this will need to figure out which are at max 
                // 100 degrees, otherwise we will be doing full U turns still, this is more optimisation than fixes tho
                
                other.GetComponent<CarAI>().SetWaypoint(nextWaypoints[nextWaypointIndex]);
            }
        }
        else
        {
            if (other.GetComponent<PlayerAI>() != null)
            {
                nextWaypointIndex = Random.Range(0, nextWaypoints.Length);

                other.GetComponent<PlayerAI>().SetWaypoint(nextWaypoints[nextWaypointIndex], "Walking");
            }
        }
    }

#if (UNITY_EDITOR)
    private void OnDrawGizmos()
    {
        if(drawGizmos)
        {
            if (nextWaypoints.Length == 1)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
            if (nextWaypoints.Length < 1)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
            if (nextWaypoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
            if (nextWaypoints.Length == 0)
                return;
            Gizmos.color = Color.cyan;
            if (nextWaypoints.Length > 0)
                if (nextWaypoints[0] != null)
                    Gizmos.DrawLine(transform.transform.position, nextWaypoints[0].transform.position);
            if (nextWaypoints.Length > 1)
                if(nextWaypoints[1] != null)
                    Gizmos.DrawLine(transform.transform.position, nextWaypoints[1].transform.position);
            if (nextWaypoints.Length > 2)
                if (nextWaypoints[2] != null)
                    Gizmos.DrawLine(transform.transform.position, nextWaypoints[2].transform.position);
            if (nextWaypoints.Length > 3)
                if (nextWaypoints[3] != null)
                    Gizmos.DrawLine(transform.transform.position, nextWaypoints[3].transform.position);
        }
    }
#endif
}
