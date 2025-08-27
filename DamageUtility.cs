using System;
using System.Collections.Generic;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace BackdoorBandit
{
    internal static class DamageUtility
    {
        internal static void CheckWeaponAndAmmo(DamageInfoStruct damageInfo, ref bool validDamage, ref HashSet<string> validWeapons, Func<AmmoTemplate, bool> isRoundValid, Func<DamageInfoStruct, bool> isValidLockHit)
        {
            MaterialType material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            MongoID weaponID = damageInfo.Weapon.TemplateId;

            //semi-pleb mode.  All regular doors are shootable any weapon except for reinforced doors
            if (DoorBreachPlugin.SemiPlebMode.Value && material != MaterialType.MetalThin && material != MaterialType.MetalThick)
            {
                validDamage = true;
                return;
            }

            //regular valid melee weapon check
            if (damageInfo.DamageType != EDamageType.Bullet && damageInfo.DamageType != EDamageType.GrenadeFragment)
            {
                if (damageInfo.DamageType == EDamageType.Melee && DoorBreachComponent.MeleeWeapons.Contains(weaponID) && material != MaterialType.MetalThin && material != MaterialType.MetalThick)
                {
                    validDamage = true;
                }

                return;
            }

            AmmoTemplate bulletTemplate = Singleton<ItemFactoryClass>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;

#if DEBUG
            DoorBreachComponent.Logger.LogDebug($"ammoTemplate: {bulletTemplate.Name}");
            DoorBreachComponent.Logger.LogDebug($"BB: Actual DamageType is : {damageInfo.DamageType}");
            DoorBreachComponent.Logger.LogDebug($"isValidLockHit: {isValidLockHit(damageInfo)}");
            DoorBreachComponent.Logger.LogDebug($"isRoundValid: {isRoundValid(bulletTemplate)}");
            DoorBreachComponent.Logger.LogDebug($"weapon used: {damageInfo.Weapon.LocalizedName()}, id: {damageInfo.Weapon.TemplateId}");
#endif

            //check if weapon is a shotgun and material type is metal
            if (!DoorBreachPlugin.BreachingRoundsOpenMetalDoors.Value)
            {
                if (isBreachingSlug(bulletTemplate) && (material == MaterialType.MetalThin || material == MaterialType.MetalThick))
                {
                    validDamage = false;
                    return;
                }
            }

            //check if its on the validWeapons hashset and its not a shotgun.. something user added then we need to skip the isRoundValidCheck
            if (validWeapons.Contains(weaponID) && !isShotgun(damageInfo) && isValidLockHit(damageInfo))
            {
                validDamage = true;
                return;
            }
            //regular valid weapon and round check
            else if (validWeapons.Contains(weaponID) && isRoundValid(bulletTemplate) && isValidLockHit(damageInfo))
            {
#if DEBUG
                DoorBreachComponent.Logger.LogDebug($"BB: Valid round detected.");
#endif
                validDamage = true;

                // Additional modifications or actions for specific cases
                if (isValidLockHit == isValidCarTrunkLockHit)
                {
                    damageInfo.Damage = 500;  //only so it opens the car trunk in one shot
                }

                return;
            }
        }

        internal static void CheckDoorWeaponAndAmmo(DamageInfoStruct damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, ref DoorBreachComponent.ApplicableWeapons,
               ammo => isHEGrenade(ammo) || isShrapnel(ammo) || isBreachingSlug(ammo), isValidDoorLockHit);
        }

        internal static void CheckCarWeaponAndAmmo(DamageInfoStruct damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, ref DoorBreachComponent.ApplicableWeapons,
                ammo => isHEGrenade(ammo) || isShrapnel(ammo) || isBreachingSlug(ammo), isValidCarTrunkLockHit);
        }

        internal static void CheckLootableContainerWeaponAndAmmo(DamageInfoStruct damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, ref DoorBreachComponent.ApplicableWeapons,
                ammo => isHEGrenade(ammo) || isShrapnel(ammo) || isBreachingSlug(ammo), isValidContainerLockHit);
        }

        internal static bool isShrapnel(AmmoTemplate bulletTemplate)
        {
            //check if bulletTemplate is shrapnel and we only want grenade shrapnel not bullet shrapnel
            //bulletTemplate._id = "5b44e3f4d4351e003562b3f4";
            return (bulletTemplate.FragmentType == "5485a8684bdc2da71d8b4567");
        }

        internal static bool isHEGrenade(AmmoTemplate bulletTemplate)
        {
            //check if bulletTemplate is HE Grenade if has ExplosionStrength and only one projectile
            return (bulletTemplate.ExplosionStrength > 0
                && bulletTemplate.ProjectileCount == 1);
        }

        internal static bool isBreachingSlug(AmmoTemplate bulletTemplate)
        {
            //doorbreach id: 660249a0712c1005a4a3ab41

            return (bulletTemplate._id == "660249a0712c1005a4a3ab41");
        }
        internal static bool isShotgun(DamageInfoStruct damageInfo)
        {
            //check if weapon is a shotgun

            return ((damageInfo.Weapon as Weapon)?.WeapClass == "shotgun");
        }
        internal static bool isValidDoorLockHit(DamageInfoStruct damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>() != null)
            {
                Vector3 localHitPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                DoorHandle doorHandle = col.GetComponentInParent<Door>().GetComponentInChildren<DoorHandle>();
                Vector3 doorHandleLocalPos = doorHandle.transform.localPosition;
                float distanceToHandle = Vector3.Distance(localHitPoint, doorHandleLocalPos);
                return distanceToHandle < 0.25f;
            }
            //if doorhandle does not exist then it is a valid hit
            else
            {
                return true;
            }

        }

        internal static bool isValidCarTrunkLockHit(DamageInfoStruct damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<Trunk>().GetComponentInChildren<DoorHandle>() != null)
            {
                GameObject gameobj = col.GetComponentInParent<Trunk>().gameObject;

                //find child game object Lock from gameobj
                GameObject carLockObj = gameobj.transform.Find("CarLock_Hand").gameObject;
                GameObject lockObj = carLockObj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);

                return distanceToLock < 0.25f;
            }
            //if doorhandle does not exist then it is a valid hit
            else
            {
                return true;
            }

        }

        internal static bool isValidContainerLockHit(DamageInfoStruct damageInfo)
        {
            //check if door handle area was hit
            Collider col = damageInfo.HitCollider;

            //if doorhandle exists and is hit
            if (col.GetComponentInParent<LootableContainer>().GetComponentInChildren<DoorHandle>() != null)
            {
                GameObject gameobj = col.GetComponentInParent<LootableContainer>().gameObject;

                //find child game object Lock from gameobj
                GameObject lockObj = gameobj.transform.Find("Lock").gameObject;

                float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
                return distanceToLock < 0.25f;
            }
            //if doorhandle does not exist then it is a valid hit
            else
            {

                return true;
            }

        }

    }
}
