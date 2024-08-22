using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interaction : MonoBehaviour
{

    public List<LayerMask> layerMasks = new List<LayerMask>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach (LayerMask mask in layerMasks)
        {
            RaycastHit detect;
            if (Physics.Raycast(transform.position, transform.forward, out detect, mask))
            {
                Debug.Log("This is a " + LayerMask.LayerToName(mask));
            }
        }

    }

    // public void OpenShop() 
    // {
    //      
    // }

    // public void EnterVehicle() {}

    // public void ExitVehicle() {}

    // public void KickOtherPlayerOut() {}

    // public void KickedOut() {}

    // void BlockPlayer

}
