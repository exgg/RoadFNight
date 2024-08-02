using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.UIUtils;
using StarterAssets;
using UnityEngine.Serialization;
using RedicionStudio.InventorySystem;
public class SyncDictionaryIntDouble : SyncDictionary<int, double> { }

	public class PlayerInventoryModule : Inventory , IIntializeable, ITick, ILate
	{

		#region Calls
		
		// input components
		[HideInInspector]
		public StarterAssets.StarterAssetsInputs inputs;
		
		// player stat components
		[HideInInspector]
		public Health health;
        
		// weapon wheel components
		public WeaponWheelSystem weaponWheelSystem;
		public RectTransform rectTransform;
		
		// emote wheel components
		public EmoteWheel emoteWheel;
		
		// chat system components
		public ChatSystem chatSystem;
		
		#endregion
		
		[Header("Player Modules")]
		public Player player;
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

        public bool isWeaponWheelActive = false;
        RectTransform _rectTransform;

        public ChatSystem chatWindow;
        
        public static Keyboard Keyboard;
        public static Mouse Mouse;

        public bool inMenu;
        public bool inWeaponWheel;
        
        private int _index;
        private double _interval = 60f;
        private double _lastTime;

        public ItemSlot slot;
        
        [Space]
        [SerializeField] private Transform _gFX;
        
        private void Start() {
	        if (isLocalPlayer) {
		        
		        UIPlayerInventory.playerInventory = this;
		        slots.Callback += Slots_Callback;
		        UIPlayerInventory.InstanceRefresh();
	        }
	       
        }

        public void OnInitialize()
        {
	        //Inputs
	        inputs = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssets.StarterAssetsInputs>(); // errr what ? THEE most expensive call in unity... 
	        Keyboard = Keyboard.current;
	        Mouse = Mouse.current;
	        
	        //Classes
	        health = GetComponent<Health>();

	        //Weapon Wheel System
	        weaponWheelSystem = UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>();
	        rectTransform = weaponWheelSystem.MousePositionText.GetComponent<RectTransform>();
	        rectTransform.anchorMin = new Vector2(0, 0);
	        rectTransform.anchorMax = new Vector2(0, 0);
	        
	        // emote system
	        emoteWheel = GetComponent<EmoteWheel>();
	        
	        // Chat System
	        chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
	        chatSystem = chatWindow.GetComponent<ChatSystem>();
        }
        
        /// <summary>
        /// Update is a mess, lots of logic in here can be both improved and split off into other methods to control readability
        /// and allow for more dynamic control over it in the future. Split this up in phase 2 or 3
        /// </summary>
        public void OnTick()
        {
	        if(!isLocalPlayer || Keyboard == null || Mouse == null) // if is not the current player of this player game object or inputs not set return
		        return;

	        if (inShop) return;
        }
        
        /// <summary>
        /// Again more expensive code, and things that can be split into separate methods to avoid arrowheads
        /// </summary>
        public void OnLate()
        {
	        if(chatWindow == null)
		        chatWindow = chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
	        if (isServer) {
		        return;
	        }

	        for (int i = 0; i < _gFX.childCount; i++) {
		        _gFX.GetChild(i).gameObject.SetActive(false);
		        _gFX.GetChild(i).GetComponent<WeaponManager>().enabled = false;
	        }
	        if (!this.GetComponent<Health>().isDeath && !inCar && !usesParachute && !this.GetComponent<EmoteWheel>().isPlayingAnimation && slots[0].amount > 0 && slots[0].item.itemSO != null) {
		        this.GetComponent<Animator>().SetLayerWeight(1, 1);
		        for (int i = 0; i < _gFX.childCount; i++) {
			        if (_gFX.GetChild(i).name == slots[0].item.itemSO.uniqueName) {
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
        /// Initializes the inventory system slots, once those are initialized it will then look up if the player has
        /// anything in their inventory via the server saved data. After that has been completed it will then place that
        /// item into the slot that is required. Saving both the position in the inventory and the type of item it is.
        /// To then be loaded through here.
        /// </summary>
        public void LoadInventory() {
			for (int i = 0; i < 67; i++) {
				slots.Add(new ItemSlot());
			}

#if UNITY_SERVER || UNITY_EDITOR // ?
			MasterServer.MSClient.GetInventory(player.id, (inventoryData) => {
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
		/// Callback method triggered when the SyncList of ItemSlots is modified.
		/// Refreshes the UI inventory to reflect changes
		/// </summary>
		/// <param name="op">The operation performed on the list (Add, Remove, etc.)</param>
		/// <param name="itemIndex">The index of the item that was changed</param>
		/// <param name="oldItem">The old item before the change</param>
		/// <param name="newItem">The new item after the change</param>
		public void Slots_Callback(SyncList<ItemSlot>.Operation op, int itemIndex, ItemSlot oldItem, ItemSlot newItem) {
			UIPlayerInventory.InstanceRefresh();
		}
        
        /// <summary>
        /// When the player is destroyed set the inventory to null, not sure the need for this? unless its to prevent double spawn
        /// on the respawn which makes more sense
        /// </summary>
        private void OnDestroy() {
			if (isLocalPlayer) {
				UIPlayerInventory.playerInventory = null;
				slots.Callback -= Slots_Callback;
			}
		}
	}

