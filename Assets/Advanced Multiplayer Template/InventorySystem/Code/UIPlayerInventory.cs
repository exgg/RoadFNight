using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace RedicionStudio.InventorySystem {

	public class UIPlayerInventory : MonoBehaviour {

		private static UIPlayerInventory _instance;

		[SerializeField] private GameObject _content;
		[SerializeField] private Transform _slotsContent;
        public static GameObject WeaponWheel;
        public static GameObject InventoryUI;
        [SerializeField] private UISlot slotA; // Weapon slot
		[SerializeField] private UISlot slotB; // Ammo slot
        [SerializeField] private UISlot slotC; // Outfit slot
        [SerializeField] private UISlot slotD; // Pet slot
        public UnityEngine.UI.Image currentCharacter;
        public ItemShopInteraction itemShopInteraction;

        private void Awake() {
			if (_instance == null) {
				_instance = this;
			}
			else {
				throw new UnityException("Instance");
			}
            #region Get Children
            foreach (Transform child in _instance.transform)
            {
                if (child.name == "WeaponWheel")
                {
                    WeaponWheel = child.gameObject;
                }
                if (child.name == "Content")
                {
                    InventoryUI = child.gameObject;
                }
            }
            #endregion
        }

        public static PlayerInventoryModule playerInventory;

		public static void SetActive(bool value) {
			_instance._content.SetActive(value);
		}

		private static List<UISlot> _instantiatedGOs = new List<UISlot>();

		private static void ClearAndInstantiateUISlots(Transform parent, GameObject prefab, int amount) {
			int i;

			for (i = 0; i < _instantiatedGOs.Count; i++) {
				Destroy(_instantiatedGOs[i].gameObject);
			}
			_instantiatedGOs.Clear();

			for (i = 0; i < amount; i++) {
				GameObject gO = Instantiate(prefab);
				gO.transform.SetParent(parent, false);
				_instantiatedGOs.Add(gO.GetComponent<UISlot>());
			}
		}

		[SerializeField] private UISlot _uISlotPrefab;

		private void OnUISlotPointerDown(ItemSlot slot, int slotIndex) {
			if (4 <= slotIndex && slotIndex < playerInventory.slots.Count && playerInventory.slots[slotIndex].amount > 0 && playerInventory.slots[slotIndex].item.itemSO is UseableItemSO usableItemSO) {
				playerInventory.CmdUseItem(slotIndex);
			}
		}

		private static UISlot _uISlot;
		private static ItemSlot _slot;

		private void UpdateSlot(int slotIndex) {
			if (slotIndex == 0) {
				_uISlot = slotA;
			}
			else if (slotIndex == 1) {
				_uISlot = slotB;
			}
            else if (slotIndex == 2){
                _uISlot = slotC;
            }
            else if (slotIndex == 3){
                _uISlot = slotD;
            }
            else {
				_uISlot = _instantiatedGOs[slotIndex - 4];
			}

			_uISlot.gameObject.name = slotIndex.ToString();

			_slot = playerInventory.slots[slotIndex];

			if (_slot.amount > 0) {
				// Valid
				_uISlot.button.onClick.AddListener(() => OnUISlotPointerDown(_slot, slotIndex)); //!
				_uISlot.uITooltip.enabled = true;
				_uISlot.uITooltip.text = _slot.TooltipText;
				_uISlot.uIDragAndDrop.draggable = true;
				_uISlot.image.sprite = _slot.item.itemSO.sprite;
                _uISlot.sellPrice = _slot.item.itemSO.sellPrice;
                _uISlot.item = _slot.item;
                _uISlot.playerInteractionModule = playerInventory.GetComponent<PlayerInteractionModule>();
                _uISlot.itemSlotIndex = slotIndex;
                itemShopInteraction.Refresh();
                /*if (_slot.item.itemSO is UseableItemSO usableItemSO) {
					_uISlot.cooldownIndicator.fillAmount = usableItemSO.cooldownInSeconds > 0f ? playerInventory.GetCooldown(_slot.item.hash) / usableItemSO.cooldownInSeconds : 0f; // ?
				}
				else {
					_uISlot.cooldownIndicator.fillAmount = 0f;
				}*/
                _uISlot.amountContent.SetActive(_slot.amount > 1);
				_uISlot.amountText.text = 'x' + _slot.amount.ToString(); // ?
				if (_slot.item.itemSO.rarity != ItemSO.Rarity.None) {
					switch (_slot.item.itemSO.rarity) {
						case ItemSO.Rarity.Common:
							_uISlot.rarityImage.color = ConfigurationSO.Instance.commonColor;
							break;
						case ItemSO.Rarity.Rare:
							_uISlot.rarityImage.color = ConfigurationSO.Instance.rareColor;
							break;
						case ItemSO.Rarity.Unique:
							_uISlot.rarityImage.color = ConfigurationSO.Instance.uniqueColor;
							break;
					}
					_uISlot.rarityText.text = _slot.item.itemSO.rarity.ToString();
				}
				_uISlot.rarityImage.gameObject.SetActive(_slot.item.itemSO.rarity != ItemSO.Rarity.None);
				_uISlot.image.gameObject.SetActive(true);
				return;
			}
			else {
				// Invalid
				_uISlot.button.onClick.RemoveAllListeners();
				_uISlot.uITooltip.enabled = false;
				_uISlot.uIDragAndDrop.draggable = false;
				_uISlot.image.gameObject.SetActive(false);
			}
		}

		private void Refresh() {
			ClearAndInstantiateUISlots(_slotsContent, _uISlotPrefab.gameObject, playerInventory.slots.Count - 4);

			for (int i = 0; i < playerInventory.slots.Count; i++) {
				UpdateSlot(i);
			}
		}

		public static void InstanceRefresh() {
			_instance.Refresh();
		}

#if !UNITY_SERVER || UNITY_EDITOR
		public void Update() {
			if (NetPlayer.LocalNetPlayer == null) {
				_content.SetActive(false);
				TPController.TPCameraController.LockCursor(false);
				return;
			}

			for (int i = 0; i < playerInventory.slots.Count; i++) {
				if (i == 0) {
					_uISlot = slotA;
				}
				else if (i == 1) {
					_uISlot = slotB;
				}
                else if (i == 2){
                    _uISlot = slotC;
                }
                else if (i == 3){
                    _uISlot = slotD;
                }
                else {
					_uISlot = _instantiatedGOs[i - 4];
				}

				_slot = playerInventory.slots[i];

				if (_slot.amount > 0 && _slot.item.itemSO is UseableItemSO useableItemSO) {
					_uISlot.cooldownIndicator.fillAmount = useableItemSO.cooldownInSeconds > 0f ? playerInventory.GetCooldown(useableItemSO.cooldownTag.GetStableHashCode()) / useableItemSO.cooldownInSeconds : 0f; // ?
				}
				else {
					_uISlot.cooldownIndicator.fillAmount = 0f;
				}
			}
		}
#endif
	}
}
