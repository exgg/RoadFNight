using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Mirror;
using TMPro;
using System;
public class PlacementModule : NetworkBehaviour
{
	private NetPlayer _netPlayer;
	
	[HideInInspector] public Instance instance;
	
	
	public List<GameObject> placedObjects = new List<GameObject>();

	public static PlacementModule LocalPlayerB;
	private void Start()
	{
		
		_netPlayer = GetComponent<NetPlayer>();
		LoadPlacement();
	}

	public override void OnStartLocalPlayer()
	{
		LocalPlayerB = this;
		// Sets up the action to be performed when a place request is made in the game.
		// The CmdPlace command is called with the current placeable object's details.
		BSystem.BSystem.OnPlaceRequestAction = () => {
			CmdPlace(BSystem.BSystem.currentPlaceableSO.uniqueName, BSystem.BSystem.position, BSystem.BSystem.rotation);
		};
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
		
		if (_netPlayer.funds >= placeableSO.price) {
			if (_netPlayer.propertyArea == null || !_netPlayer.propertyArea.Contains(new Bounds(position, Vector3.one * .1f))) {
				return;
			}
			// place object and take funds
			GameObject gO = BSystem.PlaceableObject.Place(_netPlayer.id, placeableSOUniqueName, position, rotation); // ?
			placedObjects.Add(gO);
			_netPlayer.funds -= placeableSO.price;
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
			if (_netPlayer.propertyArea == null || !identity.TryGetComponent(out BSystem.PlaceableObject placeableObject) || placeableObject.ownerId != _netPlayer.id ||
			    !_netPlayer.propertyArea.Contains(new Bounds(newPosition, Vector3.one * .1f))) {
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
			if (_netPlayer.propertyArea == null || !identity.TryGetComponent(out BSystem.PlaceableObject placeableObject) || placeableObject.ownerId != _netPlayer.id) {
				return;
			}

			placedObjects.Remove(placeableObject.gameObject);
			NetworkServer.Destroy(identity.gameObject);
			_netPlayer.funds += placeableObject.placeableSO.sellPrice;
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
	/// Retrieves and places objects that the player has placed in the game world.
	/// This function runs only on the server or within the Unity Editor for testing purposes. otherwise will never call
	/// It gets the placed objects data from the MasterServer and instantiates them in the game world
	/// at the correct positions and orientations, then adds them to the player's placed objects list.
	/// </summary>
	private void LoadPlacement()
	{
#if UNITY_SERVER// || UNITY_EDITOR // (Server)
		int propertyAreaId = PropertyArea.Assign(instance.uniqueName, _player.id);
		TargetAreaId(propertyAreaId);
		_player.propertyArea = PropertyArea.GetPropertyArea(instance.uniqueName, _player.id); // ?????
#endif

	   
#if UNITY_SERVER || UNITY_EDITOR
	    
		if (isServer) {
			MasterServer.MSClient.GetPlacedObjects(_netPlayer.id, (placedObjectsData) => {
				for (int i = 0; i < placedObjectsData.Length; i++) {
					Vector3 position = _netPlayer.propertyArea.transform.position + new Vector3(
						placedObjectsData[i].x,
						placedObjectsData[i].y,
						placedObjectsData[i].z);
					GameObject gO = BSystem.PlaceableObject.Place(_netPlayer.id, placedObjectsData[i].placeableSOUniqueName, position,
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
	/// Toggles off the players controller and removes them from the server and the game
	/// </summary>
	private void OnDestroy() {
		_ = _netPlayer.onlinePlayers.Remove(_netPlayer.id);

		if (NetPlayer.LocalNetPlayer == _netPlayer) {
			NetPlayer.LocalNetPlayer = null;
		}

		if (isServer) {
			instance.RemovePlayer(_netPlayer.id);
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
					Vector3 position = placedObjects[i].transform.position - _netPlayer.propertyArea.transform.position;
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
				MasterServer.MSClient.SavePlacedObjects(_netPlayer.id, placedObjectsData);
			}
		}

		if (isServer) {
			// send account packet to the master server
			MasterServer.MSManager.SendPacket(new MasterServer.AccountDataResponsePacket { Id = _netPlayer.id, Funds = _netPlayer.funds, OwnsProperty = true, Nutrition = _netPlayer.playerNutrition.value, ExperiencePoints = _netPlayer.experiencePoints });
			
			// Prepare and save inventory data to master server
			MasterServer.MServer.InventoryJSONData[] inventoryJSONData = new MasterServer.MServer.InventoryJSONData[_netPlayer.playerInventory.slots.Count];
			for (int i = 0; i < _netPlayer.playerInventory.slots.Count; i++) {
				inventoryJSONData[i] = new MasterServer.MServer.InventoryJSONData {
					hash = _netPlayer.playerInventory.slots[i].item.hash,
					amount = _netPlayer.playerInventory.slots[i].amount,
					shelfLife = _netPlayer.playerInventory.slots[i].item.currentShelfLifeInSeconds
				};
			}
			MasterServer.MSClient.SaveInventory(_netPlayer.id, inventoryJSONData);
		}
#endif
		// Refactor

#if UNITY_SERVER// || UNITY_EDITOR // (Server)
		_player.propertyArea?.AssignTo(0);
		_player.propertyArea = null;
#endif
	}



}
