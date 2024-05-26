using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ExplosionRadius : NetworkBehaviour
{
    /*public ExplosiveObject explosiveObject;
    public bool checkPlayersInRadius = false;

    void OnTriggerStay(Collider other)
    {
        if (checkPlayersInRadius == true & other.gameObject.CompareTag("Player"))
        {
            Collider[] colliders = Physics.OverlapSphere(other.gameObject.transform.position, explosiveObject.explosionRadius);
            foreach (Collider col in colliders)
            {
                if (col.tag == "Player")
                {
                    col.GetComponent<Health>().TakeDamage(explosiveObject.explosionDemage);
                }
            }

            checkPlayersInRadius = false;
        }
    }*/
}
