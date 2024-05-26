using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _indicator : MonoBehaviour
{
    public Color color;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 1f));

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position, new Vector3(1f, 1f, 1f));
    }

   
}
