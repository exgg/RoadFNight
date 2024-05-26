using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cinemachine;
using StarterAssets;
using UnityEngine.Animations.Rigging;

public class ManageTPController : NetworkBehaviour
{
    public WeaponManager CurrentWeaponManager;
    public GameObject Weapons;
    public List<Transform> AllFoundWeapons = new List<Transform>();
    public Transform CurrentWeaponBulletSpawnPoint;
    public Transform CurrentCartridgeEjectSpawnPoint;
    bool hasActiveWeapon;
    [HideInInspector] bool isShooting = false;
    [Header("Player")]
    public Rig PlayerRig;
    public Transform SecondHandRig_target;
    public ThirdPersonController thirdPersonController;
    private StarterAssetsInputs _input;
    public Animator PlayerAnimator;
    [Header("Camera")]
    public GameObject IdleCamera;
    public GameObject WeaponIdleCamera;
    public GameObject WeaponAimCamera;
    public GameObject FirstPersonIdleCamera;
    public Transform target;
    [Header("Camera Modes")]
    public bool isFirstPerson = false;
    [Header("Head Mesh")]
    public GameObject[] headMeshes;
    [Header("Loading Screen")]
    public GameObject loadingScreenPrefab;
    [Header("Car Theft")]
    public GameObject carTheftCamera;

    [SyncVar] public int aimValue;

    void Start()
    {
        if (isLocalPlayer)
        {
            Instantiate(loadingScreenPrefab);
            _input = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssetsInputs>();
            CharacterController cc = GetComponent<CharacterController>();
            cc.enabled = true;
            ThirdPersonController tpc = GetComponent<ThirdPersonController>();
            tpc.enabled = true;
            StickCameraToPlayer();

            WeaponIdleCamera = GameObject.Find("PlayerFollowCameraWeapon");
            if(WeaponIdleCamera != null)
                WeaponIdleCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            WeaponAimCamera = GameObject.Find("PlayerFollowCameraWeaponAim");
            if (WeaponAimCamera != null)
                WeaponAimCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            IdleCamera = GameObject.Find("PlayerFollowCamera");
            if (IdleCamera != null)
                IdleCamera.GetComponent<CinemachineVirtualCamera>().Follow = target;
            if (WeaponIdleCamera != null)
                WeaponIdleCamera.SetActive(false);
            if (WeaponAimCamera != null)
                WeaponAimCamera.SetActive(false);
            gameObject.AddComponent<AudioListener>();
        }
        foreach (Transform afw in Weapons.transform)
        {
            AllFoundWeapons.Add(afw);
        }

        foreach (Transform wps in AllFoundWeapons)
        {
            if (wps.gameObject.activeInHierarchy)
            {
                hasActiveWeapon = true;
            }
        }

        if(hasActiveWeapon == false)
        {
            this.GetComponent<Animator>().SetLayerWeight(1, 0);
        }
    }

    public void StickCameraToPlayer()
    {

        Camera vehicleCam = GetVehicleCamera();
        vehicleCam.enabled = false;

        CinemachineVirtualCamera playerCam = GetPlayerCamera();
        playerCam.enabled = true;
        playerCam.Follow = target;
    }

    public void StickCameraToVehicle(Transform followTransform)
    {
        Camera vehicleCam = GetVehicleCamera();
        vehicleCam.GetComponent<CameraFollow>().car = followTransform;
        vehicleCam.enabled = true;

        CinemachineVirtualCamera playerCam = GetPlayerCamera();
        playerCam.enabled = false;
    }

    Camera GetVehicleCamera()
    {
        GameObject pfc = GameObject.Find("Camera_Vehicle");
        Camera cvc = pfc.GetComponent<Camera>();
        return cvc;
    }
    CinemachineVirtualCamera GetPlayerCamera()
    {
        GameObject pfc = GameObject.Find("PlayerFollowCamera");
        CinemachineVirtualCamera cvc = pfc.GetComponent<CinemachineVirtualCamera>();
        return cvc;
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            //Toggle camera mode
            if(Keyboard.current.vKey.wasPressedThisFrame || _input.toggleCamera)
            {
                /*if(isFirstPerson)
                {
                    isFirstPerson = false;
                    foreach (GameObject meshes in headMeshes)
                    {
                        StopCoroutine(HideHeadMesh());
                        meshes.SetActive(true);
                    }
                }
                else
                {
                    isFirstPerson = true;
                    foreach(GameObject meshes in headMeshes)
                    {
                        //meshes.SetActive(false);
                        StartCoroutine(HideHeadMesh());
                    }
                }*/
            }

            if(CurrentWeaponManager != null)
            {
                Vector3 mouseWorldPosition = Vector3.zero;
                Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

                CurrentWeaponManager.debugTransform.position = ray.GetPoint(20f);
                mouseWorldPosition = ray.GetPoint(20f);
                /*if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, CurrentWeaponManager.aimColliderLayerMask))
                {
                    CurrentWeaponManager.debugTransform.position = raycastHit.point;
                    mouseWorldPosition = raycastHit.point;
                }
                else
                {
                    CurrentWeaponManager.debugTransform.position = ray.GetPoint(20f);
                    mouseWorldPosition = ray.GetPoint(20f);
                }*/

                Vector3 worldAimTarget = mouseWorldPosition;
                worldAimTarget.y = CurrentWeaponManager.Player.position.y;
                Vector3 aimDirection = (worldAimTarget - CurrentWeaponManager.Player.position).normalized;

                if (_input.aim)//Aim
                {
                    if (aimValue == 1)
                    {
                        this.GetComponent<PlayerInteractionModule>().playerInventory.isAiming = true;
                        CurrentWeaponManager.Player.forward = Vector3.Lerp(CurrentWeaponManager.Player.forward, aimDirection, Time.deltaTime * 20f);
                        if (PlayerRig != null)
                            PlayerRig.weight = 1;
                        if (SecondHandRig_target != null)
                        {
                            SecondHandRig_target.localPosition = CurrentWeaponManager.LeftHandPosition;
                            SecondHandRig_target.localRotation = CurrentWeaponManager.LeftHandRotation;
                        }
                        CurrentWeaponManager.isAiming = true;
                        CurrentWeaponManager.Crosshair.SetActive(true);
                        if (PlayerAnimator != null & isShooting == false)
                            PlayerAnimator.Play(CurrentWeaponManager.WeaponAimTriggerName);
                        if(thirdPersonController != null)
                        {
                            thirdPersonController.SetSensitivity(CurrentWeaponManager.aimSensitivity);
                            thirdPersonController.SetRotateOnMove(false);
                        }
                        if(isFirstPerson)
                        {
                            if (WeaponAimCamera != null)
                                WeaponAimCamera.SetActive(false);
                            if (FirstPersonIdleCamera != null)
                                FirstPersonIdleCamera.SetActive(true);
                        }
                        else
                        {
                            if (FirstPersonIdleCamera != null)
                                FirstPersonIdleCamera.SetActive(false);
                            if (WeaponAimCamera != null)
                                WeaponAimCamera.SetActive(true);
                        }
                    }
                }
                else
                {
                    this.GetComponent<PlayerInteractionModule>().playerInventory.isAiming = false;
                    if(isFirstPerson)
                        CurrentWeaponManager.Player.forward = Vector3.Lerp(CurrentWeaponManager.Player.forward, aimDirection, Time.deltaTime * 20f);
                    if (aimValue != 0)
                        CmdSetAimValue(0);
                    if (PlayerRig != null)
                        PlayerRig.weight = 0;
                    CurrentWeaponManager.isAiming = false;
                    if(isFirstPerson)
                    {
                        if (FirstPersonIdleCamera != null)
                            FirstPersonIdleCamera.SetActive(true);
                        if (WeaponAimCamera != null)
                            WeaponAimCamera.SetActive(false);
                    }
                    else
                    {
                        if (FirstPersonIdleCamera != null)
                            FirstPersonIdleCamera.SetActive(false);
                        if (WeaponAimCamera != null)
                            WeaponAimCamera.SetActive(false);
                    }
                    CurrentWeaponManager.Crosshair.SetActive(false);
                    if (thirdPersonController != null)
                    {
                        if(isFirstPerson)
                        {
                            thirdPersonController.SetSensitivity(CurrentWeaponManager.normalSensitivity);
                            thirdPersonController.SetRotateOnMove(false);
                        }
                        else
                        {
                            thirdPersonController.SetSensitivity(CurrentWeaponManager.normalSensitivity);
                            thirdPersonController.SetRotateOnMove(true);
                        }
                    }
                    if(PlayerAnimator != null)
                    {
                        PlayerAnimator.ResetTrigger(CurrentWeaponManager.WeaponAimTriggerName);
                        PlayerAnimator.SetTrigger(CurrentWeaponManager.WeaponIdleTriggerName);
                    }
                }
            }

            if (GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("KickOut"))
            {
                if (!carTheftCamera.activeInHierarchy)
                    carTheftCamera.SetActive(true);
            }
            else
            {
                if (carTheftCamera.activeInHierarchy)
                    carTheftCamera.SetActive(false);
            }
        }
        if (!isLocalPlayer)
        {
            if (aimValue == 1)//Aim
            {
                if(PlayerRig != null)
                    PlayerRig.weight = 1;
                if(SecondHandRig_target != null & CurrentWeaponManager != null)
                {
                    SecondHandRig_target.localPosition = CurrentWeaponManager.LeftHandPosition;
                    SecondHandRig_target.localRotation = CurrentWeaponManager.LeftHandRotation;
                }
                if(CurrentWeaponManager != null)
                    CurrentWeaponManager.isAiming = true;
                if (PlayerAnimator != null & CurrentWeaponManager != null & isShooting == false)
                    PlayerAnimator.Play(CurrentWeaponManager.WeaponAimTriggerName);
                if(thirdPersonController != null & CurrentWeaponManager != null)
                {
                    thirdPersonController.SetSensitivity(CurrentWeaponManager.aimSensitivity);
                    thirdPersonController.SetRotateOnMove(false);
                }
            }
            else if (aimValue == 0)
            {
                if (PlayerRig != null)
                    PlayerRig.weight = 0;
                if (CurrentWeaponManager != null)
                    CurrentWeaponManager.isAiming = false;
                if (thirdPersonController != null & CurrentWeaponManager != null)
                {
                    thirdPersonController.SetSensitivity(CurrentWeaponManager.normalSensitivity);
                    thirdPersonController.SetRotateOnMove(true);
                }
                if (PlayerAnimator != null & CurrentWeaponManager != null)
                {
                    PlayerAnimator.ResetTrigger(CurrentWeaponManager.WeaponAimTriggerName);
                    PlayerAnimator.SetTrigger(CurrentWeaponManager.WeaponIdleTriggerName);
                }
            }
        }

        if (this.GetComponent<PlayerAI>().isSetAsAi)
            Weapons.SetActive(false);
    }

    public void Shoot()
    {
        StopCoroutine(EndShooting());
        isShooting = true;
        GetComponent<Animator>().Play(CurrentWeaponManager.WeaponShootAnimationName);
        StartCoroutine(EndShooting());
    }

    IEnumerator EndShooting()
    {
        yield return new WaitForSeconds(CurrentWeaponManager.WeaponShootAnimationLength);

        isShooting = false;
    }

    [Command]
    void CmdSetAimValue(int value)
    {
        aimValue = value;

        RpcSetAimValue(value);
    }

    [ClientRpc]
    void RpcSetAimValue(int value)
    {
        aimValue = value;
    }

    IEnumerator HideHeadMesh()
    {
        yield return new WaitForSeconds(0.7f);

        foreach (GameObject meshes in headMeshes)
        {
            if(isFirstPerson == true)
                meshes.SetActive(false);
            else
                meshes.SetActive(true);
        }
    }
}
