using UnityEngine;

namespace RedicionStudio.InventorySystem {

	[CreateAssetMenu(fileName = "New Ammo Item SO", menuName = "Inventory System/ItemSOs/Ammo")]
	public class AmmoItemSO : ItemSO {

		[Header("Ammo")]
		public float damage;
	}
}
