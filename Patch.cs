using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace BackdoorBandit
{
    internal class DoorBreachComponent : MonoBehaviour
    { //also all stolen from drakiaxyz
        protected static ManualLogSource Logger
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
            int doorCount = 0;
            int changedCount = 0;
            int invalidStateCount = 0;
            int inoperableCount = 0;
            int invalidLayerCount = 0;

            FindObjectsOfType<Door>().ExecuteForEach(door =>
            {
                doorCount++;

                // We don't want doors that aren't worth breaching.. so either closed doors or locked doors or breach doors.
                if (door.DoorState != EDoorState.Shut && door.DoorState != EDoorState.Locked && door.DoorState != EDoorState.Breaching)
                {
                    invalidStateCount++;
                    return;
                }

                // We don't support non-operatable doors
                if (!door.Operatable)
                {
                    inoperableCount++;
                    return;
                }

                // We don't support doors that aren't on the "Interactive" layer
                if (door.gameObject.layer != DoorBreachPlugin.interactiveLayer)
                {
                    invalidLayerCount++;
                    return;
                }

                // Create a random number of hitpoints
                var randhitpoints = UnityEngine.Random.Range(200, 300);

                //add hitpoints component to door
                var hitpoints = door.gameObject.GetOrAddComponent<Hitpoints>();

                hitpoints.hitpoints = randhitpoints;

                changedCount++;
                door.OnEnable();

            });

            /*Logger.LogInfo($"Total Doors: {doorCount}");
            Logger.LogInfo($"Changed Doors (Added Hitpoints): {changedCount}");
            Logger.LogInfo($"Invalid State Doors: {invalidStateCount}");
            Logger.LogInfo($"Inoperable Doors: {inoperableCount}");
            Logger.LogInfo($"Invalid Layer Doors: {invalidLayerCount}");*/
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<DoorBreachComponent>();
            }
        }
    }

    //create hitpoints unityengine.component
    internal class Hitpoints : MonoBehaviour
    {
        public float hitpoints;
    }

    internal class ApplyHit : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));

        [PatchPrefix]
        public static void Prefix(DamageInfo damageInfo, GStruct307 shotID)
        {
            //Logger.LogInfo("BackdoorBandit: Inside of the ApplyHit Method");
            var player = Singleton<GameWorld>.Instance.MainPlayer;

            //only want to apply damage to doors if the player is the one shooting and not a lampcontroller related
            if (player.IsYourPlayer && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window)
            {
                //setup colliders and check if we have the right components
                var collider = damageInfo.HittedBallisticCollider as BallisticCollider;
                bool isDoor = collider.transform.parent?.gameObject?.GetComponent<Door>() != null;
                bool hasHitPoints = collider.transform.parent?.gameObject?.GetComponent<Hitpoints>() != null;
                bool validDamage = false;

                if (DoorBreach.DoorBreachPlugin.PlebMode.Value)
                {
                    //if pleb mode is enabled, we don't need to check ammo or weapons
                    validDamage = true;
                }

                if (isDoor && hasHitPoints)
                {
                    //we know door was hit and is a valid door

                    //check if weapons or ammo types are valid only if pleb mode is false
                    if (!DoorBreachPlugin.PlebMode.Value)
                    {
                        checkWeaponAndAmmo(player, damageInfo, ref validDamage);
                    }

                    Logger.LogInfo($"BackdoorBandit: validDamage is {validDamage}");

                    var hitpoints = collider.transform.parent?.gameObject?.GetComponent<Hitpoints>() as Hitpoints;

                    if (validDamage)
                    {
                        Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints");
                        //subtract damage
                        hitpoints.hitpoints -= damageInfo.Damage;

                        if (hitpoints.hitpoints <= 0)
                        {
                            //open door
                            Door door = collider.transform.parent?.gameObject?.GetComponent<Door>();

                            door.KickOpen(true);
                        }
                    }

                }
            }

        }


        private static HashSet<string> grenadeLaunchers = new HashSet<string>
        {
            "62e7e7bbe6da9612f743f1e0", //GP-25 40mm underbarrel grenade launcher
            "6357c98711fb55120211f7e1", //M203 40mm underbarrel grenade launcher
            "5e81ebcd8e146c7080625e15", //FN40GL Mk2 40mm grenade launcher
            "5d52cc5ba4b9367408500062", //AGS-30 30x29mm automatic grenade launcher
            "6275303a9f372d6ea97f9ec7" //Milkor M32A1 MSGL 40mm grenade launcher
        };

        private static HashSet<string> validMeleeWeapons = new HashSet<string>
        {
            "63a0b208f444d32d6f03ea1e", //FierceBlowSledgeHammer
            "6087e570b998180e9f76dc24", //SuperforsDeadBlowHammer
            "5c07df7f0db834001b73588a", //FreemanCrowbar
            "5bffe7930db834001b734a39" //CrashAxe
        };

        private static string ShotgunParentID = "5447b6094bdc2dc3278b4567";

        private static void checkWeaponAndAmmo(Player player, DamageInfo damageInfo, ref bool validDamage)
        {
            var material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            var weapon = damageInfo.Weapon.TemplateId;

            Logger.LogInfo($"BB: Actual DamageType is : {damageInfo.DamageType}");

            if (damageInfo.DamageType != EDamageType.Bullet && damageInfo.DamageType != EDamageType.GrenadeFragment)
            {
                if (damageInfo.DamageType == EDamageType.Melee && validMeleeWeapons.Contains(weapon) && material != MaterialType.MetalThin && material != MaterialType.MetalThick)
                {
                    validDamage = true;
                }

                return;
            }

            var bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;

            if (material == MaterialType.MetalThin || material == MaterialType.MetalThick)
            {
                if (grenadeLaunchers.Contains(weapon) && (bulletTemplate._id.LocalizedName().Contains("HE") || bulletTemplate._id.LocalizedName().ToLower().Contains("shrapnel")))
                {
                    Logger.LogInfo($"BB: HE round detected on metal door. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    validDamage = true;
                    return;
                }
            }
            else
            {
                Logger.LogInfo($"BB: bullet use detected on non metal. weapon used: {damageInfo.Weapon.LocalizedName()}");
                Logger.LogInfo($"BB: bulletTemplate is {bulletTemplate._name.Localized()}");

                //check if grenadelauncher and HE round
                if (grenadeLaunchers.Contains(weapon) && (bulletTemplate._id.LocalizedName().Contains("HE") || bulletTemplate._id.LocalizedName().ToLower().Contains("shrapnel")))
                {
                    Logger.LogInfo($"BB: HE round detected on {material} material. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    validDamage = true;
                }

                //check if shotgun and slug round
                if (damageInfo.Weapon.Template.Parent._id == ShotgunParentID && bulletTemplate._id.LocalizedName().ToLower().Contains("slug"))
                {
                    Logger.LogInfo($"BB: Slug round detected on {material} material. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    validDamage = true;
                }



            }
        }



    }
}
