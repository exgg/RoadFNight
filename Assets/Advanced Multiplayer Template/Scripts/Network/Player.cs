using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using System;

public class Player : NetworkBehaviour {

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

	public static Dictionary<int, Player> onlinePlayers = new Dictionary<int, Player>();

	public static Player localPlayer;

	public List<GameObject> placedObjects = new List<GameObject>();

    public static event Action<Player, string> OnMessage;

    [Command]
	private void CmdPlace(string placeableSOUniqueName, Vector3 position, Quaternion rotation) {
		BSystem.PlaceableSO placeableSO = BSystem.PlaceableSO.GetPlaceableSO(placeableSOUniqueName);
		if (placeableSO == null) {
			return;
		}
		if (funds >= placeableSO.price) {
			if (propertyArea == null || !propertyArea.Contains(new Bounds(position, Vector3.one * .1f))) {
				return;
			}
			// -
			GameObject gO = BSystem.PlaceableObject.Place(id, placeableSOUniqueName, position, rotation); // ?
			placedObjects.Add(gO);
			funds -= placeableSO.price;
		}
	}

	[Command]
	public void CmdEdit(uint id, Vector3 newPosition, Quaternion newRotation) { // TODO: refactor
		if (NetworkServer.spawned.TryGetValue(id, out NetworkIdentity identity)) {
			if (propertyArea == null || !identity.TryGetComponent(out BSystem.PlaceableObject placeableObject) || placeableObject.ownerId != this.id ||
				!propertyArea.Contains(new Bounds(newPosition, Vector3.one * .1f))) {
				return;
			}
			identity.transform.position = newPosition;
			identity.transform.rotation = newRotation;
			RpcEditUpdate(id, newPosition, newRotation);
		}
	}

	[ClientRpc]
	public void RpcEditUpdate(uint id, Vector3 newPosition, Quaternion newRotation) {
		if (NetworkClient.spawned.TryGetValue(id, out NetworkIdentity identity)) {
			identity.transform.position = newPosition;
			identity.transform.rotation = newRotation;
		}
	}

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

    public override void OnStartLocalPlayer() {
		base.OnStartLocalPlayer(); // ?

		localPlayer = this;

		MasterServer.MSClient.State = MasterServer.MSClient.NetworkState.InGame;
		TPController.TPCameraController.LockCursor(true);
        //_camera = FindObjectOfType<Camera>().transform;
        _camera = GameObject.Find("MainCamera").transform;

        BSystem.BSystem.OnPlaceRequestAction = () => {
			CmdPlace(BSystem.BSystem.currentPlaceableSO.uniqueName, BSystem.BSystem.position, BSystem.BSystem.rotation);
		};
	}

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

#if UNITY_SERVER// || UNITY_EDITOR // (Server)
		int propertyAreaId = PropertyArea.Assign(instance.uniqueName, id);
		TargetAreaId(propertyAreaId);
		propertyArea = PropertyArea.GetPropertyArea(instance.uniqueName, id); // ?????
#endif

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

	[TargetRpc]
	private void TargetAreaId(int id) {
		Debug.Log(id);
		PropertyArea.myIndex = id;
	}

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
			if (placedObjects.Count > 0) {
				MasterServer.MServer.PlacedObjectJSONData[] placedObjectsData = new MasterServer.MServer.PlacedObjectJSONData[placedObjects.Count];
				for (int i = 0; i < placedObjects.Count; i++) {
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
					NetworkServer.Destroy(placedObjects[i].gameObject);
				}
				MasterServer.MSClient.SavePlacedObjects(id, placedObjectsData);
			}
		}

		if (isServer) {
			MasterServer.MSManager.SendPacket(new MasterServer.AccountDataResponsePacket { Id = id, Funds = funds, OwnsProperty = true, Nutrition = playerNutrition.value, ExperiencePoints = experiencePoints });

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
