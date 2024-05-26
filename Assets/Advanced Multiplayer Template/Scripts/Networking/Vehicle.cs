using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Vehicle : NetworkBehaviour
{
    public bool canDrive = false;
    [SyncVar] public bool isControlledByCarAi = false;
}
