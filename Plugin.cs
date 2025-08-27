using System;
using System.Diagnostics;
using System.Reflection;
using SPT.Reflection.Patching;
using BackdoorBandit.Patches;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using VersionChecker;

namespace DoorBreach
{
    [BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "1.11.1")]
    public class DoorBreachPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> PlebMode;
        public static ConfigEntry<bool> SemiPlebMode;
        public static ConfigEntry<bool> BreachingRoundsOpenMetalDoors;
        public static ConfigEntry<bool> OpenLootableContainers;
        public static ConfigEntry<bool> OpenCarDoors;
        public static ConfigEntry<int> MinHitPoints;
        public static ConfigEntry<int> MaxHitPoints;
        public static ConfigEntry<int> explosiveTimerInSec;
        public static ConfigEntry<bool> explosionDoesDamage;
        public static ConfigEntry<int> explosionRadius;
        public static ConfigEntry<int> explosionDamage;

        public static int interactiveLayer;

        private void Awake()
        {

            PlebMode = Config.Bind(
                "1. Main Settings",
                "Plebmode",
                false,
                new ConfigDescription("Enabled Means No Requirements To Breach Any Door/LootContainer",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 5 }));

            SemiPlebMode = Config.Bind(
                "1. Main Settings",
                "Semi-Plebmode",
                false,
                new ConfigDescription("Enabled Means Any Round Breach Regular Doors, Not Reinforced doors",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            BreachingRoundsOpenMetalDoors = Config.Bind(
                "1. Main Settings",
                "Breach Rounds Affects Metal Doors",
                false,
                new ConfigDescription("Enabled Means Any Breach Round opens a door",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            OpenLootableContainers = Config.Bind(
                "1. Main Settings",
                "Breach Lootable Containers",
                false,
                new ConfigDescription("If enabled, can use shotgun breach rounds on safes",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            OpenCarDoors = Config.Bind(
                "1. Main Settings",
                "Breach Car Doors",
                false,
                new ConfigDescription("If Enabled, can use shotgun breach rounds on car doors",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            MinHitPoints = Config.Bind(
                "2. Hit Points",
                "Min Hit Points",
                100,
                new ConfigDescription("Minimum Hit Points Required To Breach, Default 100",
                new AcceptableValueRange<int>(0, 1000),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            MaxHitPoints = Config.Bind(
                "2. Hit Points",
                "Max Hit Points",
                200,
                new ConfigDescription("Maximum Hit Points Required To Breach, Default 200",
                new AcceptableValueRange<int>(0, 2000),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            explosiveTimerInSec = Config.Bind(
                "3. Explosive",
                "Explosive Timer In Sec",
                10,
                new ConfigDescription("Time in seconds for explosive breach to detonate",
                new AcceptableValueRange<int>(1, 60),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            explosionDoesDamage = Config.Bind(
                "3. Explosive",
                "Enable Explosive Damage",
                false,
                new ConfigDescription("Enable damage from the explosive",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 3 }));

            explosionRadius = Config.Bind(
                "3. Explosive",
                "Explosion Radius",
                5,
                new ConfigDescription("Sets the radius for the explosion",
                new AcceptableValueRange<int>(0, 200),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            explosionDamage = Config.Bind(
               "3. Explosive",
               "Explosion Damage",
               80,
               new ConfigDescription("Amount of HP Damage the Explosion Causes",
               new AcceptableValueRange<int>(0, 500),
               new ConfigurationManagerAttributes { IsAdvanced = false, Order = 1 }));

            new NewGamePatch().Enable();
            new BackdoorBandit.ApplyHit().Enable();
            new ActionMenuDoorPatch().Enable();
            new ActionMenuKeyCardPatch().Enable();
            new PerfectCullingNullRefPatch().Enable();
        }
    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //stolen from drakiaxyz - thanks
            DoorBreachPlugin.interactiveLayer = LayerMask.NameToLayer("Interactive");

            BackdoorBandit.DoorBreachComponent.Enable();
            BackdoorBandit.ExplosiveBreachComponent.Enable();
        }
    }
}
