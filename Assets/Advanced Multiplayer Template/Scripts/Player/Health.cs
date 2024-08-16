using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using UnityEngine.InputSystem;
using StarterAssets;

public class Health : NetworkBehaviour
{
    public string username;

    public const int maxHealth = 100;
    [SyncVar(hook = nameof(OnChangeHealth))] public int currentHealth = maxHealth;
    public RectTransform healthbar;

    [SerializeField] public Transform _HealthCanvas;
    private static Transform _camera;
    public static Health localPlayer;

    [SerializeField] private string deathAnimationName;

    private float _respawnTimer;

    //public string attackerUsername; //sync var is to slow to update killer name on time so we will update this by rpc

    [Space]
    [SyncVar] public bool isDeath = false;

    [Space]
    [Header("UI")]
    public GameObject killMessagePrefab;
    public GameObject deathCanvasPrefab;

    public Transform playerRagdoll;
    private bool isFallingFromCar = false;
    [HideInInspector] public bool isFallingFromAircraft = false;
    [SyncVar] public bool waitingForFallDamage = false;

    [Header("Parachute")]
    public GameObject parachute;
    public GameObject parachuteReleasedPrefab;

    [Header("Drop Money")]
    public GameObject droppedMoneyPrefab;

    GameObject[] playerSpawnPoints;

    NetPlayer _netPlayer;

    PlayerAI playerAI;
    CapsuleCollider myCapsuleCollider;
    UnityEngine.AI.NavMeshAgent navMeshAgent;
    Animator animator;
    PlayerInteraction playerInteraction;
    CharacterController characterController;
    ThirdPersonController thirdPersonController;
    RedicionStudio.InventorySystem.PlayerInventoryModule playerInventoryModule;



    private void Start()
    {
        username = this.GetComponent<NetPlayer>().username;

        playerSpawnPoints = GameObject.FindGameObjectsWithTag("PlayerSpawnPoint");

        playerAI = GetComponent<PlayerAI>();
        playerInteraction = GetComponent<PlayerInteraction>();
        myCapsuleCollider = GetComponent<CapsuleCollider>();
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        thirdPersonController = GetComponent<ThirdPersonController>();
        playerInventoryModule = GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>();
    }

    private void Update()
    {
        if (localPlayer != null && !isLocalPlayer)
        {
            _HealthCanvas.LookAt(_HealthCanvas.position + _camera.rotation * Vector3.forward,
                _camera.rotation * Vector3.up);
        }

        if (isFallingFromCar)
        {
            playerRagdoll.position = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
        }
        else if (isFallingFromAircraft)
        {
            parachute.SetActive(true);
        }
        if (isFallingFromAircraft == false)
        {
            parachute.SetActive(false);
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer(); // run base of overridden class

        localPlayer = this;


        //_camera = FindObjectOfType<Camera>().transform;
        _camera = GameObject.Find("MainCamera").transform;

        if (isLocalPlayer)
            _HealthCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// processing damage taken and what type
    /// </summary>
    /// <param name="amount">interger amount of damage taken</param>
    /// <param name="attackType">enum type of damage taken</param>
    /// <param name="attackerName">name of the source of damage</param>
    public void TakeDamage(int amount, AttackType attackType, string attackerName)
    {
        if (!isServer)
        {
            return;
        }

        /*if (isDeath)
            return;*/

        currentHealth -= amount;
        print(gameObject.name + "'s" + " health = " + currentHealth);
        if (playerAI.isSetAsAi == true)
        {
            playerAI.Run();
        }
        if (currentHealth <= 0)
        {
            print("Player: " + gameObject.name + " is dead");
            isDeath = true;

            currentHealth = maxHealth;
            _respawnTimer = 7.30f;

            //RpcPlayAnimation(deathAnimationName);
            RpcFallingDown();
            if (playerAI.isSetAsAi == true)
            {
                navMeshAgent.isStopped = true;
                healthbar.parent.gameObject.SetActive(false);
                myCapsuleCollider.isTrigger = true;
                RpcNPCDie();
            }
            else
            {
                RpcShowDeathScreen(attackerName);

                RpcDeathConfirmation(attackerName, attackType);
                isDeath = false;
                StartCoroutine(Respawn());
            }
            Vector3 droppedMoneyPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z + 2f);
            Quaternion droppedMoneyRotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);

            GameObject droppedMoney = Instantiate(droppedMoneyPrefab, droppedMoneyPosition, droppedMoneyRotation) as GameObject;

            NetworkServer.Spawn(droppedMoney);
        }
    }

    /// <summary>
    /// processing death message
    /// </summary>
    /// <param name="killerName">name of entity, player or object that caused death</param>
    /// <param name="attackType">enum type of damage that killed player</param>
    [ClientRpc]
    void RpcDeathConfirmation(string killerName, AttackType attackType)
    {
        Instantiate(killMessagePrefab).GetComponent<KillMessage>().ShowKillMessage(killerName, username, attackType);
    }

    /// <summary>
    /// processing death screen
    /// </summary>
    /// <param name="attackerName">name of entity, player or object that caused death</param>
    [ClientRpc]
    void RpcShowDeathScreen(string attackerName)
    {
        isDeath = true;
        if (isLocalPlayer)
        {
            Instantiate(deathCanvasPrefab).GetComponent<DeathScreen>().SetUpDeathScreen(this.transform.position, attackerName);
        }
    }

    /// <summary>
    /// coroutine for respawning the player
    /// </summary>
    /// <returns></returns>
    IEnumerator Respawn()
    {
        _respawnTimer -= 1;

        yield return new WaitForSeconds(7.30f);


        RpcRespawn();
    }

    void OnChangeHealth(int currenthealth, int health)
    {
        if (healthbar != null)
            healthbar.sizeDelta = new Vector2(health * 5, healthbar.sizeDelta.y);
        currentHealth = health;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            StartCoroutine(WaitUntilSuitableSpawnPointFound());

            IEnumerator WaitUntilSuitableSpawnPointFound()
            {
                int index = Random.Range(0, playerSpawnPoints.Length);
                GameObject currentSpawnPoint = playerSpawnPoints[index];

                yield return new WaitUntil(() => currentSpawnPoint.GetComponent<PlayerSpawnPoint>().isPlayerInTrigger == false);

                transform.position = currentSpawnPoint.transform.position;
                transform.rotation = new Quaternion(0, 0, 0, 0);

                StopCoroutine(WaitUntilSuitableSpawnPointFound());
            }
        }

        //this.GetComponent<Animator>().Rebind(); this messes up animations
        isDeath = false;
        if (GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().inCar)
            GetComponent<PlayerInteraction>().ForceExitVehicle();
        GetComponent<CapsuleCollider>().enabled = true;
        animator.enabled = true;
        animator.Play("Idle Walk Run Blend");
    }



    /// <summary>
    /// nullifies player health script upon death
    /// </summary>
    private void OnDestroy()
    {
        if (localPlayer == this)
        {
            localPlayer = null;
        }
    }

    /// <summary>
    /// collision for taking damage
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (waitingForFallDamage)
        {
            if (collision != null)
            {
                if (isServer)
                {
                    waitingForFallDamage = false;
                    TakeDamage(100, AttackType.Plane, "World");
                }
            }
        }
    }

    /// <summary>
    /// plays death animation
    /// </summary>
    /// <param name="Animation">animation of player's death</param>
    [ClientRpc]
    public void RpcPlayAnimation(string Animation)
    {
        this.GetComponent<Animator>().Play(Animation);
    }

    [ClientRpc]
    public void RpcFallingDown()
    {
        GetComponent<CapsuleCollider>().enabled = false;
        animator.enabled = false;
    }

    /// <summary>
    /// manages death after health reaches 0
    /// </summary>
    [ClientRpc]
    public void RpcNPCDie()
    {
        navMeshAgent.isStopped = true;
        healthbar.parent.gameObject.SetActive(false);
        myCapsuleCollider.isTrigger = true;
    }

    /// <summary>
    /// puts player in a falling state after leaving a flying vehicle while in the air
    /// </summary>
    /// <param name="vehicleType">ID number of vehicle</param>
    public void FallFromVehicle(int vehicleType)
    {
        CmdFallFromVehicle(vehicleType);
    }

    [Command]
    public void CmdFallFromVehicle(int vehicleType)
    {
        characterController.enabled = false;
        thirdPersonController.enabled = false;
        if (vehicleType == 1)
        {
            isFallingFromCar = true;
            animator.SetTrigger("FallFromVehicle");
        }
        else if (vehicleType == 2)
        {
            isFallingFromCar = true;
            animator.SetBool("FreeFall", true);
            animator.Play("InAir");
            waitingForFallDamage = true;
        }
        RpcFallFromVehicle(vehicleType);
    }

    [ClientRpc]
    public void RpcFallFromVehicle(int vehicleType)// vehicleType, 1 = Car, 2 = Aircraft
    {
        characterController.enabled = false;
        characterController.enabled = false;
        if (vehicleType == 1)//Car
        {
            isFallingFromCar = true;
            animator.SetTrigger("FallFromVehicle");

            StartCoroutine(StandUpCar());
        }
        else if (vehicleType == 2)//Aircraft
        {
            isFallingFromCar = true;
            animator.SetBool("FreeFall", true);
            animator.Play("InAir");
            waitingForFallDamage = true;

            StartCoroutine(StandUpPlane());
        }
    }

    /// <summary>
    /// Coroutine for leaving ground vehicle
    /// </summary>
    /// <returns></returns>
    IEnumerator StandUpCar()
    {
        yield return new WaitForSeconds(5.20f);

        myCapsuleCollider.enabled = true;
        animator.enabled = true;
        isFallingFromCar = false;
        animator.ResetTrigger("FallFromVehicle");
        //reanable player movement
        characterController.enabled = true;
        thirdPersonController.enabled = true;
        myCapsuleCollider.enabled = true;

        playerInventoryModule.inCar = false;

        playerInteraction.inVehicle = false;
    }

    // <summary>
    /// Coroutine for leaving air vehicle
    /// </summary>
    /// <returns></returns>
    IEnumerator StandUpPlane()
    {
        yield return new WaitForSeconds(0.17f);

        myCapsuleCollider.enabled = true;
        animator.enabled = true;
        isFallingFromCar = false;
        animator.SetBool("FreeFall", false);
        animator.Play("Idle Walk Run Blend");
        //reanable player movement
        characterController.enabled = true;
        thirdPersonController.enabled = true;
        myCapsuleCollider.enabled = true;

        playerInventoryModule.inCar = false;

        playerInteraction.inVehicle = false;
    }

    /// <summary>
    /// when the player lands on the ground after falling from an aircraft, the parachute is deactivated and the player returns to their grounded state
    /// </summary>
    public void ReleaseParachute()
    {
        isFallingFromAircraft = false;
        playerInventoryModule.usesParachute = false;
        CmdReleaseParachute(transform.position, transform.rotation);
    }


    [Command]
    void CmdReleaseParachute(Vector3 _position, Quaternion _rotation)
    {
        isFallingFromAircraft = false;
        /*GameObject ReleasedParachute = Instantiate(parachuteReleasedPrefab, _position, _rotation) as GameObject;
        NetworkServer.Spawn(ReleasedParachute, connectionToClient);*/

        RpcReleaseParachute();
    }

    [ClientRpc]
    void RpcReleaseParachute()
    {
        isFallingFromAircraft = false;
        //reanable player movement
        parachute.SetActive(false);
        animator.Play("Idle Walk Run Blend");
    }
}

/// <summary>
/// the type of objects that can damage the player and other entities
/// </summary>
public enum AttackType : byte
{
    Minigun,
    Rockets,
    Exploded,
    Gun,
    Car,
    Plane,
}

