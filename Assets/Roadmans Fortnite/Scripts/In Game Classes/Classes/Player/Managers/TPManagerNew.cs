using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Managers
{
    public class TpManagerNew : NetworkBehaviour
    {
        private InputHandler _input;

        
        // weapon assembly stuff
        [Header("Weapon Data")]
        public WeaponManager currentWeaponManager;
        public GameObject weapons;
        public List<Transform> weaponsFound = new List<Transform>();
        public Transform shellEjectionPoint;
        private bool _hasActiveWeapon;
        public bool _isShooting;
    
        // player rigging stuff
        [Header("Player Animation")] 
        public Rig playerRig;
        public Transform secondHandRigTarget;
        public Animator playerAnimator;

        
        // data for the camera. Aiming, idle, no weapon idle
        [Header("Camera")]
        public GameObject idleCamera;
        public Transform target;
        public GameObject weaponIdleCamera;
        public GameObject weaponAimCamera;
        public GameObject firstPersonControllerCamera;
        public GameObject firstPersonIdleCamera;

        private Camera _vehicleCamera;
        private CinemachineVirtualCamera _playerCamera;
    
        [Header("Camera Modes")] 
        public bool isFirstPerson = false;

        [Header("HeadMesh")] 
        public GameObject[] headMeshes;

        [Header("LoadingScreen")] 
        public GameObject loadingScreenPrefab; // this may need removing
        
        [Header("Car Theft Cams")] 
        public GameObject carTheftCamera;
        
        [SyncVar] 
        public int aimValue;
        
        public void Initialize()
        {
            weaponIdleCamera = GameObject.Find("PlayerFollowCameraWeapon");
            if(weaponIdleCamera != null)
                weaponIdleCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            
            weaponAimCamera = GameObject.Find("PlayerFollowCameraWeaponAim");
            if (weaponAimCamera != null)
                weaponAimCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            
            if (weaponIdleCamera != null)
                weaponIdleCamera.SetActive(false);
            
            if (weaponIdleCamera != null)
                weaponIdleCamera.SetActive(false);
            
            gameObject.AddComponent<AudioListener>();
            
            _vehicleCamera = GameObject.Find("Camera_Vehicle").GetComponent<Camera>();
            _playerCamera = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
            
            idleCamera = GameObject.Find("PlayerFollowCamera");
            
            if (idleCamera != null)
                idleCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            // toggle loading screen

            _input = GetComponent<InputHandler>();
        
        
            StickCameraToPlayer();

           
            // enable cc
            
            // blah blah
        }

        public void StickCameraToPlayer()
        {
            if(! _vehicleCamera || _playerCamera)
                return;

            _vehicleCamera.enabled = false;
            _playerCamera.enabled = true;
            _playerCamera.Follow = target;
        }

        public void StickCameraToVehicle(Transform followTransform)
        {
            if(!_playerCamera || !_vehicleCamera)
                return;

            var cameraFollow = _vehicleCamera.GetComponent<CameraFollow>();
        
            if(!cameraFollow)
                return;

            cameraFollow.car = followTransform;

            _vehicleCamera.enabled = true;
            _playerCamera.enabled = false;
        }

        IEnumerator HideHeadMesh()
        {
            yield return new WaitForSeconds(0.7f);

            foreach (GameObject mesh in headMeshes)
            {
                if(isFirstPerson)
                    mesh.SetActive(false);
                else
                    mesh.SetActive(true);
            }
        }
    }
}
