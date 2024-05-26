using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialIdentifier : MonoBehaviour
{
    //public MaterialEnum material = new MaterialEnum();

    public GameObject hitEffect;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<NetworkBullet>() != null)
        {
            GameObject hit = collision.gameObject;

            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;
            if(collision.gameObject.GetComponent<NetworkBullet>() != null & GetComponent<Player>() != null)
            {
                if (collision.gameObject.GetComponent<NetworkBullet>().shooterUsername != GetComponent<Player>().username)
                    SpawnHitEffect(pos, rot);
            }
        }
    }

    void SpawnHitEffect(Vector3 _position, Quaternion _rotation)
    {
        if(hitEffect != null)
            Instantiate(hitEffect, _position, _rotation);
    }
}

/*public enum MaterialEnum
{
    Metal,
    Sand,
    Stone,
    Wood,
    Meat,
    Character
};*/
