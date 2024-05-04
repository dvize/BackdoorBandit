using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Systems.Effects;
using UnityEngine;

namespace BackdoorBandit
{
    internal class ExplosiveBreachComponent : MonoBehaviour
    {
        internal static Player player;
        internal static GameWorld gameWorld;
        internal static List<C4Instance> c4Instances;
        private static ExplosiveBreachComponent componentInstance;
        //private static readonly string TNTTemplateId = "60391b0fb847c71012789415";
        private static readonly string C4ExplosiveId = "6636606320e842b50084e51a";
        private static Vector2 _impactsGagRadius;
        private static Effects effectsInstance;
        private static CameraClass cameraInstance;
        private static BetterAudio betterAudioInstance;
        internal static ManualLogSource Logger
        {
            get; private set;
        }

        private ExplosiveBreachComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(ExplosiveBreachComponent));
            }
        }

        private void Start()
        {
            //initialize variables
            c4Instances = new List<C4Instance>();
            componentInstance = this;
            gameWorld = Singleton<GameWorld>.Instance;
            player = gameWorld.MainPlayer;
            _impactsGagRadius = new Vector2(1f, 3f);
            effectsInstance = Singleton<Effects>.Instance;
            cameraInstance = CameraClass.Instance;
            betterAudioInstance = Singleton<BetterAudio>.Instance;
        }
        internal static bool hasC4Explosives(Player player)
        {
            // Search playerItems for first c4 explosive
            var foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == C4ExplosiveId);

            if (foundItem != null)
            {
                return true;
            }

            return false;
        }

        internal static bool IsValidDoorState(Door door) =>
                    door.DoorState == EDoorState.Shut || door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Breaching;

        internal static void StartExplosiveBreach(Door door, Player player)
        {
            TryPlaceC4OnDoor(door, player);

            RemoveItemFromPlayerInventory(player);

            // Ensure we have a reference to the ExplosiveBreachComponent.
            if (componentInstance == null)
            {
                componentInstance = gameWorld.GetComponent<ExplosiveBreachComponent>();
                if (componentInstance == null)
                {
                    componentInstance = gameWorld.gameObject.AddComponent<ExplosiveBreachComponent>();
                }
            }

            // Start a coroutine for the most recently placed TNT.
            if (c4Instances.Any())
            {
                var latestC4Instance = c4Instances.Last();
                StartDelayedExplosionCoroutine(door, player, componentInstance, latestC4Instance);
            }
        }

        private static void TryPlaceC4OnDoor(Door door, Player player)
        {
            var itemFactory = Singleton<ItemFactory>.Instance;
            var c4Item = itemFactory.CreateItem(MongoID.Generate(), C4ExplosiveId, null);

            // Attempt to find the DoorHandle component within the children of the Door GameObject
            DoorHandle doorHandle = door.GetComponentInChildren<DoorHandle>();
            if (doorHandle == null)
            {
                Logger.LogError("DoorHandle component not found.");
                return;
            }

            Vector3 handlePosition = doorHandle.transform.position;

            // Determine if the player is in front of or behind the door
            Vector3 doorToPlayer = player.Transform.position - door.transform.position;
            bool playerInFront = Vector3.Dot(doorToPlayer, door.transform.forward) > 0;

            // Adjust position to place the C4 above the door handle (lock)
            Vector3 positionOffset = Vector3.up * 0.2f; // Offset upwards from the handle
            Vector3 forwardOffset = door.transform.forward * (playerInFront ? -0.05f : 0.05f); // Conditional offset based on player's position
            Vector3 c4Position = handlePosition + positionOffset + forwardOffset;

            // Correct rotation: Face flat against the door
            Quaternion rotation = Quaternion.LookRotation(playerInFront ? -door.transform.forward : door.transform.forward, Vector3.up); // C4 should face outward correctly depending on player's position

            // Place the C4 item in the game world
            LootItem lootItem = gameWorld.SetupItem(c4Item, player.InteractablePlayer, c4Position, rotation);

            c4Instances.Add(new C4Instance(lootItem, c4Position));
        }


        private static void RemoveItemFromPlayerInventory(Player player)
        {
            var foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == C4ExplosiveId);
            if (foundItem == null) return;

            var traderController = (TraderControllerClass)foundItem.Parent.GetOwner();
            var discardResult = InteractionsHandlerClass.Discard(foundItem, traderController, false, false);

            if (discardResult.Error != null)
            {
                Logger.LogError($"Couldn't remove item: {discardResult.Error}");
                return;
            }

            discardResult.Value.RaiseEvents(traderController, CommandStatus.Begin);
            discardResult.Value.RaiseEvents(traderController, CommandStatus.Succeed);
        }

        private static void StartDelayedExplosionCoroutine(Door door, Player player, MonoBehaviour monoBehaviour, C4Instance c4Instance)
        {
            monoBehaviour.StartCoroutine(DelayedExplosion(door, player, c4Instance));
        }

        private static IEnumerator DelayedExplosion(Door door, Player player, C4Instance c4Instance)
        {
            // Wait for 10 seconds.
            yield return new WaitForSeconds(DoorBreachPlugin.explosiveTimerInSec.Value);

            // Apply explosion effect
            effectsInstance.EmitGrenade("big_explosion", c4Instance.LootItem.transform.position, Vector3.forward, 15f);
            ApplyHit.OpenDoorIfNotAlreadyOpen(door, player, EInteractionType.Breach);

            //delete TNT from gameWorld
            if (c4Instance.LootItem != null)
            {
                //tntInstance.LootItem.Kill();
                UnityEngine.Object.Destroy(c4Instance.LootItem.gameObject);
            }

            c4Instances.Remove(c4Instance);
        }


        /*private static DamageInfo tntDamage()
        {
            return new DamageInfo
            {
                DamageType = EDamageType.Landmine,
                ArmorDamage = 300f,
                StaminaBurnRate = 100f,
                PenetrationPower = 100f,
                Direction = Vector3.zero,
                Player = null,
                IsForwardHit = true
            };
        }*/

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<ExplosiveBreachComponent>();
            }

        }


    }



}

