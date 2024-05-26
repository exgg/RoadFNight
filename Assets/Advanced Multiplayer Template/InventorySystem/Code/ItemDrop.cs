using UnityEngine;
using Mirror;
using RedicionStudio.NetworkUtils;

namespace RedicionStudio.InventorySystem {

	[RequireComponent(typeof(Collider))]
	public class ItemDrop : NetworkBehaviour, INetInteractable<PlayerInventoryModule> {

		[SyncVar] public Item item;
		[SyncVar] public int amount;
        [HideInInspector] public bool remove = false;

		private void Start() {
            if(!remove)
			    Instantiate(item.itemSO.modelPrefab).transform.SetParent(transform, false);
            else
                NetworkServer.Destroy(gameObject);
        }

		// (Server)
		public void OnServerInteract(PlayerInventoryModule player) {
			if (amount < 1) {
				NetworkServer.Destroy(gameObject);
			}

			if (player.Add(item, amount)) {
				amount = 0; // ?
				NetworkServer.Destroy(gameObject);
			}
		}

		// (Client)
		public void OnClientInteract(PlayerInventoryModule player) { }

		// (Client)
		public string GetInfoText() {
			if (amount > 0 && item.itemSO != null) { // ?
				return amount > 1 ? item.itemSO.uniqueName + " (" + amount + ')' : item.itemSO.uniqueName; // ?
			}
			return "???";
		}
	}
}
