using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnRandomObject : MonoBehaviour
{
    public GameObject[] ObjectSelection;
    public Color color;
    public bool randomRotation;
    private float rotation;
    public bool randomScale;
    private float scaleVariationAmount;
    void Start()
    {
        SpawnObjects();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, .5f, 0), new Vector3(.5f, 1f, .5f));

        Gizmos.color = color;
        Gizmos.DrawCube(transform.position + new Vector3(0, .5f, 0), new Vector3(.5f, 1f, .5f));

    }

    private void SpawnObjects()
    {
        if (randomRotation)
        {
            rotation = Random.Range(0, 360);
        }
        else
        {
            rotation = 0;
        }

        if (randomScale)
        {
            scaleVariationAmount = Random.Range(0.9f, 1.1f);
        }
        else
        {
            scaleVariationAmount = 1;
        }
        GameObject newObj = Instantiate(ObjectSelection[Random.Range(0, ObjectSelection.Length)],transform.position,new Quaternion(0,rotation,0,0), transform);
        newObj.transform.localScale = new Vector3(scaleVariationAmount, scaleVariationAmount, scaleVariationAmount);
    }

}
