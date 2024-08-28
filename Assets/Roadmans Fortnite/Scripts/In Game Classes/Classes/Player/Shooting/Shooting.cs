using System.Collections;
using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Inventory;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Managers;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Shooting
{
    public class Shooting : NetworkBehaviour
    {
        private InputHandler _input;
        private TpManagerNew _tpManagerNew;
        private PlayerInventory _playerInventory;
        private NetPlayer _netPlayer;
        
        [Space]
        public float bulletSpeed;
        public GameObject muzzleFlashPrefab;
        
        public bool isDebugger;
        
        [Header("Controllers")] 
        public ThirdPersonController thirdPersonController;
        
        private bool _isShooting;
        [SyncVar] public int aimValue;
        
        public void Initialize()
        {
            _input = GetComponent<InputHandler>();
            _tpManagerNew = GetComponent<TpManagerNew>();
            thirdPersonController = GetComponent<ThirdPersonController>();
            _netPlayer = GetComponent<NetPlayer>();
            _playerInventory = GetComponent<PlayerInventory>();
        }

        public void TickUpdate()
        {
            AimingLogic();
            HandleShooting();
        }
        
        /// <summary>
        /// Handles the shooting logic by casting a ray from the weapon's bullet spawn point.
        /// </summary>
        public void HandleShooting()
        {
            if (!base.hasAuthority) return;

            if (_input.aimInput && _input.shootInput && _playerInventory.bulletCount > 0)
            {
                Shoot();
                
                RaycastHit hit;
                Transform bulletSpawnTransform = _tpManagerNew.currentWeaponManager.MuzzleFlashEffectPosition;
                
                Vector3 bulletDirection = bulletSpawnTransform.forward;

                if (Physics.Raycast(bulletSpawnTransform.position, bulletDirection, out hit, 100f))
                {
                    Debug.Log("Hit " + hit.collider.name + " at position " + hit.point);
                    // We can push to the server here if we hit an enemy or trigger some effect
                }

                // Trigger the muzzle flash effect
                TriggerMuzzleFlash(bulletSpawnTransform);
            }
        }

        /// <summary>
        /// Triggers the muzzle flash effect at the weapon's bullet spawn point.
        /// </summary>
        private void TriggerMuzzleFlash(Transform spawnPoint)
        {
            if (muzzleFlashPrefab)
            {
	            _tpManagerNew.currentWeaponManager.particleSystem.Play();
            }
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
			
                _tpManagerNew.playerAnimator.SetLayerWeight(1, 1f);

                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = _tpManagerNew.currentWeaponManager.Player.position.y;
                Vector3 aimDirection = (worldAimTarget - _tpManagerNew.currentWeaponManager.Player.position).normalized;

                if (_input.aimInput || isDebugger)
                {
		            _tpManagerNew.currentWeaponManager.Player.forward = Vector3.Lerp(_tpManagerNew.currentWeaponManager.Player.forward, aimDirection, Time.deltaTime * 20f);
		            
			        _tpManagerNew.playerRig.weight = 1;
		        
		            _tpManagerNew.secondHandRigTarget.localPosition = _tpManagerNew.currentWeaponManager.LeftHandPosition;
		            _tpManagerNew.secondHandRigTarget.localRotation = _tpManagerNew.currentWeaponManager.LeftHandRotation;
		           
		            _tpManagerNew.currentWeaponManager.isAiming = true;
		            _tpManagerNew.currentWeaponManager.Crosshair.SetActive(true);
		            if (_isShooting == false)
			            _tpManagerNew.playerAnimator.Play(_tpManagerNew.currentWeaponManager.WeaponAimTriggerName);
	            
		            thirdPersonController.SetSensitivity(_tpManagerNew.currentWeaponManager.aimSensitivity);
		            thirdPersonController.SetRotateOnMove(false);
		            
			        _tpManagerNew.weaponAimCamera.SetActive(true);
                }
                else
                {
		            if (aimValue != 0)
			            CmdSetAimValue(0);
		           
			        _tpManagerNew.playerRig.weight = 0;
			        
		            _tpManagerNew.currentWeaponManager.isAiming = false;
		            _tpManagerNew.playerAnimator.SetLayerWeight(1, 1f);
		            
		           
			        _tpManagerNew.weaponAimCamera.SetActive(false);
		            _tpManagerNew.currentWeaponManager.Crosshair.SetActive(false);
		          
		            thirdPersonController.SetSensitivity(_tpManagerNew.currentWeaponManager.normalSensitivity);
		            thirdPersonController.SetRotateOnMove(true);
		            
		           
		            _tpManagerNew.playerAnimator.ResetTrigger(_tpManagerNew.currentWeaponManager.WeaponAimTriggerName);
		            _tpManagerNew.playerAnimator.SetTrigger(_tpManagerNew.currentWeaponManager.WeaponIdleTriggerName);
	            }
            }
        }

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
        
        private void OnDrawGizmosSelected()
        {
	        if (_tpManagerNew != null && _tpManagerNew.currentWeaponManager != null)
	        {
		        Transform bulletSpawnTransform = _tpManagerNew.currentWeaponManager.CurrentWeaponBulletSpawnPoint.transform;
                
		        // Set the color of the gizmo
		        Gizmos.color = Color.red;

		        // Define the maximum distance of the raycast (same as the raycast in HandleShooting)
		        float maxRayDistance = 100f;

		        // Draw the raycast line in the scene view
		        Gizmos.DrawRay(bulletSpawnTransform.position, bulletSpawnTransform.forward * maxRayDistance);

		        // Optionally, draw a sphere at the end of the raycast to indicate where it would hit
		        RaycastHit hit;
		        if (Physics.Raycast(bulletSpawnTransform.position, bulletSpawnTransform.forward, out hit, maxRayDistance))
		        {
			        Gizmos.color = Color.green;
			        Gizmos.DrawSphere(hit.point, 0.1f); // Draw a small sphere at the hit point
		        }
	        }
        }
    }
}
