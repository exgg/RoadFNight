using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.UIUtils;
using StarterAssets;

namespace RedicionStudio.InventorySystem
{
    public class ShopManager : Inventory
    {
        /// <summary>
        /// Activation toggle for the UI of the ShopUI, can be done in event based instead of this but that can happen in phase 3
        /// </summary>
        public void ShopUIToggle()
        {
            // PSEUDO
            // allow for expanding to event based later PHASE 3

            if (!PlayerInventoryModule._keyboard.tabKey.wasPressedThisFrame) return;

            PlayerInventoryModule.inMenu = !PlayerInventoryModule.inMenu;

            switch (PlayerInventoryModule.inMenu)
            {
                case true:
                    EnterShopMenu();
                    break;
                case false:
                    ExitShopMenu();
                    break;
            }
        }

        /// <summary>
        /// Toggle on the Shop Menu, checking if BSsystem has in menu to toggle another instance
        /// </summary>
        private void EnterShopMenu()
        {
            if (BSystem.BSystem.inMenu)
            {
                BSystem.BSystem.inMenu = false;
                BSystemUI.Instance.SetActive(false);

            }

            UIPlayerInventory.SetActive(true);
            UIPlayerInventory.InventoryUI.SetActive(true);
            TPController.TPCameraController.LockCursor(false);
        }

        /// <summary>
        /// Toggle off the shop menu
        /// </summary>
        private void ExitShopMenu()
        {
            UIPlayerInventory.SetActive(false);
            UIPlayerInventory.InventoryUI.SetActive(false);
            TPController.TPCameraController.LockCursor(true);
        }
    }
}