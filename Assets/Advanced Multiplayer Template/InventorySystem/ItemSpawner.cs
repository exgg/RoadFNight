using UnityEngine;
using Mirror;
using RedicionStudio.InventorySystem;

public class ItemSpawner : MonoBehaviour
{

	[SerializeField] private ItemSO itemSO;
	[SerializeField] private int amount = 1;

	private GameObject _spawned;

	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}

		GameObject gO = Instantiate(ConfigurationSO.Instance.itemDropPrefab, transform.position, transform.rotation);
		ItemDrop itemDrop = gO.GetComponent<ItemDrop>();
		itemDrop.item = new RedicionStudio.InventorySystem.Item(itemSO.uniqueName, itemSO is ConsumableItemSO consumableItemSO ? consumableItemSO.shelfLifeInSeconds : 0f);
		itemDrop.amount = amount;
		NetworkServer.Spawn(gO);
		_spawned = gO;
	}

	private void OnDestroy()
	{
		if (_spawned != null)
		{
			NetworkServer.Destroy(_spawned);
		}
	}

	private void OnValidate()
	{
		if (amount < 1)
		{
			amount = 1;
		}

		if (itemSO is UseableItemSO useableItemSO && amount > 1)
		{
			amount = 1;
		}
	}
}
