using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;


namespace Roadmans_Fortnite.Scripts.Classes.Player.UI.Inventory
{

    public class NewInventory : MonoBehaviour
    {
        public int cash;

        public void PurchaseItem(int price) //other parameters: item
        {
            if (cash < price)
            {
                Debug.Log("Insufficient funds");
                return;
            }

            cash -= price;

            //AddToInventory(item);
        }

        public void SellItem(int cost) //other parameters: item
        {
            cash += cost;

            //RemoveFromInventory(item);
        }

        public void GiveItem()
        {

        }

        public virtual void LoadInventory()
        {
            
        }

    }

}
