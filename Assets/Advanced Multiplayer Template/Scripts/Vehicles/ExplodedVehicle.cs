using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodedVehicle : MonoBehaviour
{
    public Rigidbody[] vehicleParts;
    public float explosionRadius = 5.0F;
    public float power = 10.0F;

    void Start()
    {
        Vector3 explosionPos = transform.position;
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
        if (vehicleParts != null)
        {
            foreach (Rigidbody part in vehicleParts)
                part.AddExplosionForce(power, explosionPos, explosionRadius, 3.0F);
        }
    }
}
