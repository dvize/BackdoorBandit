using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using UnityEngine;
using DoorBreach;

namespace BackdoorBandit
{
    internal static class DamageUtility
    {
        internal static void CheckWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage, HashSet<string> validWeapons, Func<AmmoTemplate, bool> isRoundValid, Func<DamageInfo, bool> isValidLockHit)
        {
            var material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            var weapon = damageInfo.Weapon.TemplateId;

            // Logger.LogInfo($"BB: Actual DamageType is : {damageInfo.DamageType}");

            //semi-pleb mode.  All regular doors are shootable any weapon except for reinforced doors
            if (DoorBreachPlugin.SemiPlebMode.Value && material != MaterialType.MetalThin && material != MaterialType.MetalThick)
            {
                validDamage = true;
                return;
            }

            //regular valid melee weapon check
            if (damageInfo.DamageType != EDamageType.Bullet && damageInfo.DamageType != EDamageType.GrenadeFragment)
            {
                if (damageInfo.DamageType == EDamageType.Melee && DoorBreachComponent.MeleeWeapons.Contains(weapon) && material != MaterialType.MetalThin && material != MaterialType.MetalThick)
                {
                    validDamage = true;
                }

                return;
            }

            var bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;

            //regular valid shotgun round check
            if (validWeapons.Contains(weapon) && isRoundValid(bulletTemplate) && isValidLockHit(damageInfo))
            {
                // Logger.LogInfo($"BB: Valid round detected. weapon used: {damageInfo.Weapon.LocalizedName()}");
                validDamage = true;

                // Additional modifications or actions for specific cases
                if (isValidLockHit == isValidCarTrunkLockHit)
                {
                    damageInfo.Damage = 500;  //only so it opens the car trunk in one shot
                }

                return;
            }
        }

        internal static void CheckDoorWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.GrenadeLaunchers,
                ammo => isHEGrenade(ammo) || isShrapnel(ammo), isValidDoorLockHit);
        }

        internal static void CheckCarWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.GrenadeLaunchers,
                ammo => isHEGrenade(ammo) || isShrapnel(ammo), isValidCarTrunkLockHit);
        }

        internal static void CheckLootableContainerWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.GrenadeLaunchers,
                ammo => isHEGrenade(ammo) || isShrapnel(ammo), isValidContainerLockHit);
        }


        internal static bool isShrapnel(AmmoTemplate bulletTemplate)
        {
            DoorBreachComponent.Logger.LogDebug("BB: detected grenade shrapnel");

            //check if bulletTemplate is shrapnel and we only want grenade shrapnel not bullet shrapnel
            return (bulletTemplate.FragmentType == "5485a8684bdc2da71d8b4567");
        }

        internal static bool isHEGrenade(AmmoTemplate bulletTemplate)
        {
            //Logger.LogDebug("BB: detected HE Grenade");

            //check if bulletTemplate is HE Grenade if has ExplosionStrength and only one projectile
            return (bulletTemplate.ExplosionStrength > 0
                && bulletTemplate.ProjectileCount == 1);
        }

        internal static bool isBreachingSlug(AmmoTemplate bulletTemplate)
        {
            return (bulletTemplate._id == "doorbreacher");
        }
        internal static bool isShotgun(DamageInfo damageInfo)
        {
            //Logger.LogDebug("BB: detected Shotgun: " + (damageInfo.Weapon as Weapon).WeapClass == "shotgun");

            //check if weapon is a shotgun

            return ((damageInfo.Weapon as Weapon)?.WeapClass == "shotgun");
        }
        internal static bool isValidDoorLockHit(DamageInfo damageInfo)
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

        internal static bool isValidCarTrunkLockHit(DamageInfo damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<Trunk>().GetComponentInChildren<DoorHandle>() != null)
            {
                //Logger.LogDebug("BB: doorhandle exists so checking if hit");
                var gameobj = col.GetComponentInParent<Trunk>().gameObject;

                //find child game object Lock from gameobj
                var carLockObj = gameobj.transform.Find("CarLock_Hand").gameObject;
                var lockObj = carLockObj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
                //Logger.LogError("Distance to lock: " + distanceToLock);
                return distanceToLock < 0.25f;
            }
            //if doorhandle does not exist then it is a valid hit
            else
            {
                //Logger.LogDebug("BB: doorhandle does not exist so valid hit");
                return true;
            }

        }

        internal static bool isValidContainerLockHit(DamageInfo damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<LootableContainer>().GetComponentInChildren<DoorHandle>() != null)
            {
                //Logger.LogDebug("BB: doorhandle exists so checking if hit");
                var gameobj = col.GetComponentInParent<LootableContainer>().gameObject;

                //find child game object Lock from gameobj
                var lockObj = gameobj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
                //Logger.LogError("Distance to lock: " + distanceToLock);
                return distanceToLock < 0.25f;
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
