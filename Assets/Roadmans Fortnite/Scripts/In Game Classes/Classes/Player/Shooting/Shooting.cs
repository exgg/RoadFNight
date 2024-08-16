using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

namespace Roadmans_Fortnite.Scripts.Classes.Player.Shooting
{
    public class Shooting : NetworkBehaviour
    {
        public GameObject bulletPrefab;
        public GameObject rocketPrefab;
        public float bulletSpeed;
        Transform _bulletSpawnPointPosition;

        public WeaponManager CurrentWeaponManager;
        public GameObject Weapons;
        public List<Transform> AllFoundWeapons = new List<Transform>();
        public Transform CurrentWeaponBulletSpawnPoint;
        public Transform CurrentCartridgeEjectSpawnPoint;
        public GameObject cartridgeEjectPrefab;
        Transform _cartridgeEjectSpawnPointPosition;
        bool hasActiveWeapon;
        [HideInInspector] bool isShooting = false;

        public GameObject IdleCamera;
        public GameObject WeaponIdleCamera;
        public GameObject WeaponAimCamera;
        public GameObject FirstPersonIdleCamera;
        public Transform target;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AimWeapon()
        {
            // if (player NOT in any menu && NOT emoting && NOT dead && has weapon equiped) {CmdAim()}
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

            _bulletSpawnPointPosition = CurrentWeaponBulletSpawnPoint;
            _cartridgeEjectSpawnPointPosition = CurrentCartridgeEjectSpawnPoint;
            string currentBulletName = CurrentWeaponManager.WeaponBulletPrefab.name;
            //bulletSpeed = this.GetComponent<ManageTPController>().CurrentWeaponManager.BulletSpeed;
            Shoot();
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
            Bullet.GetComponent<NetworkBullet>().SetupProjectile(currentPlayerUsername(), hasAuthority);

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
            Bullet.GetComponent<NetworkRocket>().SetupProjectile(currentPlayerUsername(), hasAuthority);

            Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
        }


        /// <summary>
        /// Begins shooting, then starts a coroutine to stop shooting after the animation length.
        /// </summary>
        public void Shoot()
        {
            StopCoroutine(EndShooting());
            isShooting = true;
            GetComponent<Animator>().Play(CurrentWeaponManager.WeaponShootAnimationName);
            StartCoroutine(EndShooting());
        }

        /// <summary>
        /// Ends the shooting boolean after the coroutine has met the conditions on how long the shooting animation is.
        /// </summary>
        /// <returns></returns>
        IEnumerator EndShooting()
        {
            yield return new WaitForSeconds(CurrentWeaponManager.WeaponShootAnimationLength);

            isShooting = false;
        }

        [Command]
        public void CmdAim()
        {
            // TPControllerManager.aimValue = 1;
        }

        public string currentPlayerUsername()
        {
            return null; // GetComponent<PlayerNet>().username;
        }
    }
}
