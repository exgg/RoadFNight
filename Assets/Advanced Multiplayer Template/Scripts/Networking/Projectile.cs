using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Projectile : NetworkBehaviour
{
    public string shooterUsername;

    [Space]
    public int Damage = 20;

    bool _hitted = false;
    public AttackType AttackType;

    [SerializeField] GameObject _projectileModel;

    public void SetupProjectile(string ownerUsername, bool hasAuthority)
    {
        shooterUsername = ownerUsername;
        _hitted = false;
    }
    public void SetupProjectile_ServerSide()
    {
        StartCoroutine(DestroyProjectile());
    }
    IEnumerator DestroyProjectile()
    {
        yield return new WaitForSeconds(5);

        NetworkServer.Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        //if (!hasAuthority) return;

        if (_hitted) return;

        if (collision.gameObject.GetComponent<VehicleEnterExit.VehicleSync>() != null)
        {
            if (collision.gameObject.GetComponent<VehicleEnterExit.VehicleSync>().DriverUsername == shooterUsername)
            {
                Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                // To prevent the fired projectile from causing damage upon contact with the vehicle of the player who fired the projectile.
                return;
            }
        }
        else if (collision.transform.root.GetComponent<VehicleEnterExit.VehicleSync>() != null)
        {
            if (collision.transform.root.GetComponent<VehicleEnterExit.VehicleSync>().DriverUsername == shooterUsername)
            {
                Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                // To prevent the fired projectile from causing damage upon contact with the vehicle of the player who fired the projectile.
                return;
            }
        }
        if (collision.gameObject.GetComponent<Health>() != null)
        {
            if (collision.gameObject.GetComponent<Player>().username == shooterUsername)
            {
                Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                // To prevent the fired projectile from causing damage upon contact with the player who fired the projectile.
                return;
            }
        }
        else if (collision.transform.root.GetComponent<Health>() != null)
        {
            if (collision.transform.root.gameObject.GetComponent<Player>().username == shooterUsername)
            {
                Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
                // To prevent the fired projectile from causing damage upon contact with the player who fired the projectile.
                return;
            }
        }

        // print("I HITTED: " + collision.gameObject.name);

        GameObject hit = collision.gameObject;
        VehicleHealth health = hit.GetComponent<VehicleHealth>();
        Health playerHealth = hit.GetComponent<Health>();
        if (hit.GetComponent<VehicleHealth>() != null)
            health = hit.GetComponent<VehicleHealth>();
        else if (hit.transform.root.GetComponent<VehicleHealth>() != null)
            health = hit.transform.root.GetComponent<VehicleHealth>();
        if (hit.GetComponent<Health>() != null)
            playerHealth = hit.GetComponent<Health>();
        else if (hit.transform.root.GetComponent<Health>() != null)
            playerHealth = hit.transform.root.GetComponent<Health>();
        ContactPoint contact = collision.contacts[0];
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
        Vector3 pos = contact.point;

        if (health != null)
        {
            if (health.gameObject.GetComponent<VehicleEnterExit.VehicleSync>().DriverUsername != shooterUsername)
            {
                if (hasAuthority)
                    CmdTakeDamageVehicle(health, Damage);
            }
        }

        if (playerHealth != null)
        {
            if(playerHealth.gameObject.GetComponent<Player>().username != shooterUsername)
            {
                if (hasAuthority)
                    CmdTakeDamage(playerHealth, Damage);
            }
        }


        StopProjectile();

        GetComponent<CapsuleCollider>().enabled = false;

        OnCollided(collision.gameObject);
        if(hasAuthority)
            CmdCollided();
        _hitted = true;
    }

    protected virtual void OnCollided(GameObject objectCollidedWith)
    {
        print("COLLIED");
    }

    [Command]
    void CmdCollided() 
    {
        RpcOnCollided();
    }
    [ClientRpc]
    void RpcOnCollided() 
    {
        OnCollidedRPC();
    }
    protected virtual void OnCollidedRPC()
    {
        StopProjectile();
        _projectileModel.SetActive(false);
    }


    void StopProjectile() 
    {
        Rigidbody rg = GetComponent<Rigidbody>();
        rg.isKinematic = true;
        rg.velocity = Vector3.zero;
    }

    [Command]
    public void CmdTakeDamageVehicle(VehicleHealth health, int damage)
    {
        health.TakeDamage(damage);
    }
    [Command]
    public void CmdTakeDamage(Health health, int damage)
    {
        health.TakeDamage(damage, AttackType, shooterUsername);
        //health.attackerUsername = shooterUsername;
    }
}
