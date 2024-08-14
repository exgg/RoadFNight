using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : NetworkBehaviour
{
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

    public ShootWeapon()
    {

    }

    [Command]
    public void CmdAim()
    {
        TPControllerManager.aimValue = 1;
    }
}
