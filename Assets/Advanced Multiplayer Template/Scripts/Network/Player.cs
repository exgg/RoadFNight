using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using System;

public class Player : NetworkBehaviour {

	#region Calls

	private Health _health;
	private PlayerAI _playerAI;
	private ExperienceManager _experienceManager;
	
	#endregion
	
	[Header("Player Modules")]
	public PlayerInventoryModule playerInventory;
	public PlayerNutritionModule playerNutrition;

	[Space]
	[SyncVar] public int id;
	[SyncVar] public string username;
	[SyncVar] public byte status;
	[SyncVar] public int funds;
    [SyncVar] public int experiencePoints;
    [HideInInspector] public Instance instance;
    [HideInInspector] public PropertyArea propertyArea;
    
	public readonly Dictionary<int, Player> onlinePlayers = new Dictionary<int, Player>();

	public static Player localPlayer;

	

    public static event Action<Player, string> OnMessage;

    [SerializeField] private Transform _nameplateCanvas;
    [SerializeField] private TextMeshProUGUI _nameplateText;
    private static Transform _camera;

    private bool _isDead;
    public bool IsDead
    {
	    get => _isDead;
		
	    set
	    {
		    if (_isDead == value) return;
		    
		    this._isDead = value;
		    HandleDeath();
	    }
	    
    }
    
      /// <summary>
    /// Initializes the local player when they start.
    /// Sets up necessary client-side states, locks the cursor, and assigns the camera.
    /// Also sets up the action for placing objects in the game world.
    /// </summary>
    public override void OnStartLocalPlayer() {
		base.OnStartLocalPlayer(); // Calls the base method to ensure any base initialization is also performed.

		localPlayer = this;  // Assigns this instance as the local player.
		
		MasterServer.MSClient.State = MasterServer.MSClient.NetworkState.InGame;	// Updates the state of the MasterServer client to indicate the player is in-game.
		TPController.TPCameraController.LockCursor(true);
        //_camera = FindObjectOfType<Camera>().transform;
        _camera = GameObject.Find("MainCamera").transform;
	}

    /// <summary>
    /// Seems to be disabling everything for the AI on the player controller...
    /// But surely it would be more efficient just having a seperate prefab for the AI ?
    /// </summary>
	private void Start() {
	    InitializeCalls();
		InitialisePlayer();
	}

    private void HandleDeath()
    {
	    Debug.Log("You have died");
	    _health._HealthCanvas.gameObject.SetActive(false);
	    _nameplateCanvas.gameObject.SetActive(false);
    }

    #region Setup On Join
    private void InitialisePlayer()
    {
	    _isDead = _health.isDeath;
	    
	    TemporarySetupAIorPlayer();
	    Debug.Log("Is Dead:" + IsDead);
	    
	    if (!isLocalPlayer && !isServerOnly) { // setup player username nameplate
		    _nameplateText.text = username + (status == 100 ? "\n<color=#6ab04c><developer></color>" : string.Empty);
	    }
	    
	    SetupFloatingUI();
    }

    private void SetupFloatingUI()
    {
	    _health._HealthCanvas.gameObject.SetActive(true);
	    _nameplateCanvas.gameObject.SetActive(!_playerAI.isSetAsAi);
	    _nameplateCanvas.LookAt(_nameplateCanvas.position + _camera.rotation * Vector3.forward,
		    _camera.rotation * Vector3.up);
    }
    private void InitializeCalls()
    {
	    _health = GetComponent<Health>();
	    _playerAI = GetComponent<PlayerAI>();
	    _experienceManager = GetComponent<ExperienceManager>();
    }
    
    /// <summary>
    /// Setup whether the player is AI or a Player. This is an odd way of doing AI maniplulation as behaviours is difficult
    /// to change if this stays, so I have moved it to a temporary method
    /// </summary>
    private void TemporarySetupAIorPlayer()
    {
	    if (GetComponent<PlayerAI>().isSetAsAi == true)
	    {
		    GetComponent<PlayerInventoryModule>().enabled = false;
		    return;
	    }
	    onlinePlayers[id] = this;

	    if (!isLocalPlayer) {
		    Destroy(GetComponent<TPController.TPCharacterController>());
	    }
	    else {
		    Destroy(_nameplateCanvas.gameObject);
		    if (username != "")
		    {
			    GetComponent<PlayerAI>().enabled = false;
			    GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
		    }
	    }
	    
	    if (localPlayer != null && isLocalPlayer)
		    this.GetComponent<ExperienceManager>().ExperienceUI.SetActive(true);
    }
    
   
    #endregion

	
    public void SetExperience(int _xp)
    {
        if (isServer)
        {
            experiencePoints += _xp;
        }
    }


    [Command]
    public void CmdSend(string message)
    {
        if (message.Trim() != "")
            RpcReceive(message.Trim());
    }

    [ClientRpc]
    public void RpcReceive(string message)
    {
        OnMessage?.Invoke(this, message);
    }
}
