using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIItemSale : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public bool saleMode;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(saleMode)
        {
            SellItem();
        }
    }

    public void SellItem()
    {
        RedicionStudio.InventorySystem.UISlot uiSlot;
        uiSlot = GetComponent<RedicionStudio.InventorySystem.UISlot>();

        uiSlot.playerInteractionModule.RemoveItem(uiSlot.playerInteractionModule.GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>(), uiSlot.sellPrice, uiSlot.item, 1, uiSlot.itemSlotIndex);
    }
}
