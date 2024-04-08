using EFT.Interactive;
using UnityEngine;

namespace BackdoorBandit
{
    internal class TNTInstance
    {
        public LootItem LootItem
        {
            get; set;
        }
        public Vector3 Position
        {
            get; set;
        }

        public TNTInstance(LootItem lootItem, Vector3 position)
        {
            LootItem = lootItem;
            Position = position;
        }
    }
}
