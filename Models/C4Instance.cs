using EFT.Interactive;
using UnityEngine;

namespace BackdoorBandit
{
    public class C4Instance
    {
        public LootItem LootItem
        {
            get;
            set;
        }
        public Vector3 Position
        {
            get;
            set;
        }
        public Coroutine ExplosionCoroutine
        {
            get;
            set;
        }

        public C4Instance(LootItem lootItem, Vector3 position)
        {
            LootItem = lootItem;
            Position = position;
        }
    }


}
