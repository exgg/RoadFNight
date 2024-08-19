using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cinemachine;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using StarterAssets;
using UnityEngine.Animations.Rigging;

public class ManageTPController : NetworkBehaviour
{

    [Header("Player")]

    private InputHandler _input;

    [Header("Camera")]
    public Transform target;

    [Header("Camera Modes")]
    public bool isFirstPerson = false;

    [Header("Head Mesh")]
    public GameObject[] headMeshes;

    [Header("Loading Screen")]
    public GameObject loadingScreenPrefab;

    [Header("Car Theft")]
    public GameObject carTheftCamera;

    void Start()
    {
        if (isLocalPlayer)
        {
            Instantiate(loadingScreenPrefab);
            _input = GameObject.FindGameObjectWithTag("InputManager").GetComponent<InputHandler>(); // change to FindObjectOfType
            CharacterController cc = GetComponent<CharacterController>();
            cc.enabled = true;
            ThirdPersonController tpc = GetComponent<ThirdPersonController>();
            tpc.enabled = true;
            StickCameraToPlayer();
        }
    }


    void Update()
    {
        if (isLocalPlayer)
        {
            //Toggle camera mode
            // if (Keyboard.current.vKey.wasPressedThisFrame || _input.toggleCamera)
            // {
            //     /*if(isFirstPerson)
            //     {
            //         isFirstPerson = false;
            //         foreach (GameObject meshes in headMeshes)
            //         {
            //             StopCoroutine(HideHeadMesh());
            //             meshes.SetActive(true);
            //         }
            //     }
            //     else
            //     {
            //         isFirstPerson = true;
            //         foreach(GameObject meshes in headMeshes)
            //         {
            //             //meshes.SetActive(false);
            //             StartCoroutine(HideHeadMesh());
            //         }
            //     }*/
            // }


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
    }


    /// <summary>
    /// Toggles the camera to the players character camera and turns off the vehicle camera for the cinema-chine
    /// sets the camera to follow the player character
    /// </summary>
    public void StickCameraToPlayer()
    {
        Camera vehicleCam = GetVehicleCamera();
        vehicleCam.enabled = false;

        CinemachineVirtualCamera playerCam = GetPlayerCamera();
        playerCam.enabled = true;
        playerCam.Follow = target;
    }

    /// <summary>
    /// Toggles the camera to the vehicle camera and turns off the players character camera for the cinema-chine
    /// sets the camera to follow the vehicle the player is in
    /// </summary>
    public void StickCameraToVehicle(Transform followTransform)
    {
        Camera vehicleCam = GetVehicleCamera();
        vehicleCam.GetComponent<CameraFollow>().car = followTransform;
        vehicleCam.enabled = true;

        CinemachineVirtualCamera playerCam = GetPlayerCamera();
        playerCam.enabled = false;
    }

    /// <summary>
    /// Finds the vehicle camera via GameObject.Find then gets its camera component and returns it
    /// </summary>
    /// <returns></returns>
    Camera GetVehicleCamera()
    {
        GameObject pfc = GameObject.Find("Camera_Vehicle");
        Camera cvc = pfc.GetComponent<Camera>();
        return cvc;
    }
    /// <summary>
    /// Finds the player camera via GameObject.Find and finds the cinema-chine camera 
    /// </summary>
    /// <returns></returns>
    CinemachineVirtualCamera GetPlayerCamera()
    {
        GameObject pfc = GameObject.Find("PlayerFollowCamera");
        CinemachineVirtualCamera cvc = pfc.GetComponent<CinemachineVirtualCamera>();
        return cvc;
    }

    /// <summary>
    /// Hides the head mesh if playing within first person
    /// </summary>
    /// <returns></returns>
    IEnumerator HideHeadMesh()
    {
        yield return new WaitForSeconds(0.7f);

        foreach (GameObject meshes in headMeshes)
        {
            if (isFirstPerson == true)
                meshes.SetActive(false);
            else
                meshes.SetActive(true);
        }
    }
}
