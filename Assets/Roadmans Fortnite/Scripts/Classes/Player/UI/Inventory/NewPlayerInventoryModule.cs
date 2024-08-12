using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class NewPlayerInventoryModule : NewInventory
{
    public int ammo;

    public int money;

    private GameObject[] weapons = new GameObject[];

    GameObject currentWeapon = null;

    // Start is called before the first frame update
    void Start()
    {
        currentWeapon = weapons[0];
    }

    // Update is called once per frame
    void Update()
    {

    }
}
