using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _building : MonoBehaviour
{
    public Color color;
    public int _regionNo;
    public bool corner;
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(5f, 0f, 5f));

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position, new Vector3(5f, 0f, 5f));
    }
}