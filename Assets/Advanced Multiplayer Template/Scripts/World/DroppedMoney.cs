using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DroppedMoney : NetworkBehaviour
{
    [SyncVar] public int moneyAmount = 10;
    [SyncVar] public bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if(!collected & other.tag == "Player" && other.GetComponent<NetPlayer>())
        {
            if(isServer)
            {
                collected = true;
                other.GetComponent<PlayerInteractionModule>().AddMoney(other.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>(), moneyAmount);

                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
