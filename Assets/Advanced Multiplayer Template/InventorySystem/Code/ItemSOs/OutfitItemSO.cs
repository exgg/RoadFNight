using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedicionStudio.InventorySystem{

    [CreateAssetMenu(fileName = "New Outfit Item SO", menuName = "Inventory System/ItemSOs/Outfit")]
    public class OutfitItemSO : ItemSO{

        [Header("Outfit")]
        public string outfitStyle;
    }
}
