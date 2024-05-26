using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.InventorySystem;
using RedicionStudio.NetworkUtils;

public class PlayerInteractionModule : NetworkBehaviour {

	[Header("Player Modules")]
	public PlayerInventoryModule playerInventory;

	[HideInInspector] public INetInteractable<PlayerInventoryModule> currentInteractable;

	[SerializeField] private float _maxDistance;

    [Space]
    [SerializeField] private GameObject UIMessagePrefab;
    GameObject instantiatedUIMessage;

    private static Transform _camera;

    private void Start() {
		if (!isLocalPlayer) {
			return;
		}

        //_camera = FindObjectOfType<Camera>().transform;
        _camera = GameObject.Find("MainCamera").transform;
        UIInteraction.playerInteraction = this;
	}

	private void OnDestroy() {
		if (isLocalPlayer) {
			UIInteraction.playerInteraction = null;
		}
	}

	private void Raycast(Vector3 position, Vector3 forward) {
		if (Physics.Raycast(position, forward, out RaycastHit hitInfo, _maxDistance, 1 << LayerMask.NameToLayer("Ground")) && hitInfo.transform.TryGetComponent(out INetInteractable<PlayerInventoryModule> interactable)) {
			currentInteractable = interactable;
		}
		else {
			currentInteractable = null;
		}
	}

	private static Keyboard _keyboard;

	[Command]
	public void CmdInteract(Vector3 position, Vector3 forward) {
		Raycast(position, forward);
		if (currentInteractable != null) {
			currentInteractable.OnServerInteract(playerInventory);
		}
	}

    public void AddItem(PlayerInventoryModule player, int itemPrice, Item item, int amount)
    {
        if (instantiatedUIMessage != null)
            Destroy(instantiatedUIMessage);

        instantiatedUIMessage = Instantiate(UIMessagePrefab);

        if (player.GetComponent<Player>().funds < itemPrice)
        {
            instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Not enough funds");

            return;
        }

        instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Item: " + item.itemSO.uniqueName + " " + amount + "x"  + " purchased");
        CmdAddItem(player, itemPrice, item, amount);
    }

    [Command]
    public void CmdAddItem(PlayerInventoryModule player, int itemPrice, Item item, int amount)
    {
        if (player.GetComponent<Player>().funds < itemPrice)
        {
            //Not enough funds
        }
        else if (player.GetComponent<Player>().funds == itemPrice || player.GetComponent<Player>().funds > itemPrice)
        {
            player.GetComponent<Player>().funds -= itemPrice;
            player.Add(item, amount);
        }
    }

    public void RemoveItem(PlayerInventoryModule player, int sellPrice, Item item, int amount, int itemSlotIndex)
    {
        if (instantiatedUIMessage != null)
            Destroy(instantiatedUIMessage);

        instantiatedUIMessage = Instantiate(UIMessagePrefab);

        instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Item: " + item.itemSO.uniqueName + " " + amount + "x" + " sold" + " for" + "$" + sellPrice);
        player.CmdDropAndRemoveItem(itemSlotIndex, true);
        CmdRemoveItem(player, sellPrice, item, amount);
    }

    [Command]
    public void CmdRemoveItem(PlayerInventoryModule player, int sellPrice, Item item, int amount)
    {
        player.GetComponent<Player>().funds += sellPrice;
        //player.Remove(item, amount);
    }

    public void AddMoney(PlayerInventoryModule player, int amount)
    {
        if (isServer)
        {
            player.GetComponent<Player>().funds += amount;
            RpcAddMoney(player, amount);
        }
        else if (hasAuthority)
        {
            if (instantiatedUIMessage != null)
                Destroy(instantiatedUIMessage);

            instantiatedUIMessage = Instantiate(UIMessagePrefab);

            instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Amount: " + "$" + amount + " added");
            CmdAddMoney(player, amount);
        }
    }

    [Command]
    public void CmdAddMoney(PlayerInventoryModule player, int amount)
    {
        player.GetComponent<Player>().funds += amount;
    }

    [ClientRpc]
    public void RpcAddMoney(PlayerInventoryModule player, int amount)
    {
        player.GetComponent<Player>().funds += amount;
    }

    public void RemoveMoney(PlayerInventoryModule player, int amount)
    {
        if (instantiatedUIMessage != null)
            Destroy(instantiatedUIMessage);

        instantiatedUIMessage = Instantiate(UIMessagePrefab);

        if (player.GetComponent<Player>().funds < amount)
        {
            instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Not enough funds");

            return;
        }

        instantiatedUIMessage.GetComponent<UIMessage>().ShowMessage("Amount: " + "$" + amount + " removed");
        CmdRemoveMoney(player, amount);
    }

    [Command]
    public void CmdRemoveMoney(PlayerInventoryModule player, int amount)
    {
        if (player.GetComponent<Player>().funds < amount)
        {
            // Amount cannot be removed because the player does not have sufficient funds.
        }
        else if (player.GetComponent<Player>().funds == amount || player.GetComponent<Player>().funds > amount)
        {
            player.GetComponent<Player>().funds -= amount;
        }
    }

    private static Vector3 _position;
	private static Vector3 _forward;

	private void Update() {
		if (!isLocalPlayer) {
			return;
		}

		_position = _camera.position;
		_forward = _camera.forward;

		Raycast(_position, _forward);

		_keyboard = Keyboard.current;

		if (currentInteractable == null || _keyboard == null) {
			return;
		}

		if (_keyboard.fKey.wasPressedThisFrame) {
			currentInteractable.OnClientInteract(playerInventory);
			CmdInteract(_position, _forward);
		}
    }
}
