using System;
using System.Reflection;
using SPT.Reflection.Patching;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable CS0169 // The field is never used

namespace BackdoorBandit
{

    internal class ApplyHit : ModulePatch
    {
        private static BallisticCollider collider;
        private static bool isDoor;
        private static bool isCarTrunk;
        private static bool isLootableContainer;
        private static bool hasHitPoints;
        private static bool validDamage;
        private static Hitpoints hitpoints;
        private static Door door;
        private static Trunk carTrunk;
        private static LootableContainer lootContainer;
        protected override MethodBase GetTargetMethod() => typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


        [PatchPostfix]
        public static void PatchPostFix(DamageInfoStruct damageInfo, ShotIdStruct shotID)
        {
            //try catch for random things applying damage that we don't want
            try
            {
                if (ShouldApplyDamage(damageInfo))
                {
                    HandleDamageForEntity(damageInfo, damageInfo.HittedBallisticCollider as BallisticCollider);
                }
            }
            catch { }
        }

        private static bool ShouldApplyDamage(DamageInfoStruct damageInfo)
        {
            return damageInfo.Player != null
                && damageInfo.Player.iPlayer.IsYourPlayer
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp
                && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window
                && damageInfo.DamageType != EDamageType.Explosion;
        }

        private static void HandleDamageForEntity(DamageInfoStruct damageInfo, BallisticCollider collider)
        {
            bool isCarTrunk = false;
            bool isLootableContainer = false;
            bool isDoor = false;
            bool hasHitPoints = false;
            bool validDamage = DoorBreachPlugin.PlebMode.Value || false;

            if (collider != null)
            {
                isCarTrunk = collider.GetComponentInParent<Trunk>() != null;
                isLootableContainer = collider.GetComponentInParent<LootableContainer>() != null;
                isDoor = collider.GetComponentInParent<Door>() != null;
                hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;
            }

            if (isCarTrunk && hasHitPoints)
            {
                HandleCarTrunkDamage(damageInfo, collider, ref validDamage);
            }

            if (isLootableContainer && hasHitPoints)
            {
                HandleLootableContainerDamage(damageInfo, collider, ref validDamage);
            }

            if (isDoor && hasHitPoints)
            {
                HandleDoorDamage(damageInfo, collider, ref validDamage);
            }
        }

        #region DamageApplication
        private static void HandleCarTrunkDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value && DoorBreachPlugin.OpenCarDoors.Value)
            {
                DamageUtility.CheckCarWeaponAndAmmo(damageInfo, ref validDamage);
            }

            HandleDamage(damageInfo, collider, ref validDamage, "Car Trunk", (hitpoints, entity) =>
            {
                if (hitpoints.hitpoints <= 0)
                {
                    Trunk carTrunk = entity.GetComponentInParent<Trunk>();
                    OpenDoorIfNotAlreadyOpen(carTrunk, damageInfo.Player.AIData.Player, EInteractionType.Open);
                }
            });
        }

        private static void HandleLootableContainerDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value && DoorBreachPlugin.OpenLootableContainers.Value)
            {
                DamageUtility.CheckLootableContainerWeaponAndAmmo(damageInfo, ref validDamage);
            }

            HandleDamage(damageInfo, collider, ref validDamage, "Lootable Container", (hitpoints, entity) =>
            {
                if (hitpoints.hitpoints <= 0)
                {
                    LootableContainer lootContainer = entity.GetComponentInParent<LootableContainer>();
                    OpenDoorIfNotAlreadyOpen(lootContainer, damageInfo.Player.AIData.Player, EInteractionType.Open);
                }
            });
        }

        internal static void HandleDoorDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage)
        {
            if (!DoorBreachPlugin.PlebMode.Value)
            {
                DamageUtility.CheckDoorWeaponAndAmmo(damageInfo, ref validDamage);
            }

            HandleDamage(damageInfo, collider, ref validDamage, "Door", (hitpoints, entity) =>
            {
                WorldInteractiveObject door = entity.GetComponentInParent<WorldInteractiveObject>();

#if DEBUG
                DoorBreachComponent.Logger.LogDebug("[Door info]");
                DoorBreachComponent.Logger.LogDebug($"KeyId: {door.KeyId}");
                DoorBreachComponent.Logger.LogDebug($"DoorState: {door.DoorState}");
                DoorBreachComponent.Logger.LogDebug($"InitialDoorState: {door.InitialDoorState}");
#endif

                if (hitpoints.hitpoints <= 0)
                {
                    OpenDoorIfNotAlreadyOpen(door, damageInfo.Player.AIData.Player, EInteractionType.Breach);
                }
            });
        }

        internal static void HandleDamage(DamageInfoStruct damageInfo, BallisticCollider collider, ref bool validDamage, string entityName, Action<Hitpoints, GameObject> onHitpointsZero)
        {
            Hitpoints hitpoints = collider.GetComponentInParent<Hitpoints>() as Hitpoints;

            if (validDamage)
            {
                Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints to {entityName}");
                hitpoints.hitpoints -= damageInfo.Damage;

                onHitpointsZero?.Invoke(hitpoints, collider.gameObject);
            }
        }
        internal static void OpenDoorIfNotAlreadyOpen<T>(T entity, Player player, EInteractionType interactionType) where T : class
        {
            if (entity is Door door)
            {
                if (door.DoorState != EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    door.interactWithoutAnimation = true;

                    // Run the ExecuteDoorInteraction
                    player.CurrentManagedState.ExecuteDoorInteraction(door, new InteractionResult(interactionType), null, player);
                    door.interactWithoutAnimation = false;
                }
            }

            if (entity is LootableContainer container)
            {
                if (container.DoorState != EDoorState.Open)
                {
                    container.DoorState = EDoorState.Shut;
                    container.interactWithoutAnimation = true;
                    player.CurrentManagedState.ExecuteDoorInteraction(container, new InteractionResult(interactionType), null, player);
                    container.interactWithoutAnimation = false;
                }
            }
            if (entity is Trunk trunk)
            {

                if (trunk.DoorState != EDoorState.Open)
                {
                    trunk.interactWithoutAnimation = true;
                    trunk.DoorState = EDoorState.Shut;
                    player.CurrentManagedState.ExecuteDoorInteraction(trunk, new InteractionResult(interactionType), null, player);
                    trunk.interactWithoutAnimation = false;
                }
            }
        }


#endregion



    }

}
