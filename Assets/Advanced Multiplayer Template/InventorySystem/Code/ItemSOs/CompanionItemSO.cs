using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedicionStudio.InventorySystem{

    [CreateAssetMenu(fileName = "New Companion Item SO", menuName = "Inventory System/ItemSOs/Companion")]
    public class CompanionItemSO : ItemSO{

        [Header("Companion")]
        public string companionNote;
    }
}
