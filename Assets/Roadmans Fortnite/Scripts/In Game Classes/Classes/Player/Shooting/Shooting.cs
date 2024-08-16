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

        public void ShootWeapon()
        {

        }

        [Command]
        public void CmdAim()
        {
            // TPControllerManager.aimValue = 1;
        }
    }
}
