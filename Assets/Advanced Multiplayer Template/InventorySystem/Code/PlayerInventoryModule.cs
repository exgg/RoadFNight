using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.UIUtils;
using StarterAssets;
using UnityEngine.Serialization;

namespace RedicionStudio.InventorySystem
{

	public class SyncDictionaryIntDouble : SyncDictionary<int, double> { }

	public class PlayerInventoryModule : Inventory
	{

		#region Calls

		// input components
		private StarterAssets.StarterAssetsInputs _inputs;

		// player stat components
		private Health _health;

		// weapon wheel components
		[HideInInspector]
		public WeaponWheelManager weaponWheelManager;

		// shop components
		[HideInInspector]
		public ShopManager shopManager;

		// emote wheel components
		[HideInInspector]
		public EmoteWheel _emoteWheel;

		// chat system components
		[HideInInspector]
		public ChatSystem _chatSystem;

		#endregion

		[FormerlySerializedAs("player")] [Header("Player Modules")]
		public NetPlayer netPlayer;
		public PlayerNutritionModule playerNutrition;

		[Space]
		public AudioSource audioSource;

		[Space]
		public ManageTPController TPControllerManager;

		[Space]
		public GameObject bulletPrefab;
		public GameObject rocketPrefab;
		public float bulletSpeed;
		Transform _bulletSpawnPointPosition;

		[Space]
		public GameObject cartridgeEjectPrefab;
		Transform _cartridgeEjectSpawnPointPosition;

		[Space]
		public bool inPropertyArea = false;

		[Space]
		public bool inShop = false;

		[Space]
		public bool inCar = false;

		[Space]
		public bool usesParachute = false;

		[Space]
		public bool isAiming = false;


		public ChatSystem chatWindow;

		public static Keyboard _keyboard;
		private static Mouse _mouse;

		public static bool inMenu;

		private int _index;
		private double _interval = 60f;
		private double _lastTime;
		public ItemSlot _slot;

		[Space]
		[SerializeField] private Transform _gFX;



		private void Start()
		{
			if (isLocalPlayer)
			{
				UIPlayerInventory.playerInventory = this;
				slots.Callback += Slots_Callback;
				UIPlayerInventory.InstanceRefresh();

				Initialisation();

				UIDragAndDrop.OnDragAndClearAction = CmdDropItem;
				UIDragAndDrop.OnDragAndDropAction = (from, to) =>
				{
					if (slots[from].amount > 0 && slots[to].amount > 0 &&
						slots[from].item.Equals(slots[to].item))
					{
						CmdInventoryMerge(from, to);
					}
					else if (_keyboard != null && _keyboard.shiftKey.isPressed)
					{
						CmdInventorySplit(from, to);
					}
					else
					{
						CmdSwapInventoryInventory(from, to);
					}
				};
			}

		}

		/// <summary>
		/// Setup all required class calls
		/// </summary>
		private void Initialisation()
		{
			//Inputs
			_inputs = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssets.StarterAssetsInputs>(); // errr what ? THEE most expensive call in unity... 
			_keyboard = Keyboard.current;
			_mouse = Mouse.current;

			//Classes
			_health = GetComponent<Health>();

			weaponWheelManager = GetComponent<WeaponWheelManager>();
			weaponWheelManager.Initialisation();

			shopManager = GetComponent<ShopManager>();

			// emote system
			_emoteWheel = GetComponent<EmoteWheel>();

			// Chat System
			chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
			_chatSystem = chatWindow.GetComponent<ChatSystem>();
		}


		/// <summary>
		/// Update is a mess, lots of logic in here can be both improved and split off into other methods to control readability
		/// and allow for more dynamic control over it in the future. Split this up in phase 2 or 3
		/// </summary>
		private void Update()
		{
			if (!isLocalPlayer || _keyboard == null || _mouse == null) // if is not the current player of this player game object or inputs not set return
				return;

			ShelfLifeCalculation();

			if (inShop) return;
			shopManager.ShopUIToggle();
			weaponWheelManager.WeaponWheelUIToggle(slots[0]);
			EmoteWheelUIToggle();
			AimWeapon();
		}

		/// <summary>
		/// Again more expensive code, and things that can be split into separate methods to avoid arrowheads
		/// </summary>
		private void LateUpdate()
		{
			if (chatWindow == null)
				chatWindow = chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
			if (isServer)
			{
				return;
			}

			for (int i = 0; i < _gFX.childCount; i++)
			{
				_gFX.GetChild(i).gameObject.SetActive(false);
				_gFX.GetChild(i).GetComponent<WeaponManager>().enabled = false;
			}
			if (!this.GetComponent<Health>().isDeath && !inCar && !usesParachute && !this.GetComponent<EmoteWheel>().isPlayingAnimation && slots[0].amount > 0 && slots[0].item.itemSO != null)
			{
				this.GetComponent<Animator>().SetLayerWeight(1, 1);
				for (int i = 0; i < _gFX.childCount; i++)
				{
					if (_gFX.GetChild(i).name == slots[0].item.itemSO.uniqueName)
					{
						_gFX.GetChild(i).gameObject.SetActive(true);
						_gFX.GetChild(i).GetComponent<WeaponManager>().enabled = true;
						this.GetComponent<ManageTPController>().CurrentWeaponManager = _gFX.GetChild(i).GetComponent<WeaponManager>();
						this.GetComponent<ManageTPController>().CurrentWeaponBulletSpawnPoint = _gFX.GetChild(i).GetComponent<WeaponManager>().CurrentWeaponBulletSpawnPoint;
						this.GetComponent<ManageTPController>().CurrentCartridgeEjectSpawnPoint = _gFX.GetChild(i).GetComponent<WeaponManager>().CartridgeEjectEffectSpawnPoint;
					}
				}
			}
			else
			{
				this.GetComponent<Animator>().SetLayerWeight(1, 0);
				this.GetComponent<ManageTPController>().PlayerRig.weight = 0;
			}
		}


		/// <summary>
		/// Controls the deterioration for shelf life calculation on items that have one 
		/// </summary>
		public void ShelfLifeCalculation()
		{
			if (!isServer || !(NetworkTime.time >= _lastTime + _interval)) return; // if not server or cooldown still going return

			for (_index = 0; _index < slots.Count; _index++)
			{
				_slot = slots[_index];
				if (_slot.amount <= 0 || _slot.item.itemSO == null ||
					_slot.item.itemSO is not ConsumableItemSO) continue;
				if (_slot.item.currentShelfLifeInSeconds > 0f)
				{
					_slot.item.currentShelfLifeInSeconds -= (float)_interval;
				}
				else
				{
					_slot.item = new Item();
					_slot.amount = 0;
				}
				slots[_index] = _slot;
			}
			_lastTime = NetworkTime.time;
		}

		// #region Shop UI

		// /// <summary>
		// /// Activation toggle for the UI of the ShopUI, can be done in event based instead of this but that can happen in phase 3
		// /// </summary>
		// public void ShopUIToggle()
		// {
		// 	// PSEUDO
		// 	// allow for expanding to event based later PHASE 3

		// 	if (!_keyboard.tabKey.wasPressedThisFrame) return;

		// 	inMenu = !inMenu;

		// 	switch (inMenu)
		// 	{
		// 		case true:
		// 			EnterShopMenu();
		// 			break;
		// 		case false:
		// 			ExitShopMenu();
		// 			break;

		// 	}
		// }

		// /// <summary>
		// /// Toggle on the Shop Menu, checking if BSsystem has in menu to toggle another instance
		// /// </summary>
		// private void EnterShopMenu()
		// {
		// 	if (BSystem.BSystem.inMenu)
		// 	{
		// 		BSystem.BSystem.inMenu = false;
		// 		BSystemUI.Instance.SetActive(false);

		// 	}

		// 	UIPlayerInventory.SetActive(true);
		// 	UIPlayerInventory.InventoryUI.SetActive(true);
		// 	TPController.TPCameraController.LockCursor(false);
		// }

		// /// <summary>
		// /// Toggle off the shop menu
		// /// </summary>
		// private void ExitShopMenu()
		// {
		// 	UIPlayerInventory.SetActive(false);
		// 	UIPlayerInventory.InventoryUI.SetActive(false);
		// 	TPController.TPCameraController.LockCursor(true);
		// }

		// #endregion

		/// <summary>
		/// Toggles the emote wheel. This looks a mess. And must have a massive amount of other ways to do this
		/// </summary>
		public void EmoteWheelUIToggle()
		{
			if (!BSystem.BSystem.inMenu && !WeaponWheelManager.inWeaponWheel && !GetComponent<EmoteWheel>().inEmoteWheel &&
				!inPropertyArea && !inShop && !inCar && !usesParachute && !this.GetComponent<EmoteWheel>().isPlayingAnimation &&
				isAiming && !this.GetComponent<Health>().isDeath && _inputs.shoot && _slot.amount > 0 && _slot.item.itemSO != null && _slot.item.itemSO is WeaponItemSO weaponItemSO)
			{
				_interval = weaponItemSO.cooldownInSeconds;
				if (NetworkTime.time >= _lastTime + _interval)
				{
					if (weaponItemSO.automatic)
					{
						CmdUseItem(0);
					}
					else if (_mouse.leftButton.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame)
					{
						CmdUseItem(0);
					}
					_lastTime = NetworkTime.time;
				}
			}
		}

		public void AimWeapon()
		{
			//Aim
			if (!BSystem.BSystem.inMenu & !inPropertyArea & !inShop & !inCar & !usesParachute & !_health.isDeath &
				!this.GetComponent<EmoteWheel>().isPlayingAnimation & _inputs.aim & _slot.amount > 0 & _slot.item.itemSO != null & _slot.item.itemSO is WeaponItemSO)
			{
				CmdAim();
			}
		}

		/// <summary>
		/// Initializes the inventory system slots, once those are initialized it will then look up if the player has
		/// anything in their inventory via the server saved data. After that has been completed it will then place that
		/// item into the slot that is required. Saving both the position in the inventory and the type of item it is.
		/// To then be loaded through here.
		/// </summary>
		public void LoadInventory()
		{
			for (int i = 0; i < 67; i++)
			{
				slots.Add(new ItemSlot());
			}

#if UNITY_SERVER || UNITY_EDITOR // ?
			MasterServer.MSClient.GetInventory(netPlayer.id, (inventoryData) => {
				if (inventoryData == null || inventoryData.Length < 1) { // if inventory data not set or nothing in there return 
					return;
				}
				ItemSlot slot;
				for (int i = 0; i < inventoryData.Length; i++) {
					slot = new ItemSlot();
					Debug.Log(inventoryData[i].hash);
					slot.item.hash = inventoryData[i].hash;
					slot.amount = inventoryData[i].amount;
					slot.item.currentShelfLifeInSeconds = inventoryData[i].shelfLife;
					slots[i] = slot;
				}
			});
#endif
		}

		/// <summary>
		/// Merges item stacks in the player's inventory.
		/// This method allows the player to move partial stacks of items from one slot to another.
		/// If the items in both slots are the same, it combines the stacks up to the maximum stack size,
		/// adjusting the amounts in each slot accordingly.
		/// </summary>
		/// <param name="from">The index of the slot to move items from</param>
		/// <param name="to">The index of the slot to move items to</param>
		[Command]
		private void CmdInventoryMerge(int from, int to)
		{
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to)
			{
				ItemSlot fromSlot = slots[from];
				ItemSlot toSlot = slots[to];
				if (fromSlot.amount > 0 && toSlot.amount > 0)
				{
					if (fromSlot.item.Equals(toSlot.item))
					{
						int put = toSlot.IncreaseBy(fromSlot.amount);
						fromSlot.DecreaseBy(put);
						slots[from] = fromSlot;
						slots[to] = toSlot;
					}
				}
			}
		}

		/// <summary>
		/// Handles the ability to split items into two separate stacks. This will only be possible if the
		/// stack size is greater than 2. Since it is an integer division no floating point formula is required
		/// </summary>
		/// <param name="from">The index of the slot to move items from</param>
		/// <param name="to">The index of the slot to move items to</param>
		[Command]
		private void CmdInventorySplit(int from, int to)
		{
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to)
			{
				ItemSlot fromSlot = slots[from];
				ItemSlot toSlot = slots[to];
				if (fromSlot.amount >= 2 && toSlot.amount == 0)
				{
					toSlot = fromSlot;

					toSlot.amount = fromSlot.amount / 2;
					fromSlot.amount -= toSlot.amount;

					slots[from] = fromSlot;
					slots[to] = toSlot;
				}
			}
		}

		/// <summary>
		/// Switches item from one inventory space to another 
		/// </summary>
		/// <param name="from">The point it was</param>
		/// <param name="to">Empty slot or taken slot if different item (not stackable) </param>
		[Command]
		private void CmdSwapInventoryInventory(int from, int to)
		{
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to)
			{
				ItemSlot fromSlot = slots[from];

				// Ensure the item is compatible with the targeted slot
				if ((to == 0 && !(fromSlot.item.itemSO is WeaponItemSO)) ||
					(to == 1 && !(fromSlot.item.itemSO is AmmoItemSO)) ||
					(to == 2 && !(fromSlot.item.itemSO is OutfitItemSO)) ||
					(to == 3 && !(fromSlot.item.itemSO is CompanionItemSO)))
				{
					return;
				}

				//swap the items between the two slots
				slots[from] = slots[to];
				slots[to] = fromSlot;
			}
		}


		/// <summary>
		/// Callback method triggered when the SyncList of ItemSlots is modified.
		/// Refreshes the UI inventory to reflect changes
		/// </summary>
		/// <param name="op">The operation performed on the list (Add, Remove, etc.)</param>
		/// <param name="itemIndex">The index of the item that was changed</param>
		/// <param name="oldItem">The old item before the change</param>
		/// <param name="newItem">The new item after the change</param>
		public static void Slots_Callback(SyncList<ItemSlot>.Operation op, int itemIndex, ItemSlot oldItem, ItemSlot newItem)
		{
			UIPlayerInventory.InstanceRefresh();
		}

		/// <summary>
		/// (Server)
		/// </summary>
		private void DropItem(Item item, int amount, bool remove)
		{
			Vector2 randomPoint = Random.insideUnitCircle * 2f;
			Vector3 position = new Vector3(transform.position.x + randomPoint.x, transform.position.y, transform.position.z + randomPoint.y);

			GameObject gO = Instantiate(ConfigurationSO.Instance.itemDropPrefab, position, Quaternion.identity);
			ItemDrop itemDrop = gO.GetComponent<ItemDrop>();
			itemDrop.remove = remove;
			itemDrop.item = item;
			itemDrop.amount = amount;
			NetworkServer.Spawn(gO);
		}

		/// <summary>
		/// (Server)
		/// Drops an Item from the specific slot and clears the slot
		/// this method is executed on the server. Removing it from the master server player data slot.
		/// calls the drop item method and updates the slot to be emptied
		/// </summary>
		private void DropItemAndClearSlot(int slotIndex, bool remove)
		{
			ItemSlot slot = slots[slotIndex];
			DropItem(slot.item, slot.amount, remove);
			slot.amount = 0;
			slots[slotIndex] = slot;
		}


		/*
		 * Command to drop an item from the inventory.
		 * This method is executed on the server. It ensures that the item to be dropped is not equipped
		 * (i.e., the index is greater than 3), checks that the slot index is valid, and the slot contains an item.
		 * If these conditions are met, it calls DropItemAndClearSlot to remove the item from the slot and drop it in the game world.
		 */

		[Command]
		public void CmdDropItem(int index)
		{
			if (index > 3) // Ensures that no item can be dropped as long as it is equipped.
			{
				if (0 <= index && index < slots.Count && slots[index].amount > 0)
				{
					DropItemAndClearSlot(index, false);
				}
			}
		}

		/*
		 * Command to drop and optionally remove item from inventory
		 * this is a server executable. Ensuring that the item to be dropped is not equipped
		 * once conditions are met then calls drop item and clear slots
		 * the parameter determines if it should be removed from the server completely
		 */
		[Command]
		public void CmdDropAndRemoveItem(int index, bool remove)
		{
			if (index > 3) // Ensures that no item can be dropped as long as it is equipped.
			{
				if (0 <= index && index < slots.Count && slots[index].amount > 0)
				{
					DropItemAndClearSlot(index, remove);
				}
			}
		}

		#region Cooldowns

		private Dictionary<int, double> _local_itemCooldowns = new Dictionary<int, double>();
		private readonly SyncDictionaryIntDouble _itemCooldowns = new SyncDictionaryIntDouble();

		/// <summary>
		/// Sets the cooldown for an item. The cooldown is tracked separately for the client and server.
		/// If executed on the client, the cooldown is stored in the local item cooldowns dictionary.
		/// If executed on the server, the cooldown is stored in the server item cooldowns dictionary.
		/// </summary>
		/// <param name="itemSOHash">ID of item for cooldown</param>
		/// <param name="cooldownInSeconds">Time of cooldown</param>
		public void SetCooldown(int itemSOHash, float cooldownInSeconds)
		{
			double cooldownEndTime = NetworkTime.time + cooldownInSeconds;

			if (isClient && !isServer)
			{
				_local_itemCooldowns[itemSOHash] = cooldownEndTime;
			}
			else
			{
				_itemCooldowns[itemSOHash] = cooldownEndTime;
			}
		}

		/// <summary>
		/// Retrieves the remaining cooldown time for a specific item.
		/// This method checks both the local client and the server for the cooldown end time of the item
		/// identified by its unique hash. If the current time exceeds the cooldown end time, it returns 0,
		/// indicating no cooldown. Otherwise, it returns the remaining cooldown time in seconds.
		/// </summary>
		/// <param name="itemSOHash">ID of item</param>
		/// <returns>returns the remaining cooldown, if there is non returns 0</returns>
		public float GetCooldown(int itemSOHash)
		{
			double cooldownEndTime;

			if (isClient && !isServer)
			{
				if (_local_itemCooldowns.TryGetValue(itemSOHash, out cooldownEndTime))
				{
					return NetworkTime.time >= cooldownEndTime ? 0f : (float)(cooldownEndTime - NetworkTime.time);
				}
			}

			if (_itemCooldowns.TryGetValue(itemSOHash, out cooldownEndTime))
			{
				return NetworkTime.time >= cooldownEndTime ? 0f : (float)(cooldownEndTime - NetworkTime.time);
			}

			return 0f;
		}

		#endregion

		/// <summary>
		/// Checks if the item attempted to be used is usable,
		/// if it is used it will call OnUsed abstract and reduce the stack
		/// </summary>
		/// <param name="item"></param>
		[ClientRpc]
		public void RpcOnItemUsed(Item item)
		{
			if (item.itemSO is UseableItemSO usableItemSO)
			{
				usableItemSO.OnUsed(this);
			}
		}

		/// <summary>
		/// Parses the index of the item used. This will then check if it can be used and will reduce the stack and
		/// call use item starting the cooldown. Pushing this to the server
		/// </summary>
		/// <param name="slotIndex"></param>
		[Command]
		public void CmdUseItem(int slotIndex)
		{
			if (0 <= slotIndex && slotIndex < slots.Count && slots[slotIndex].amount > 0 && slots[slotIndex].item.itemSO is UseableItemSO usableItemSO && usableItemSO.CanBeUsed(this, slotIndex))
			{
				usableItemSO.Use(this, slotIndex);
			}
		}

		/// <summary>
		/// This will push to the server that the player is aiming to the TP controller setting the aim value to 1
		/// this likely is working in parallel to setting the animator values for the server?
		/// </summary>
		[Command]
		public void CmdAim()
		{
			TPControllerManager.aimValue = 1;
		}

		/// <summary>
		/// Spawns a bullet at the location of the current weapon spawn then ejects bullet towards a location using the screen point to ray
		/// method from unity
		/// Uses an if Statement for that of rockets or bullets
		///
		///
		/// These need changing need to look for swap weapon, then call the change of current weapon bullet spawn to be only called on weapon change and awake (if has a weapon)
		/// or maybe add a debounce to have a more simple approach.
		/// Switch statement to replace the if statement so we can include different bullet types such as 9mm etc.
		/// </summary>
		public void ShootBullet()
		{
			if (!base.hasAuthority) return;

			_bulletSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentWeaponBulletSpawnPoint;
			_cartridgeEjectSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentCartridgeEjectSpawnPoint;
			string currentBulletName = this.GetComponent<ManageTPController>().CurrentWeaponManager.WeaponBulletPrefab.name;
			//bulletSpeed = this.GetComponent<ManageTPController>().CurrentWeaponManager.BulletSpeed;
			this.GetComponent<ManageTPController>().Shoot();
			//CmdSetAttackerUsername(username);

			Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

			int layerToIgnore = 4; // Replace with the layer you want to ignore
			LayerMask layerMask = ~(1 << layerToIgnore);
			RaycastHit hit;
			Vector3 collisionPoint;
			if (Physics.Raycast(ray, out hit, 50f, layerMask))
			{
				collisionPoint = hit.point;
			}
			else
			{
				collisionPoint = ray.GetPoint(50f);
			}

			Vector3 bulletVector = (collisionPoint - _bulletSpawnPointPosition.transform.position).normalized;




			if (currentBulletName == "Bullet")
				CmdShootBullet(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, _cartridgeEjectSpawnPointPosition.position, _cartridgeEjectSpawnPointPosition.rotation, bulletVector, bulletSpeed);
			else
				CmdShootRocket(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, bulletVector, bulletSpeed);
		}

		/// <summary>
		/// Server command to shoot the bullet after the shoot bullet client side has been called, calculating which type of projectile will be fired.
		/// Then it will use the position of the pawn rotation, position of the cartridge ejection, spawn point position of the cartridge, bullet direction of shooting and its speed.
		/// This will then move to a remote procedure to fire this event from the client to the network. 
		/// </summary>
		/// <param name="_position"></param>
		/// <param name="_rotation"></param>
		/// <param name="_cartridgeEjectPosition"></param>
		/// <param name="_cartridgeEjectRotation"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[Command]
		void CmdShootBullet(Vector3 _position, Quaternion _rotation, Vector3 _cartridgeEjectPosition, Quaternion _cartridgeEjectRotation, Vector3 _bulletVector, float _bulletSpeed)
		{
			GameObject Bullet = Instantiate(bulletPrefab, _position, _rotation) as GameObject;

			Bullet.GetComponent<Rigidbody>().velocity = _bulletVector * _bulletSpeed;

			//Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

			NetworkServer.Spawn(Bullet);

			NetworkBullet bullet = Bullet.GetComponent<NetworkBullet>();
			bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

			bullet.SetupProjectile_ServerSide();

			RpcBulletFired(bullet, _bulletVector, _bulletSpeed);


			GameObject _cartridgeEject = Instantiate(cartridgeEjectPrefab, _cartridgeEjectPosition, _cartridgeEjectRotation) as GameObject;



			NetworkServer.Spawn(_cartridgeEject, connectionToClient);
		}

		/// <summary>
		/// Sets up the projectile for who fired it, and then also set the autority to the player who fired it,
		/// this seems to be to prevent the player from getting hit by their own bullets.
		/// </summary>
		/// <param name="Bullet"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[ClientRpc]
		void RpcBulletFired(NetworkBullet Bullet, Vector3 _bulletVector, float _bulletSpeed)
		{
			Bullet.GetComponent<NetworkBullet>().SetupProjectile(currentPlayerUsername(), hasAuthority);

			//Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
		}

		/// <summary>
		/// Commands the server to instantiate a rocket prefab at the position of the weapon spawn. this is passed from the shoot bullet method
		/// which allows us to carry over the positions of the spawn, its rotation, the direction it is going and the speed.
		///
		/// Naming convention on this and a few others is pretty bad so that needs editing
		/// </summary>
		/// <param name="_position"></param>
		/// <param name="_rotation"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[Command]
		void CmdShootRocket(Vector3 _position, Quaternion _rotation, Vector3 _bulletVector, float _bulletSpeed)
		{
			GameObject Bullet = Instantiate(rocketPrefab, _position, _rotation) as GameObject;

			//Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);

			//Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

			NetworkServer.Spawn(Bullet);

			NetworkRocket bullet = Bullet.GetComponent<NetworkRocket>();
			bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

			bullet.SetupProjectile_ServerSide();

			RpcRocketFired(bullet, _bulletVector, _bulletSpeed);
		}

		/// <summary>
		/// Remote to send the projectile setup from the client to the server, using the method aboves
		/// positional data and direction 
		/// </summary>
		/// <param name="Bullet"></param>
		/// <param name="_bulletVector"></param>
		/// <param name="_bulletSpeed"></param>
		[ClientRpc]
		void RpcRocketFired(NetworkRocket Bullet, Vector3 _bulletVector, float _bulletSpeed)
		{
			Bullet.GetComponent<NetworkRocket>().SetupProjectile(currentPlayerUsername(), hasAuthority);

			Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
		}

		/// <summary>
		/// This is a simple method to find the username that has been setup by the player
		///
		/// I am not too sure as to why this has been placed inside of the player inventory module though
		/// </summary>
		/// <returns></returns>
		public string currentPlayerUsername()
		{
			return GetComponent<NetPlayer>().username;
		}

		/*[Command]
        void CmdSetAttackerUsername(string _username)
        {
            TPControllerManager.GetComponent<Health>().attackerUsername = _username;
        }*/



		/// <summary>
		/// When the player is destroyed set the inventory to null, not sure the need for this? unless its to prevent double spawn
		/// on the respawn which makes more sense
		/// </summary>
		private void OnDestroy()
		{
			if (isLocalPlayer)
			{
				UIPlayerInventory.playerInventory = null;
				slots.Callback -= Slots_Callback;
			}
		}
	}
}
