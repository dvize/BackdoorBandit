using System.Collections.Generic;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
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
            int inoperableCount = 0;
            int invalidLayerCount = 0;
            int invalidStateCount = 0;

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
                var randhitpoints = UnityEngine.Random.Range(100, 200);

                //add hitpoints component to door
                var hitpoints = door.gameObject.GetOrAddComponent<Hitpoints>();

                hitpoints.hitpoints = randhitpoints;

                door.OnEnable();

            });

            Logger.LogInfo($"Total Doors: {doorCount}");
            Logger.LogInfo($"Invalid State Doors: {invalidStateCount}");
            Logger.LogInfo($"Inoperable Doors: {inoperableCount}");
            Logger.LogInfo($"Invalid Layer Doors: {invalidLayerCount}");
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
        private static BallisticCollider collider;
        private static bool isDoor;
        private static bool hasHitPoints;
        private static bool validDamage;
        private static Hitpoints hitpoints;
        private static Door door;
        protected override MethodBase GetTargetMethod() => typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


        [PatchPostfix]
        public static void PatchPostFix(DamageInfo damageInfo, GStruct307 shotID)
        {
            //Logger.LogInfo("BackdoorBandit: Inside of the ApplyHit Method");

            //only want to apply damage to doors if the player is the one shooting and not a lampcontroller related
            if (damageInfo.Player != null
                && damageInfo.Player.IsYourPlayer
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window
                && damageInfo.DamageType != EDamageType.Explosion)
            {
                //setup colliders and check if we have the right components
                collider = damageInfo.HittedBallisticCollider as BallisticCollider;

                isDoor = false;
                hasHitPoints = false;
                validDamage = DoorBreachPlugin.PlebMode.Value ? true : false;

                try
                {
                    //check if collider is in the children of the door.. found out the heirarchy is different for doors on different maps.
                    //see if any of the parents are a door

                    isDoor = collider.GetComponentInParent<Door>() != null;
                    //check if door we found has hitpoints
                    hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;
                }
                catch { }

                //Logger.LogInfo($"BackdoorBandit: isDoor is {isDoor}");
                //Logger.LogInfo($"BackdoorBandit: hasHitPoints is {hasHitPoints}");


                if (isDoor && hasHitPoints)
                {
                    //we know door was hit and is a valid door

                    //check if weapons or ammo types are valid only if pleb mode is false
                    if (!DoorBreachPlugin.PlebMode.Value)
                    {
                        checkWeaponAndAmmo(damageInfo.Player, damageInfo, ref validDamage);
                    }

                    //Logger.LogInfo($"BackdoorBandit: validDamage is {validDamage}");

                    hitpoints = collider.GetComponentInParent<Hitpoints>() as Hitpoints;

                    if (validDamage)
                    {
                        //Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints");
                        //subtract damage
                        hitpoints.hitpoints -= damageInfo.Damage;

                        //check if door is openable
                        if (hitpoints.hitpoints <= 0)
                        {
                            //open door and load correctly
                            door = collider.GetComponentInParent<Door>();
                            damageInfo.Player.CurrentState.ExecuteDoorInteraction(door, new GClass2600(EInteractionType.Breach), null, damageInfo.Player);
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

        private static void checkWeaponAndAmmo(Player player, DamageInfo damageInfo, ref bool validDamage)
        {
            var material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            var weapon = damageInfo.Weapon.TemplateId;

            //Logger.LogInfo($"BB: Actual DamageType is : {damageInfo.DamageType}");

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
                if (grenadeLaunchers.Contains(weapon) && (isHEGrenade(bulletTemplate) || isShrapnel(bulletTemplate)))
                {
                    //Logger.LogInfo($"BB: HE round detected on metal door. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    validDamage = true;
                    return;
                }
            }
            else
            {
                //Logger.LogInfo($"BB: bullet use detected on non metal. weapon used: {damageInfo.Weapon.LocalizedName()}");
                //Logger.LogInfo($"BB: bulletTemplate is {bulletTemplate._name.Localized()}");

                //check if grenadelauncher and HE round
                if (grenadeLaunchers.Contains(weapon) && (isHEGrenade(bulletTemplate) || isShrapnel(bulletTemplate)))
                {
                    //Logger.LogInfo($"BB: HE round detected on {material} material. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    validDamage = true;
                }

                //check if shotgun and slug round
                if (isShotgun(damageInfo) && isBreachingSlug(bulletTemplate) && isValidHit(damageInfo))
                {
                    //Logger.LogInfo($"BB: Slug round detected on {material} material. weapon used: {damageInfo.Weapon.LocalizedName()}");
                    damageInfo.Damage = 200;
                    validDamage = true;
                }

            }
        }


        private static bool isShrapnel(AmmoTemplate bulletTemplate)
        {
            Logger.LogDebug("BB: detected grenade shrapnel");

            //check if bulletTemplate is shrapnel and we only want grenade shrapnel not bullet shrapnel
            return (bulletTemplate.FragmentType == "5485a8684bdc2da71d8b4567");
        }

        private static bool isHEGrenade(AmmoTemplate bulletTemplate)
        {
            //Logger.LogDebug("BB: detected HE Grenade");

            //check if bulletTemplate is HE Grenade if has ExplosionStrength and only one projectile
            return (bulletTemplate.ExplosionStrength > 0
                && bulletTemplate.ProjectileCount == 1);
        }

        private static bool isBreachingSlug(AmmoTemplate bulletTemplate)
        {
            return (bulletTemplate._id == "doorbreacher");
        }
        private static bool isShotgun(DamageInfo damageInfo)
        {
            //Logger.LogDebug("BB: detected Shotgun: " + (damageInfo.Weapon as Weapon).WeapClass == "shotgun");

            //check if weapon is a shotgun

            return ((damageInfo.Weapon as Weapon)?.WeapClass == "shotgun");
        }
        private static bool isValidHit(DamageInfo damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>() != null)
            {
                //Logger.LogDebug("BB: doorhandle exists so checking if hit");
                Vector3 localHitPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                DoorHandle doorHandle = col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>();
                Vector3 doorHandleLocalPos = doorHandle.transform.localPosition;
                float distanceToHandle = Vector3.Distance(localHitPoint, doorHandleLocalPos);
                return distanceToHandle < 0.25f;
            }
            //if doorhandle does not exist then it is a valid hit
            else
            {
                //Logger.LogDebug("BB: doorhandle does not exist so valid hit");
                return true;
            }

        }
    }
}
