using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialIdentifier : MonoBehaviour
{
    //public MaterialEnum material = new MaterialEnum();

    public GameObject hitEffect;

    /// <summary>
    /// manages collisions with bullets?
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<NetworkBullet>() != null)
        {
            GameObject hit = collision.gameObject;

            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;
            if (collision.gameObject.GetComponent<NetworkBullet>() != null & GetComponent<NetPlayer>() != null)
            {
                if (collision.gameObject.GetComponent<NetworkBullet>().shooterUsername != GetComponent<NetPlayer>().username)
                    SpawnHitEffect(pos, rot);
            }
        }
    }

    /// <summary>
    /// instantiates hitEffect from being hit by bullets? or spawning bullets?
    /// </summary>
    /// <param name="_position"></param>
    /// <param name="_rotation"></param>
    void SpawnHitEffect(Vector3 _position, Quaternion _rotation)
    {
        if (hitEffect != null)
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
