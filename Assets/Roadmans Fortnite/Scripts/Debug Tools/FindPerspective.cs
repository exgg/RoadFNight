using System.Collections;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.ThirdPersonController.Items;
using UnityEngine;

public class FindPerspective : MonoBehaviour
{
    private ThirdPersonPerspectiveItem _thirdPersonPerspectiveItem;
    void Start()
    {
        _thirdPersonPerspectiveItem = FindObjectOfType<ThirdPersonPerspectiveItem>();
        
        Debug.Log($"The parent of the ThirdPersonPerspectiveItem is {_thirdPersonPerspectiveItem.gameObject}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
