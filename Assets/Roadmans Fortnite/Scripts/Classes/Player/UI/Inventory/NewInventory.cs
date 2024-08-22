using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

/// <summary>
/// Represents an individual item in the inventory system
/// </summary>
[System.Serializable]
public struct Item
{

    public int hash; // unique ID?

    [System.NonSerialized]
    private NewItemSO _itemSO; // cashed reference to the items ScriptableObject

    [System.NonSerialized]
    private int oldHash; // previous ID to detect changes

    /// <summary>
    /// Get the associated scriptable object (SO) for this item/
    /// If the cashed item SO is null or has changed, it fetchees the items SO based on the Hash ID
    /// </summary>
    public NewItemSO itemSO
    {
        get
        {
            if (_itemSO == null || hash != oldHash)
            {
                _itemSO = NewItemSO.GetItemSO(hash);
                oldHash = hash;
            }
            return _itemSO;
        }
    }

    public float currentShelfLifeInSeconds;

    /// <summary>
    /// Initialise a new instance of the Item Struct with the specified unique name with a current shelf life in seconds
    /// </summary>
    /// <param name="itemSOUniqueName">ID</param>
    /// <param name="currentShelfLifeInSeconds">Life Time</param>
    public Item(string itemSOUniqueName, float currentShelfLifeInSeconds)
    {
        hash = itemSOUniqueName.GetStableHashCode(); // custom extension method for mirror to formulate a standardized hash for each machine

        _itemSO = null;
        oldHash = hash;

        this.currentShelfLifeInSeconds = currentShelfLifeInSeconds;
    }

    public string TooltipText => itemSO.GetTooltipText().Replace("{CURRENT_SHELF_LIFE}",
        System.TimeSpan.FromSeconds(currentShelfLifeInSeconds).ToString("hh\\:mm\\:ss"));
}

[System.Serializable]
public struct ItemSlot
{

    public Item item;
    public int amount;

    public int IncreaseBy(int value)
    {
        int c = Mathf.Clamp(value, 0, item.itemSO.stackSize - amount); // ?
        amount += c;
        return c;
    }

    public int DecreaseBy(int value)
    {
        int c = Mathf.Clamp(value, 0, amount);
        amount -= c;
        return c;
    }

    public string TooltipText => item.TooltipText.Replace("{AMOUNT}", amount.ToString());
}

public class NewSyncListItemSlot : SyncList<ItemSlot> { }


public abstract class NewInventory : NetworkBehaviour
{
    public readonly NewSyncListItemSlot slots = new NewSyncListItemSlot();

    public int GetSlotIndex(string itemSOUniqueName)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].amount > 0 && slots[i].item.itemSO.uniqueName == itemSOUniqueName)
            {
                return i;
            }
        }

        return -1;
    }

    public bool PossibleToAdd(Item item, int amount)
    {
        for (int i = 4; i < slots.Count; i++)
        {
            if (slots[i].amount == 0)
            {
                amount -= item.itemSO.stackSize;
            }
            else if (slots[i].item.Equals(item))
            { // ?
                amount -= slots[i].item.itemSO.stackSize - slots[i].amount;
            }

            if (amount <= 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool Add(Item item, int amount)
    {
        if (!PossibleToAdd(item, amount))
        {
            return false;
        }

        int i;

        for (i = 4; i < slots.Count; i++)
        {
            if (slots[i].amount > 0 && slots[i].item.Equals(item))
            { // ?
                ItemSlot tempSlot = slots[i];
                amount -= tempSlot.IncreaseBy(amount);
                slots[i] = tempSlot;
            }

            if (amount <= 0)
            {
                return true;
            }
        }

        for (i = 4; i < slots.Count; i++)
        {
            if (slots[i].amount == 0)
            {
                int tempValue = Mathf.Min(amount, item.itemSO.stackSize);
                slots[i] = new ItemSlot { item = item, amount = tempValue };
                amount -= tempValue;
            }

            if (amount <= 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool Remove(Item item, int amount)
    {
        ItemSlot slot;
        for (int i = 0; i < slots.Count; i++)
        {
            slot = slots[i];
            if (slot.amount > 0 && slot.item.Equals(item))
            { // ?
                amount -= slot.DecreaseBy(amount);
                slots[i] = slot;

                if (amount == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
