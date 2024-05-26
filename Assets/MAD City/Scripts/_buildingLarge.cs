using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _buildingLarge : MonoBehaviour
{
    public Color color;
    public int _regionNo;
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(10f, 0f, 10f));

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position, new Vector3(10f, 0f, 10f));
    }
}
