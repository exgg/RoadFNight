using System.Collections;
using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using UnityEngine;

public class ItemShopInteraction : MonoBehaviour
{
    [Header("UI")]
    public GameObject interactUI;
    public GameObject itemShopUI;
    public GameObject itemShopTitelUI;
    public GameObject itemShopUIContent;
    public TMPro.TMP_Text interactionText;
    [HideInInspector] public List<GameObject> inventorySlots = new List<GameObject>();
    public Transform inventorySlotsContent;
    public GameObject inventoryContent;
    public GameObject cancelSellItemsButton;

    bool inTrigger = false;

    bool saleMode = false;

    private Transform _camera;

    GameObject player;

    private void Start()
    {
        //_camera = FindObjectOfType<Camera>().transform;
        _camera = GameObject.Find("MainCamera").transform;
    }

    private void Update()
    {
        itemShopTitelUI.transform.LookAt(itemShopTitelUI.transform.position + _camera.rotation * Vector3.forward,
        _camera.rotation * Vector3.up);

        if(inTrigger)
        {
            interactUI.transform.LookAt(interactUI.transform.position + _camera.rotation * Vector3.forward,
            _camera.rotation * Vector3.up);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if(!saleMode)
                {
                    if (itemShopUI.activeInHierarchy)
                    {
                        player.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().inShop = false;
                        itemShopUI.SetActive(false);
                        LockCursor(false);
                        interactionText.text = "Press 'E'";
                    }
                    else if (!itemShopUI.activeInHierarchy)
                    {
                        player.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().inShop = true;
                        itemShopUI.SetActive(true);
                        LockCursor(true);
                        interactionText.text = "Press 'E' to close";
                    }
                }
                else
                {
                    CancelSellItems();
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!saleMode)
                {
                    if (itemShopUI.activeInHierarchy)
                    {
                        player.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().inShop = false;
                        itemShopUI.SetActive(false);
                        LockCursor(false);
                        interactionText.text = "Press 'E'";
                    }
                }
                else
                {
                    CancelSellItems();
                }
            }
        }
        else
        {
            if (saleMode)
                CancelSellItems();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if(other.GetComponent<ThirdPersonController>().enabled == true)
            {
                player = other.gameObject;
                inTrigger = true;
                interactUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            if (other.GetComponent<ThirdPersonController>().enabled == true)
            {
                player = null;
                inTrigger = false;
                interactUI.SetActive(false);
                if (itemShopUI.activeInHierarchy)
                {
                    other.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().inShop = false;
                    itemShopUI.SetActive(false);
                    LockCursor(false);
                    interactionText.text = "Press 'E'";
                }
            }
        }
    }

    public void DisableItemShopUI()
    {
        itemShopUI.SetActive(false);
        LockCursor(false);
    }

    public void SellItems()
    {
        saleMode = true;
        itemShopUIContent.SetActive(false);
        foreach (Transform slot in inventorySlotsContent){
            inventorySlots.Add(slot.gameObject);
        }
        foreach (GameObject slot in inventorySlots){
            slot.GetComponent<UIItemSale>().saleMode = true;
        }
        inventoryContent.SetActive(true);
        cancelSellItemsButton.SetActive(true);
    }

    public void CancelSellItems()
    {
        cancelSellItemsButton.SetActive(false);
        inventoryContent.SetActive(false);
        inventorySlots.Clear();
        foreach (Transform slot in inventorySlotsContent)
        {
            inventorySlots.Add(slot.gameObject);
        }
        foreach (GameObject slot in inventorySlots)
        {
            slot.GetComponent<UIItemSale>().saleMode = false;
        }
        inventorySlots.Clear();
        saleMode = false;
        itemShopUIContent.SetActive(true);
    }

    public void Refresh()
    {
        if(saleMode)
        {
            inventorySlots.Clear();
            foreach (Transform slot in inventorySlotsContent)
            {
                inventorySlots.Add(slot.gameObject);
            }
            foreach (GameObject slot in inventorySlots)
            {
                slot.GetComponent<UIItemSale>().saleMode = true;
            }
        }
    }

    public static void LockCursor(bool value)
    {
        if (value)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
