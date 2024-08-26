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

        private NetPlayer _netPlayer;
        
        [Space]
        public GameObject bulletPrefab;
        public GameObject rocketPrefab;
        public float bulletSpeed;
        Transform _bulletSpawnPointPosition;

        
        [Space]
        public GameObject cartridgeEjectPrefab;
        Transform _cartridgeEjectSpawnPointPosition;
     
        
        [Header("Controllers")] 
        public ThirdPersonController thirdPersonController;
        
     

        public void Initialize()
        {
	        _input = GetComponent<InputHandler>();
            _tpManagerNew = GetComponent<TpManagerNew>();
            thirdPersonController = GetComponent<ThirdPersonController>();
            _netPlayer = GetComponent<NetPlayer>();
        }

        public void TickUpdate()
        {
	        AimingLogic();
        }
        
		/// <summary>
		/// Spawns a bullet at the location of the current weapon spawn then ejects bullet towards a location using the screen point to ray
		/// method from unity
		/// Uses an if Statement for that of rockets or bullets
		///
		///
		/// These need changing need to look for swap weapon, then call the change of current weapon bullet spawn to be only called on weapon change and awake (if has a weapon)
		/// or maybe add a debounce to have a more simple approach.
		/// Switch statement to replace the if statement so we can include different bullet types such as 9mm etc.
		/// </summary>
		public void ShootBullet()
		{
			if (!base.hasAuthority) return;

			_bulletSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentWeaponBulletSpawnPoint;
			_cartridgeEjectSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentCartridgeEjectSpawnPoint;
			string currentBulletName = this.GetComponent<ManageTPController>().CurrentWeaponManager.WeaponBulletPrefab.name;
			//bulletSpeed = this.GetComponent<ManageTPController>().CurrentWeaponManager.BulletSpeed;
			this.GetComponent<ManageTPController>().Shoot();
			//CmdSetAttackerUsername(username);

			Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

			int layerToIgnore = 4; // Replace with the layer you want to ignore
			LayerMask layerMask = ~(1 << layerToIgnore);
			RaycastHit hit;
			Vector3 collisionPoint;
			if (Physics.Raycast(ray, out hit, 50f, layerMask))
			{
				collisionPoint = hit.point;
			}
			else
			{
				collisionPoint = ray.GetPoint(50f);
			}

			Vector3 bulletVector = (collisionPoint - _bulletSpawnPointPosition.transform.position).normalized;




			if (currentBulletName == "Bullet")
				CmdShootBullet(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, _cartridgeEjectSpawnPointPosition.position, _cartridgeEjectSpawnPointPosition.rotation, bulletVector, bulletSpeed);
			else
				CmdShootRocket(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, bulletVector, bulletSpeed);
		}

		
		/// <summary>
		/// Server command to shoot the bullet after the shoot bullet client side has been called, calculating which type of projectile will be fired.
		/// Then it will use the position of the pawn rotation, position of the cartridge ejection, spawn point position of the cartridge, bullet direction of shooting and its speed.
		/// This will then move to a remote procedure to fire this event from the client to the network. 
		/// </summary>
		/// <param name="_position"></param>
		/// <param name="_rotation"></param>
		/// <param name="_cartridgeEjectPosition"></param>
		/// <param name="_cartridgeEjectRotation"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[Command]
		void CmdShootBullet(Vector3 _position, Quaternion _rotation, Vector3 _cartridgeEjectPosition, Quaternion _cartridgeEjectRotation, Vector3 _bulletVector, float _bulletSpeed)
		{
			GameObject Bullet = Instantiate(bulletPrefab, _position, _rotation) as GameObject;

			Bullet.GetComponent<Rigidbody>().velocity = _bulletVector * _bulletSpeed;

			//Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

			NetworkServer.Spawn(Bullet);

			NetworkBullet bullet = Bullet.GetComponent<NetworkBullet>();
			bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

			bullet.SetupProjectile_ServerSide();

			RpcBulletFired(bullet, _bulletVector, _bulletSpeed);


			GameObject _cartridgeEject = Instantiate(cartridgeEjectPrefab, _cartridgeEjectPosition, _cartridgeEjectRotation) as GameObject;



			NetworkServer.Spawn(_cartridgeEject, connectionToClient);
		}
        
		/// <summary>
		/// Sets up the projectile for who fired it, and then also set the autority to the player who fired it,
		/// this seems to be to prevent the player from getting hit by their own bullets.
		/// </summary>
		/// <param name="Bullet"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[ClientRpc]
		void RpcBulletFired(NetworkBullet Bullet, Vector3 _bulletVector, float _bulletSpeed)
		{
			Bullet.GetComponent<NetworkBullet>().SetupProjectile(_netPlayer.username, hasAuthority);

			//Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
		}
		
		
		/// <summary>
		/// Commands the server to instantiate a rocket prefab at the position of the weapon spawn. this is passed from the shoot bullet method
		/// which allows us to carry over the positions of the spawn, its rotation, the direction it is going and the speed.
		///
		/// Naming convention on this and a few others is pretty bad so that needs editing
		/// </summary>
		/// <param name="_position"></param>
		/// <param name="_rotation"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[Command]
		void CmdShootRocket(Vector3 _position, Quaternion _rotation, Vector3 _bulletVector, float _bulletSpeed)
		{
			GameObject Bullet = Instantiate(rocketPrefab, _position, _rotation) as GameObject;

			//Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);

			//Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

			NetworkServer.Spawn(Bullet);

			NetworkRocket bullet = Bullet.GetComponent<NetworkRocket>();
			bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

			bullet.SetupProjectile_ServerSide();

			RpcRocketFired(bullet, _bulletVector, _bulletSpeed);
		}

		/// <summary>
		/// Remote to send the projectile setup from the client to the server, using the method aboves
		/// positional data and direction 
		/// </summary>
		/// <param name="Bullet"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[ClientRpc]
		void RpcRocketFired(NetworkRocket Bullet, Vector3 _bulletVector, float _bulletSpeed)
		{
			Bullet.GetComponent<NetworkRocket>().SetupProjectile(_netPlayer.username, hasAuthority);

			Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
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
                        if(!_tpManagerNew.carTheftCamera.activeInHierarchy)
                            _tpManagerNew.carTheftCamera.SetActive(true);
                    }
                    else
                    {
                        if(_tpManagerNew.carTheftCamera.activeInHierarchy)
                            _tpManagerNew.carTheftCamera.SetActive(false);
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
