using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _poi : MonoBehaviour
{
    public Color color;
    public int _regionNo;
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(30f, 0f, 30f));

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position, new Vector3(30f, 0f, 30f));
    }
}
