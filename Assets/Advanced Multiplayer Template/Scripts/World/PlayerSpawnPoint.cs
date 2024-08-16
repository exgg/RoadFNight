using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [HideInInspector]
    public bool isPlayerInTrigger = false;

    private void OnTriggerStay(Collider obj)
    {
        if (obj.GetComponent<NetPlayer>() != null)
        {
            isPlayerInTrigger = true;
        }
        else
        {
            isPlayerInTrigger = false;
        }
    }
}
