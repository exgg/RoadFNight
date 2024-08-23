using System.Collections;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Managers;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Shooting
{
    public class Shooting : NetworkBehaviour
    {
        private InputHandler _input;

        private TpManagerNew _tpManagerNew;
       
    
        [Header("Car Theft Cams")] 
        public GameObject carTheftCamera;
        
        [Header("Controllers")] 
        public ThirdPersonController thirdPersonController;
        
     

        public void Initialize()
        {
            _tpManagerNew = GetComponent<TpManagerNew>();
        }
        
        
        private void AimingLogic()
        {
            if (!_tpManagerNew.currentWeaponManager || !_tpManagerNew.playerRig || !thirdPersonController || !_tpManagerNew.weaponAimCamera || !_tpManagerNew.secondHandRigTarget || !_tpManagerNew.playerAnimator)
            {
                Debug.LogError($"There is an issue with CurrentWeaponManager{_tpManagerNew.currentWeaponManager == null} " +
                               $"n/ or the PlayerRig: {_tpManagerNew.playerRig == null} n/ or Third Person Controller {thirdPersonController == null} n/ or" +
                               $"Weapon aim Camera {_tpManagerNew.weaponAimCamera == null} n/ or SecondHandRigTarget {_tpManagerNew.secondHandRigTarget == null} n/ or playerAnimator {_tpManagerNew.playerAnimator == null}");
                return;
            }
            if (isLocalPlayer)
            {
                Vector3 mouseWorldPosition = Vector3.zero;
                Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

                _tpManagerNew.currentWeaponManager.debugTransform.position = ray.GetPoint(20f);
                mouseWorldPosition = ray.GetPoint(20f);

                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = _tpManagerNew.currentWeaponManager.Player.position.y;
                Vector3 aimDirection = (worldAimTarget - _tpManagerNew.currentWeaponManager.Player.position).normalized;

                if (_input.aimInput) // why do we have 2 here?
                {

                    // use the inventory manager, this however will be using the player manager class: 

                    _tpManagerNew.currentWeaponManager.Player.forward = Vector3.Lerp(_tpManagerNew.currentWeaponManager.Player.forward, aimDirection, Time.deltaTime * 20f);
              
                    _tpManagerNew.playerRig.weight = 1;
        
            
                    _tpManagerNew.secondHandRigTarget.localPosition = _tpManagerNew.currentWeaponManager.LeftHandPosition;
                    _tpManagerNew.secondHandRigTarget.localRotation = _tpManagerNew.currentWeaponManager.LeftHandRotation;
         

                    _tpManagerNew.currentWeaponManager.isAiming = true;
                    _tpManagerNew.currentWeaponManager.Crosshair.SetActive(true);

                    // use the animation set from the current weapon
                    if (!_input.isShooting)
                        _tpManagerNew.playerAnimator.Play(_tpManagerNew.currentWeaponManager.WeaponAimTriggerName);


                    thirdPersonController.SetSensitivity(_tpManagerNew.currentWeaponManager.aimSensitivity);
                    thirdPersonController.SetRotateOnMove(false);

              
                    _tpManagerNew.weaponAimCamera.SetActive(true);
                }
          
                else
                {
                    if (_tpManagerNew.playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("KickOut"))
                    {
                        if(!carTheftCamera.activeInHierarchy)
                            carTheftCamera.SetActive(true);
                    }
                    else
                    {
                        if(carTheftCamera.activeInHierarchy)
                            carTheftCamera.SetActive(false);
                    }
                }
            }
            else
            {
                if (_input.aimInput)
                {
            
                    _tpManagerNew.playerRig.weight = 1;
            
                    // set target rig location / rotation 
                    _tpManagerNew.secondHandRigTarget.localPosition = _tpManagerNew.currentWeaponManager.LeftHandPosition;
                    _tpManagerNew.secondHandRigTarget.localRotation = _tpManagerNew.currentWeaponManager.LeftHandRotation;

                    // toggle is aiming
                    _tpManagerNew.currentWeaponManager.isAiming = true;
            
                    // play the animation of aiming
                    _tpManagerNew.playerAnimator.Play(_tpManagerNew.currentWeaponManager.WeaponAimTriggerName);

                    thirdPersonController.SetSensitivity(_tpManagerNew.currentWeaponManager.aimSensitivity);
                }
                else
                {
                    _tpManagerNew.playerRig.weight = 0;
                    _tpManagerNew.currentWeaponManager.isAiming = false;
            
                    thirdPersonController.SetSensitivity(_tpManagerNew.currentWeaponManager.normalSensitivity);
                    thirdPersonController.SetRotateOnMove(true);
            
                    _tpManagerNew.playerAnimator.ResetTrigger(_tpManagerNew.currentWeaponManager.WeaponAimTriggerName);
                    _tpManagerNew.playerAnimator.SetTrigger(_tpManagerNew.currentWeaponManager.WeaponIdleTriggerName);
                }
            }
   
            // this then sets to player ai weapon is false... we are getting rid of this
        } // This needs moving into the shooting class:

        public void Shoot()
        {
            StopCoroutine(EndShooting());
            _tpManagerNew._isShooting = true;
    
            _tpManagerNew.playerAnimator.Play(_tpManagerNew.currentWeaponManager.WeaponShootAnimationName);
            StartCoroutine(EndShooting());
        }

        IEnumerator EndShooting()
        {
            yield return new WaitForSeconds(_tpManagerNew.currentWeaponManager.WeaponShootAnimationLength);

            _tpManagerNew._isShooting = false;
        }


        // push current aim value to server to display animation on all sides
        [Command]
        private void CmdSetAimValue(int value)
        {
            _tpManagerNew.aimValue = value;
            RpcSetValue(value);
        }

        [ClientRpc]
        private void RpcSetValue(int value)
        {
            _tpManagerNew.aimValue = value;
        }
    
    }
}
