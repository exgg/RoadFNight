using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class WeaponWheel : NetworkBehaviour
{
   
	private PlayerInventoryModule _pIm;
	private Player _player;
	
	private void Start()
	{
		_pIm = GetComponent<PlayerInventoryModule>();
		_player = GetComponent<Player>();
		
		_pIm.slots.Callback += _pIm.Slots_Callback;
	}
	
    public void WeaponWheelUIToggle()
    {
        _pIm.slot = _pIm.slots[0];  
        
        if (!_pIm.inputs.weaponWheel)
        {
	        DeactivateWeaponWheel();
	        return;
        }
        if (!_pIm.isWeaponWheelActive)
        {
	        ToggleWeaponWheel();
        }

        if (_pIm.isWeaponWheelActive)
        {
	        UpdateWeaponWheel();
        }
    }

    private void ToggleWeaponWheel()
    {
	    _pIm.inWeaponWheel = !_pIm.inWeaponWheel;
	    _pIm.isWeaponWheelActive = !_pIm.isWeaponWheelActive;
        
        if(_pIm.inWeaponWheel)  EnterWeaponWheel();
        else ExitWeaponWheel();
    }
    private void EnterWeaponWheel()
    {
        UIPlayerInventory.WeaponWheel.SetActive(true);
        TPController.TPCameraController.LockCursor(false);
        
        RegisterWeapon();
    }

    private void RegisterWeapon()
    {
        foreach (ItemSlot slot in _pIm.slots)
        {
	        if (!slot.item.itemSO) continue; // if null continue

	        bool alreadyRegistered =
		        _pIm.weaponWheelSystem.weapons.Any(item => item.WeaponName == slot.item.itemSO.uniqueName);

	        if (!alreadyRegistered)
	        {
		        WeaponWheelItem weaponItem = new WeaponWheelItem
		        {
			        WeaponName = slot.item.itemSO.uniqueName,
			        InfoText = slot.item.itemSO.tooltipText,
			        type = slot.item.itemSO.weaponType
		        };

		        _pIm.weaponWheelSystem.weapons.Add(weaponItem);

		        if (weaponItem.type == ItemSO.WeaponType.Item)
		        {
			        _pIm.weaponWheelSystem.weapons.Remove(weaponItem);
		        }
	        }
        }
    }

    private void ExitWeaponWheel()
    {
        UIPlayerInventory.WeaponWheel.SetActive(false);
        TPController.TPCameraController.LockCursor(true);
        _pIm.isWeaponWheelActive = false;
    }

    private void UpdateWeaponWheel()
    {
	    _pIm.rectTransform.anchoredPosition3D = Input.mousePosition;
	    _pIm.weaponWheelSystem.MousePositionText.text = Input.mousePosition.ToString(); // set text of weapon system to mouse position on screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void DeactivateWeaponWheel()
    {
        UIPlayerInventory.WeaponWheel.SetActive(false);

        if (!_pIm.inMenu && !_pIm.emoteWheel.inEmoteWheel && !_pIm.inShop && ! _pIm.chatSystem.isChatOpen)
        {
	        TPController.TPCameraController.LockCursor(true);
	        Cursor.lockState = CursorLockMode.Locked;
	        Cursor.visible = false;
        }

        _pIm.isWeaponWheelActive = false;
        _pIm.inWeaponWheel = false;
    }
    
}
