using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.UIUtils;
using StarterAssets;


namespace RedicionStudio.InventorySystem
{
    public class WeaponWheelManager : Inventory
    {

        private WeaponWheelSystem _weaponWheelSystem;

        private RectTransform _rectTransform;

        private StarterAssets.StarterAssetsInputs _inputs;

        public ItemSlot _slot;

        public static bool inWeaponWheel = false;

        private bool isWeaponWheelActive = false;

        PlayerInventoryModule playerInventoryModule;


        // Start is called before the first frame update
        // private void Start()
        // {
        //     if (isLocalPlayer)
        //     {
        //         Initialisation();
        //         slots.Callback += PlayerInventoryModule.Slots_Callback;
        //     }
        // }

        public void Initialisation()
        {
            _inputs = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssets.StarterAssetsInputs>();

            _weaponWheelSystem = UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>();
            _rectTransform = _weaponWheelSystem.MousePositionText.GetComponent<RectTransform>();
            _rectTransform.anchorMin = new Vector2(0, 0);
            _rectTransform.anchorMax = new Vector2(0, 0);

            playerInventoryModule = GetComponent<PlayerInventoryModule>();

        }

        public void WeaponWheelUIToggle(ItemSlot slot)
        {
            _slot = slot;

            if (!_inputs.weaponWheel)
            {
                DeactivateWeaponWheel();
                return;
            }
            if (!isWeaponWheelActive)
            {
                ToggleWeaponWheel();
            }

            if (isWeaponWheelActive)
            {
                UpdateWeaponWheel();
            }
        }

        private void ToggleWeaponWheel()
        {
            inWeaponWheel = !inWeaponWheel;
            isWeaponWheelActive = !isWeaponWheelActive;

            if (inWeaponWheel) EnterWeaponWheel();
            else ExitWeaponWheel();
        }
        private void EnterWeaponWheel()
        {
            UIPlayerInventory.WeaponWheel.SetActive(true);
            TPController.TPCameraController.LockCursor(false);

            RegisterWeapon();

            if (!BSystem.BSystem.inMenu) return; // only continue if in menu
            BSystem.BSystem.inMenu = false;
            BSystemUI.Instance.SetActive(false);
        }

        private void RegisterWeapon()
        {
            foreach (ItemSlot slot in slots)
            {
                if (!slot.item.itemSO) continue; // if null continue

                bool alreadyRegistered =
                    _weaponWheelSystem.weapons.Any(item => item.WeaponName == slot.item.itemSO.uniqueName);

                if (!alreadyRegistered)
                {
                    WeaponWheelItem weaponItem = new WeaponWheelItem
                    {
                        WeaponName = slot.item.itemSO.uniqueName,
                        InfoText = slot.item.itemSO.tooltipText,
                        type = slot.item.itemSO.weaponType
                    };

                    _weaponWheelSystem.weapons.Add(weaponItem);

                    if (weaponItem.type == ItemSO.WeaponType.Item)
                    {
                        _weaponWheelSystem.weapons.Remove(weaponItem);
                    }
                }
            }
        }

        private void ExitWeaponWheel()
        {
            UIPlayerInventory.WeaponWheel.SetActive(false);
            TPController.TPCameraController.LockCursor(true);
            isWeaponWheelActive = false;
        }

        private void UpdateWeaponWheel()
        {
            _rectTransform.anchoredPosition3D = Input.mousePosition;
            _weaponWheelSystem.MousePositionText.text = Input.mousePosition.ToString(); // set text of weapon system to mouse position on screen
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void DeactivateWeaponWheel()
        {
            UIPlayerInventory.WeaponWheel.SetActive(false);

            if (!BSystem.BSystem.inMenu && !PlayerInventoryModule.inMenu && !playerInventoryModule._emoteWheel.inEmoteWheel && !playerInventoryModule.inShop && !playerInventoryModule._chatSystem.isChatOpen)
            {
                TPController.TPCameraController.LockCursor(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            isWeaponWheelActive = false;
            inWeaponWheel = false;
        }
    }
}
