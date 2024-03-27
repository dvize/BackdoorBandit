using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT.InventoryLogic;
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
