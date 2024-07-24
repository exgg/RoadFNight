using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using System;

public class Player : NetworkBehaviour {

	#region Calls

	private Health _health;

	#endregion
	
	[Header("Player Modules")]
	public RedicionStudio.InventorySystem.PlayerInventoryModule playerInventory;
	public PlayerNutritionModule playerNutrition;

	[Space]
	[SyncVar] public int id;
	[SyncVar] public string username;
	[SyncVar] public byte status;
	[SyncVar] public int funds;
    [SyncVar] public int experiencePoints;
    [HideInInspector] public Instance instance;
	[HideInInspector] public PropertyArea propertyArea;

	public static readonly Dictionary<int, Player> onlinePlayers = new Dictionary<int, Player>();

	public static Player localPlayer;

	public List<GameObject> placedObjects = new List<GameObject>();

    public static event Action<Player, string> OnMessage;

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

        // Sets up the action to be performed when a place request is made in the game.
        // The CmdPlace command is called with the current placeable object's details.
        BSystem.BSystem.OnPlaceRequestAction = () => {
			CmdPlace(BSystem.BSystem.currentPlaceableSO.uniqueName, BSystem.BSystem.position, BSystem.BSystem.rotation);
		};
	}

    /// <summary>
    /// Seems to be disabling everything for the AI on the player controller...
    /// But surely it would be more efficient just having a seperate prefab for the AI ?
    /// </summary>
	private void Start() {
        if (GetComponent<PlayerAI>().isSetAsAi == true)
        {
            GetComponent<RedicionStudio.InventorySystem.PlayerInventoryModule>().enabled = false;
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

		if (!isLocalPlayer && !isServerOnly) {
			_nameplateText.text = username + (status == 100 ? "\n<color=#6ab04c><developer></color>" : string.Empty);
		}
		
		LoadPlacement();
	}
    
    private void Update() {
	    if (localPlayer != null && !isLocalPlayer)
	    {
		    if (this.GetComponent<Health>().isDeath == false)
		    {
			    this.GetComponent<Health>()._HealthCanvas.gameObject.SetActive(true);
			    if (!this.GetComponent<PlayerAI>().isSetAsAi)
				    _nameplateCanvas.gameObject.SetActive(true);
			    else
				    _nameplateCanvas.gameObject.SetActive(false);
			    _nameplateCanvas.LookAt(_nameplateCanvas.position + _camera.rotation * Vector3.forward,
				    _camera.rotation * Vector3.up);
		    }
		    else
		    {
			    this.GetComponent<Health>()._HealthCanvas.gameObject.SetActive(false);
			    _nameplateCanvas.gameObject.SetActive(false);
		    }
	    }
	    else if (localPlayer != null && isLocalPlayer)
		    this.GetComponent<ExperienceManager>().ExperienceUI.SetActive(true);
    }

    private void LoadPlacement()
    {
	#if UNITY_SERVER// || UNITY_EDITOR // (Server)
		int propertyAreaId = PropertyArea.Assign(instance.uniqueName, id);
		TargetAreaId(propertyAreaId);
		propertyArea = PropertyArea.GetPropertyArea(instance.uniqueName, id); // ?????
	#endif

	    /*
	     * Retrieves and places objects that the player has placed in the game world.
	     * This function runs only on the server or within the Unity Editor for testing purposes. otherwise will never call
	     * It gets the placed objects data from the MasterServer and instantiates them in the game world
	     * at the correct positions and orientations, then adds them to the player's placed objects list.
	     */
	#if UNITY_SERVER || UNITY_EDITOR
	    
	    if (isServer) {
		    MasterServer.MSClient.GetPlacedObjects(id, (placedObjectsData) => {
			    for (int i = 0; i < placedObjectsData.Length; i++) {
				    Vector3 position = propertyArea.transform.position + new Vector3(
					    placedObjectsData[i].x,
					    placedObjectsData[i].y,
					    placedObjectsData[i].z);
				    GameObject gO = BSystem.PlaceableObject.Place(id, placedObjectsData[i].placeableSOUniqueName, position,
					    new Quaternion(placedObjectsData[i].rotX,
						    placedObjectsData[i].rotY,
						    placedObjectsData[i].rotZ,
						    placedObjectsData[i].rotW)); // ?
				    placedObjects.Add(gO);
			    }
		    });
	    }
	#endif
    }
    
    /// <summary>
    /// Attempts to place a placeable object at a specified position and rotation.
    /// Checks if the player has the object, sufficient funds, and if the position is within the allowed area.
    /// If conditions are met, the object is placed, added to the list of placed objects, and the cost is deducted from the player's funds.
    /// </summary>
    /// <param name="placeableSOUniqueName">Unique ID for the object to be placed</param>
    /// <param name="position">The position of the object to be placed</param>
    /// <param name="rotation">The rotation to apply to the placed object</param>
    [Command]
	private void CmdPlace(string placeableSOUniqueName, Vector3 position, Quaternion rotation) {
		BSystem.PlaceableSO placeableSO = BSystem.PlaceableSO.GetPlaceableSO(placeableSOUniqueName);
		if (placeableSO == null) 
			return;
		
		if (funds >= placeableSO.price) {
			if (propertyArea == null || !propertyArea.Contains(new Bounds(position, Vector3.one * .1f))) {
				return;
			}
			// place object and take funds
			GameObject gO = BSystem.PlaceableObject.Place(id, placeableSOUniqueName, position, rotation); // ?
			placedObjects.Add(gO);
			funds -= placeableSO.price;
		}
	}

    /// <summary>
    /// Edits the position and rotation of a placeable object with a specified ID.
    /// Validates if the object exists, belongs to the player, and the new position is within the allowed area.
    /// If conditions are met, updates the object's position and rotation on the server and clients.
    /// </summary>
    /// <param name="id">The unique ID of the placeable object to be edited</param>
    /// <param name="newPosition">The new position to place the object</param>
    /// <param name="newRotation">The new rotation to apply to the object</param>
	[Command]
	public void CmdEdit(uint id, Vector3 newPosition, Quaternion newRotation) { // TODO: refactor
		// Check if the object exists in the network and retrieve its NetworkIdentity
		if (NetworkServer.spawned.TryGetValue(id, out NetworkIdentity identity)) {
			// Validate if the object is within the property area, belongs to the player, and is of the correct type
			if (propertyArea == null || !identity.TryGetComponent(out BSystem.PlaceableObject placeableObject) || placeableObject.ownerId != this.id ||
				!propertyArea.Contains(new Bounds(newPosition, Vector3.one * .1f))) {
				return;
			}
			// Update the object's position and rotation
			identity.transform.position = newPosition;
			identity.transform.rotation = newRotation;
			// Inform clients of the update
			RpcEditUpdate(id, newPosition, newRotation);
		}
	}

	/// <summary>
	/// Updates the position and rotation of a placeable object on all clients.
	/// Checks if the object exists on the client, then updates its transform properties.
	/// </summary>
	/// <param name="id">The unique ID of the placeable object to be updated</param>
	/// <param name="newPosition">The new position to place the object</param>
	/// <param name="newRotation">The new rotation to apply to the object</param>
	[ClientRpc]
	public void RpcEditUpdate(uint id, Vector3 newPosition, Quaternion newRotation) {
		// Check if the object exists on the client and retrieve its NetworkIdentity
		if (NetworkClient.spawned.TryGetValue(id, out NetworkIdentity identity)) {
			// Update the object's position and rotation on the client
			identity.transform.position = newPosition;
			identity.transform.rotation = newRotation;
		}
	}

	/// <summary>
	/// Deletes a placeable object on the server and updates the client's state.
	/// Checks if the object exists and is owned by the player, then removes it from the placedObjects list and destroys it.
	/// Adds the object's sell price back to the player's funds.
	/// </summary>
	/// <param name="id">The unique ID of the placeable object to be deleted</param>
	[Command]
	public void CmdEditDelete(uint id) {
		if (NetworkServer.spawned.TryGetValue(id, out NetworkIdentity identity)) {
			if (propertyArea == null || !identity.TryGetComponent(out BSystem.PlaceableObject placeableObject) || placeableObject.ownerId != this.id) {
				return;
			}

			placedObjects.Remove(placeableObject.gameObject);
			NetworkServer.Destroy(identity.gameObject);
			funds += placeableObject.placeableSO.sellPrice;
		}
	}

    public void SetExperience(int _xp)
    {
        if (isServer)
        {
            experiencePoints += _xp;
        }
    }

  
    
	/// <summary>
	/// Receives the property area ID for the player.
	/// This method is called on the client that owns the player object.
	/// </summary>
	[TargetRpc]
	private void TargetAreaId(int id) {
		Debug.Log(id);
		PropertyArea.myIndex = id;
	}

	/// <summary>
	/// Toggles off the players controller and removes them from the server and the game
	/// </summary>
	private void OnDestroy() {
		_ = onlinePlayers.Remove(id);

		if (localPlayer == this) {
			localPlayer = null;
		}

		if (isServer) {
			instance.RemovePlayer(id);
		}

		if (isLocalPlayer) {
			TPController.TPCameraController.LockCursor(false);
			PropertyArea.myIndex = -1;
		}

#if UNITY_SERVER || UNITY_EDITOR
		if (isServer) {
			// save placed objects to master server
			if (placedObjects.Count > 0) {
				MasterServer.MServer.PlacedObjectJSONData[] placedObjectsData = new MasterServer.MServer.PlacedObjectJSONData[placedObjects.Count];
				for (int i = 0; i < placedObjects.Count; i++) {
					// Calculate relative position to the property area
					Vector3 position = placedObjects[i].transform.position - propertyArea.transform.position;
					placedObjectsData[i] = new MasterServer.MServer.PlacedObjectJSONData {
						placeableSOUniqueName = placedObjects[i].GetComponent<BSystem.PlaceableObject>().placeableSOUniqueName,
						x = position.x,
						y = position.y,
						z = position.z,
						rotX = placedObjects[i].transform.rotation.x,
						rotY = placedObjects[i].transform.rotation.y,
						rotZ = placedObjects[i].transform.rotation.z,
						rotW = placedObjects[i].transform.rotation.w,
					};
					// destroy placed objects on the server
					NetworkServer.Destroy(placedObjects[i].gameObject);
				}
				// save placed objects data to master server
				MasterServer.MSClient.SavePlacedObjects(id, placedObjectsData);
			}
		}

		if (isServer) {
			// send account packet to the master server
			MasterServer.MSManager.SendPacket(new MasterServer.AccountDataResponsePacket { Id = id, Funds = funds, OwnsProperty = true, Nutrition = playerNutrition.value, ExperiencePoints = experiencePoints });
			
			// Prepare and save inventory data to master server
			MasterServer.MServer.InventoryJSONData[] inventoryJSONData = new MasterServer.MServer.InventoryJSONData[playerInventory.slots.Count];
			for (int i = 0; i < playerInventory.slots.Count; i++) {
				inventoryJSONData[i] = new MasterServer.MServer.InventoryJSONData {
					hash = playerInventory.slots[i].item.hash,
					amount = playerInventory.slots[i].amount,
					shelfLife = playerInventory.slots[i].item.currentShelfLifeInSeconds
				};
			}
			MasterServer.MSClient.SaveInventory(id, inventoryJSONData);
		}
#endif
		// Refactor

#if UNITY_SERVER// || UNITY_EDITOR // (Server)
		propertyArea?.AssignTo(0);
		propertyArea = null;
#endif
	}

	[SerializeField] private Transform _nameplateCanvas;
	[SerializeField] private TextMeshProUGUI _nameplateText;
	private static Transform _camera;



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
