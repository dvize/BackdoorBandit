using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Interactive;
using Newtonsoft.Json;
using UnityEngine;


#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type

namespace BackdoorBandit
{
    internal class DoorBreachComponent : MonoBehaviour
    {
        private static int doorCount = 0;
        private static int invalidStateCount = 0;
        private static int inoperableCount = 0;
        private static int invalidLayerCount = 0;
        private static int containerCount = 0;
        private static int invalidContainers = 0;
        private static int inoperatableContainers = 0;
        private static int invalidContainerLayer = 0;
        private static int trunkCount = 0;
        private static int invalidCarTrunks = 0;
        private static int inoperatableTrunks = 0;
        private static int invalidTrunkLayer = 0;

        internal static HashSet<string> GrenadeLaunchers = new HashSet<string>();
        internal static HashSet<string> MeleeWeapons = new HashSet<string>();

        internal static ManualLogSource Logger
        {
            get; private set;
        }

        private DoorBreachComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DoorBreachComponent));
            }
        }

        public void Awake()
        {
            doorCount = 0;
            invalidStateCount = 0;
            inoperableCount = 0;
            invalidLayerCount = 0;
            containerCount = 0;
            invalidContainers = 0;
            inoperatableContainers = 0;
            invalidContainerLayer = 0;
            trunkCount = 0;
            invalidCarTrunks = 0;
            inoperatableTrunks = 0;
            invalidTrunkLayer = 0;

            LoadHashSetFromJson(GrenadeLaunchers, "GrenadeLaunchers.json");
            LoadHashSetFromJson(MeleeWeapons, "MeleeWeapons.json");

            ProcessObjectsOfType<Door>("Doors", DoorBreachPlugin.interactiveLayer);
            ProcessObjectsOfType<LootableContainer>("Containers", DoorBreachPlugin.interactiveLayer);
            ProcessObjectsOfType<Trunk>("Trunks", DoorBreachPlugin.interactiveLayer);

            LogStatistics("Doors", doorCount, invalidStateCount, inoperableCount, invalidLayerCount);
            LogStatistics("Containers", containerCount, invalidContainers, inoperatableContainers, invalidContainerLayer);
            LogStatistics("Trunks", trunkCount, invalidCarTrunks, inoperatableTrunks, invalidTrunkLayer);
        }

        private void ProcessObjectsOfType<T>(string objectType, int interactiveLayer) where T : Component
        {
            int count = 0;
            int invalidCount = 0;
            int inoperableCount = 0;
            int invalidLayerCount = 0;

            FindObjectsOfType<T>().ExecuteForEach(obj =>
            {
                count++;

                if (!IsValidObject(obj, ref invalidCount, ref inoperableCount, ref invalidLayerCount, interactiveLayer))
                    return;

                var randHitPoints = UnityEngine.Random.Range(DoorBreachPlugin.MinHitPoints.Value, DoorBreachPlugin.MaxHitPoints.Value);
                var hitpoints = obj.gameObject.GetOrAddComponent<Hitpoints>();
                hitpoints.hitpoints = randHitPoints;

                if (obj is Door door)
                {
                    door.OnEnable();
                }
                else if (obj is LootableContainer container)
                {
                    container.OnEnable();
                }
                else if (obj is Trunk trunk)
                {
                    trunk.OnEnable();
                }
            });

            LogStatistics(objectType, count, invalidCount, inoperableCount, invalidLayerCount);
        }

        private bool IsOperatable<T>(T obj) where T : Component
        {
            if (obj is Door door)
            {
                return door.Operatable;
            }
            else if (obj is LootableContainer container)
            {
                return container.Operatable;
            }
            else if (obj is Trunk trunk)
            {
                return trunk.Operatable;
            }

            // Default case: assume operatable if not one of the specific types
            return true;
        }

        private bool IsValidObject<T>(T obj, ref int invalidCount, ref int inoperableCount, ref int invalidLayerCount, int interactiveLayer) where T : Component
        {
            if (obj is Door door && !IsValidDoorState(door))
            {
                invalidCount++;
                return false;
            }

            if (obj is LootableContainer container && !IsValidContainerState(container))
            {
                invalidCount++;
                return false;
            }

            if (obj is Trunk trunk && !IsValidTrunkState(trunk))
            {
                invalidCount++;
                return false;
            }

            if (!IsOperatable(obj))
            {
                inoperableCount++;
                return false;
            }

            if (!IsValidLayer(obj, interactiveLayer))
            {
                invalidLayerCount++;
                return false;
            }

            return true;
        }

        private bool IsValidDoorState(Door door) =>
            door.DoorState == EDoorState.Shut || door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Breaching || door.DoorState == EDoorState.Open;

        private bool IsValidContainerState(LootableContainer container) =>
            container.DoorState == EDoorState.Shut || container.DoorState == EDoorState.Locked || container.DoorState == EDoorState.Breaching;

        private bool IsValidTrunkState(Trunk trunk) =>
            trunk.DoorState == EDoorState.Shut || trunk.DoorState == EDoorState.Locked || trunk.DoorState == EDoorState.Breaching;

        private bool IsValidLayer<T>(T obj, int interactiveLayer) where T : Component =>
            obj.gameObject.layer == interactiveLayer;


        private void LogStatistics(string objectType, int totalCount, int invalidStateCount, int inoperableCount, int invalidLayerCount)
        {
            Logger.LogInfo($"Total {objectType}: {totalCount}");
            Logger.LogInfo($"Invalid State {objectType}: {invalidStateCount}");
            Logger.LogInfo($"Inoperable {objectType}: {inoperableCount}");
            Logger.LogInfo($"Invalid Layer {objectType}: {invalidLayerCount}");
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DoorBreachComponent>();
            }
        }
        private void LoadHashSetFromJson(HashSet<string> hashSet, string jsonFileName)
        {
            string dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string jsonPath = Path.Combine(dllDirectory, jsonFileName);

            if (File.Exists(jsonPath))
            {
                string jsonContent = File.ReadAllText(jsonPath);
                hashSet = JsonConvert.DeserializeObject<HashSet<string>>(jsonContent);
            }
            else
            {
                Logger.LogError($"JSON file not found: {jsonFileName}");
            }
        }
    }

}

