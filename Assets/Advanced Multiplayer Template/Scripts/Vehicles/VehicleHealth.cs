using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleHealth : NetworkBehaviour
{
    public const int maxHealth = 100;
    
    [SyncVar]
    public int currentHealth = maxHealth;
    public GameObject VehicleExplosionPrefab;
    public GameObject VehicleDestroyedPrefab;
    public UnityEngine.UI.Slider healthbar;
    public TMPro.TMP_Text healthText;
    public bool shouldRegenerateHealth = false;
    bool isRegeneratingHealth = false;
    bool isVehicleDestroyed = false;
    [Header("Engine Demage")]
    public GameObject engineDemageLevel1;
    public GameObject engineDemageLevel2;
    public GameObject engineDemageLevel3;

    [Space]
    public float TimeToExplosionDuringBurning = 10f;

    [SyncVar]public float burningTimer = 0;

    public void TakeDamage(int amount)
    {
        if (!isServer) return;

        currentHealth -= amount;
        if(shouldRegenerateHealth & isRegeneratingHealth == false)
            InvokeRepeating("Regenerate", 0.0f, 1.0f / 5);
        if (currentHealth <= 0)
        {
            if(isVehicleDestroyed == false)
            {
                isVehicleDestroyed = true;
                foreach (VehicleEnterExit.VehicleSync.Seat seats in GetComponent<VehicleEnterExit.VehicleSync>()._seats)
                {
                    if (seats.Player != null)
                        seats.Player.GetComponent<Health>().TakeDamage(100, AttackType.Exploded, "Explosion wave");
                }
                RpcDie(transform.position, transform.rotation);
            }
        }
    }
    [ClientRpc]
    void RpcDie(Vector3 _position, Quaternion _rotation)
    {
        isVehicleDestroyed = true;
        GameObject VehicleExplosion = Instantiate(VehicleExplosionPrefab, _position, _rotation) as GameObject;
        GameObject VehicleDestroyed = Instantiate(VehicleDestroyedPrefab, _position, _rotation) as GameObject;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        healthbar.value = currentHealth;
        healthText.text = currentHealth.ToString();

        if (currentHealth > 45 & currentHealth < 65)
        {
            engineDemageLevel1.SetActive(true);
            engineDemageLevel2.SetActive(false);
            engineDemageLevel3.SetActive(false);
        }
        if (currentHealth > 25 & currentHealth < 45)
        {
            engineDemageLevel1.SetActive(false);
            engineDemageLevel2.SetActive(true);
            engineDemageLevel3.SetActive(false);
        }
        if (currentHealth > 0 & currentHealth < 25)
        {
            engineDemageLevel1.SetActive(false);
            engineDemageLevel2.SetActive(false);
            engineDemageLevel3.SetActive(true);

            if (isServer || hasAuthority)
            {
                if (currentHealth <= 0)
                {
                    burningTimer += 0.1f;
                    if (burningTimer > TimeToExplosionDuringBurning & isVehicleDestroyed == false)
                    {
                        isVehicleDestroyed = true;
                        foreach (VehicleEnterExit.VehicleSync.Seat seats in GetComponent<VehicleEnterExit.VehicleSync>()._seats)
                        {
                            if (seats.Player != null)
                                seats.Player.GetComponent<Health>().TakeDamage(100, AttackType.Exploded, "Explosion wave");
                        }
                        RpcDie(transform.position, transform.rotation);
                    }
                }
            }
        }
        if (Vector3.Dot(transform.up, Vector3.down) > 0)
        {
            if(GetComponent<UnityStandardAssets.Vehicles.Aeroplane.AeroplaneController>() == null)
            {
                if (isServer)
                {
                    if (isVehicleDestroyed == false)
                    {
                        isVehicleDestroyed = true;
                        foreach (VehicleEnterExit.VehicleSync.Seat seats in GetComponent<VehicleEnterExit.VehicleSync>()._seats)
                        {
                            if (seats.Player != null)
                                seats.Player.GetComponent<Health>().TakeDamage(100, AttackType.Exploded, "Explosion wave");
                        }
                        RpcDie(transform.position, transform.rotation);
                    }
                }
            }
        }
    }

    void Regenerate()
    {
        isRegeneratingHealth = true;
        if (currentHealth < maxHealth & currentHealth > 0)
            currentHealth += 2;
    }
}
