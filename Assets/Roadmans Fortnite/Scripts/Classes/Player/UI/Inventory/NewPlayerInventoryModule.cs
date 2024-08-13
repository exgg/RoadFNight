using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class NewPlayerInventoryModule : NewInventory
{
    public int ammoAmount;

    public int money;

    public static int maxWeapons = 10;

    public static int maxSlots = 30;

    private GameObject[] weapons = new GameObject[maxWeapons];

    private Item[] item = new Item[maxSlots];

    GameObject currentWeapon = null;

    public static bool inMenu = false;

    ItemSlot _slot;

    // Start is called before the first frame update
    void Start()
    {
        currentWeapon = weapons[0];
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ShelfLifeCalculation()
    {

    }

    void LoadInventory()
    {
        for (int i = 0; i < maxSlots; i++)
        {

        }
    }


}
