using UnityEngine;

namespace TDF.Core.Data
{
    [CreateAssetMenu(fileName = "NewShopItem", menuName = "TDF/Data/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        
        [Header("Cost")]
        public int costGold;
        public int costGems;

        [Header("Rewards")]
        public int rewardGold;
        public int rewardGems;
        // 추후 아이템이나 영웅 보상 등이 추가될 수 있습니다.

        [Header("Visuals")]
        public Sprite itemIcon;
    }
}
