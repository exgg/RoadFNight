using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using RedicionStudio.NetworkUtils;

namespace RedicionStudio.InventorySystem
{
    public class ShopItem : NetworkBehaviour
    {
        public ItemSO itemSO;
        public Item item;
        public int amount;

        [Space]
        public string itemName;

        [Space]
        public ConfigurationSO rarityConfiguration;
        public string itemRarity;
        public Image rarityImage;

        [Space]
        public Sprite itemSprite;

        [Space]
        public int itemPrice;

        [Space]
        [Header("UI")]
        public TMPro.TMP_Text itemNameText;
        public TMPro.TMP_Text itemRarityText;
        public TMPro.TMP_Text itemPriceText;
        public Image itemImage;

        [Space]
        public Vector3 Scale;

        public void SetUpItem()
        {
            itemNameText.text = itemName;
            itemRarityText.text = itemRarity;
            itemPriceText.text = itemPrice + "$";
            itemImage.sprite = itemSprite;
            if (itemRarity == "None")
                rarityImage.color = Color.black;
            else if(itemRarity == "Common")
                rarityImage.color = rarityConfiguration.commonColor;
            else if(itemRarity == "Rare")
                rarityImage.color = rarityConfiguration.rareColor;
            else if (itemRarity == "Unique")
                rarityImage.color = rarityConfiguration.uniqueColor;

            this.GetComponent<RectTransform>().localScale = Scale;
        }

        public void BuyItem()
        {
            GameObject _localPlayer;

            _localPlayer = NetworkClient.localPlayer.gameObject;

            item = new Item(itemSO.uniqueName, itemSO is ConsumableItemSO consumableItemSO ? consumableItemSO.shelfLifeInSeconds : 0f);

            _localPlayer.GetComponent<PlayerInteractionModule>().AddItem(_localPlayer.GetComponent<PlayerInventoryModule>(), itemPrice, item, amount);
        }
    }
}
