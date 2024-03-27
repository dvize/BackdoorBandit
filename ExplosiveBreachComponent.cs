using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
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
        internal static List<TNTInstance> tntInstances;
        private static ExplosiveBreachComponent componentInstance;
        private static readonly string TNTTemplateId = "60391b0fb847c71012789415";
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
            tntInstances = new List<TNTInstance>();
            componentInstance = this;
            gameWorld = Singleton<GameWorld>.Instance;
            player = gameWorld.MainPlayer;
            _impactsGagRadius = new Vector2(1f, 3f);
            effectsInstance = Singleton<Effects>.Instance;
            cameraInstance = CameraClass.Instance;
            betterAudioInstance = Singleton<BetterAudio>.Instance;
        }
        internal static bool hasTNTExplosives(Player player)
        {
            // Search playerItems for first TNT-200 of item.TemplateId 60391b0fb847c71012789415
            var foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == "60391b0fb847c71012789415");

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
            TryPlaceTNTOnDoor(door, player);

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
            if (tntInstances.Any())
            {
                var latestTNTInstance = tntInstances.Last();
                StartDelayedExplosionCoroutine(door, player, componentInstance, latestTNTInstance);
            }
        }

        private static void TryPlaceTNTOnDoor(Door door, Player player)
        {
            var itemFactory = Singleton<ItemFactory>.Instance;
            var tntItem = itemFactory.CreateItem(MongoID.Generate(), TNTTemplateId, null);

            // Attempt to find the DoorHandle component within the children of the Door GameObject
            DoorHandle doorHandle = door.GetComponentInChildren<DoorHandle>();
            if (doorHandle == null)
            {
                Logger.LogError("DoorHandle component not found.");
                return; // Exit if the DoorHandle component is not found
            }

            // Use the DoorHandle's position as the base for placing the TNT
            Vector3 handlePosition = doorHandle.transform.position;

            // Position the TNT just in front of the door, near the handle.
            Vector3 positionOffset = door.transform.forward * -0.13f; // Move the TNT slightly in front of the door based on its forward direction
            Vector3 tntPosition = handlePosition + positionOffset;
            tntPosition.y = handlePosition.y;

            // make the TNT lay against the lock and face the player, 
            Quaternion baseRotation = Quaternion.LookRotation(door.transform.forward, -Vector3.up);

            // make the TNT face the player.
            Vector3 toPlayerFlat = player.Transform.position - tntPosition;
            toPlayerFlat.y = 0;
            Quaternion toPlayerRotation = Quaternion.LookRotation(toPlayerFlat);
            Quaternion rotation = Quaternion.Lerp(baseRotation, toPlayerRotation, 0.5f);

            LootItem lootItem = gameWorld.SetupItem(tntItem, player.InteractablePlayer, tntPosition, rotation);

            tntInstances.Add(new TNTInstance(lootItem, tntPosition));
        }


        private static void RemoveItemFromPlayerInventory(Player player)
        {
            var foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == TNTTemplateId);
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

        private static void StartDelayedExplosionCoroutine(Door door, Player player, MonoBehaviour monoBehaviour, TNTInstance tntInstance)
        {
            monoBehaviour.StartCoroutine(DelayedExplosion(door, player, tntInstance));
        }

        private static IEnumerator DelayedExplosion(Door door, Player player, TNTInstance tntInstance)
        {
            // Wait for 10 seconds.
            yield return new WaitForSeconds(10);

            // Apply explosion effect
            effectsInstance.EmitGrenade("big_explosion", tntInstance.LootItem.transform.position, Vector3.forward, 15f);
            ApplyHit.OpenDoorIfNotAlreadyOpen(door, player, EInteractionType.Breach);

            //delete TNT from gameWorld
            if (tntInstance.LootItem != null)
            {
                //tntInstance.LootItem.Kill();
                UnityEngine.Object.Destroy(tntInstance.LootItem.gameObject);
            }

            tntInstances.Remove(tntInstance);
        }


        private static DamageInfo tntDamage()
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
        }

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

