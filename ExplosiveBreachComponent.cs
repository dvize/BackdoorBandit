using System;
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
using UnityEngine.Networking;

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

        internal static AudioClip beepClip;
        internal static AudioClip finalToneClip; 
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

            // Preload Audio Clips
            StartCoroutine(LoadAudioClip(BepInEx.Paths.PluginPath + "/dvize.BackdoorBandit/Beep.mp3", true));
            StartCoroutine(LoadAudioClip(BepInEx.Paths.PluginPath + "/dvize.BackdoorBandit/FinalBeepTone.mp3", false));
        }

        private IEnumerator LoadAudioClip(string filePath, bool isBeepClip)
        {
            string uri = "file:///" + filePath;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
            {
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Logger.LogError($"Error loading audio clip: {uwr.error}");
                }
                else
                {
                    if (isBeepClip)
                        beepClip = DownloadHandlerAudioClip.GetContent(uwr);
                    else
                        finalToneClip = DownloadHandlerAudioClip.GetContent(uwr);

                    Logger.LogInfo($"Audio Clip loaded successfully: {filePath}");
                }
            }
        }
        internal static bool hasC4Explosives(Player player)
        {
            // Search playerItems for first c4 explosive
            Item foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == C4ExplosiveId);

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
            if (door == null || player == null)
            {
                Logger.LogError("Either the door or Player is null. Can't start breach.");
                return;
            }

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
                C4Instance latestC4Instance = c4Instances.Last();
                StartDelayedExplosionCoroutine(door, player, componentInstance, latestC4Instance);
            }
        }
        private static void TryPlaceC4OnDoor(Door door, Player player)
        {
            ItemFactoryClass itemFactory = Singleton<ItemFactoryClass>.Instance;
            Item c4Item = itemFactory.CreateItem(MongoID.Generate(), C4ExplosiveId, null);

            Transform lockTransform = door.transform.Find("Lock");
            Transform doorHandleTransform = null;
            try
            {
                // Attempt to safely access door.Handle
                doorHandleTransform = door.Handle?.transform;
            }
            catch (Exception ex)
            {
                //Logger.LogError($"Failed to access door handle: {ex.Message}");
            }

            Component lockComponent = door.gameObject.GetComponent("Lock");

            // Determine the target transform based on availability of components
            Transform targetTransform = null;
            if (lockTransform != null)
            {
                targetTransform = lockTransform;
            }
            else if (doorHandleTransform != null)
            {
                targetTransform = doorHandleTransform;
            }
            else if (lockComponent != null && lockComponent.transform != null)
            {
                targetTransform = lockComponent.transform;
            }
            else
            {
                // If no lock component or door handle, default to the door's center position
#if DEBUG
        Logger.LogInfo("Lock component and Door Handle not found. Defaulting to door center");
#endif
                targetTransform = door.transform;
            }

            if (targetTransform == null)
            {
                Logger.LogError("Unable to find a suitable position on the door for C4 placement.");
                return;
            }

            Vector3 targetPosition = targetTransform.position;
            Vector3 playerPosition = player.Transform.position;

            // Calculate the vector from the door (lock or handle position) towards the player
            Vector3 doorToPlayer = playerPosition - targetPosition;
            doorToPlayer.y = 0;

            Vector3 doorForward = doorToPlayer.normalized;
            float doorThickness = 0.07f; // Modify thickness if needed
            Vector3 c4Position = targetPosition + doorForward * doorThickness; // Placing it slightly forward

            Quaternion rotation = Quaternion.LookRotation(doorForward, Vector3.up);
            Quaternion correctionRotation = Quaternion.Euler(90, 0, 0);
            rotation *= correctionRotation;

            // Place the C4 item in the game world
            LootItem lootItem = gameWorld.SetupItem(c4Item, player.InteractablePlayer, c4Position, rotation);
            c4Instances.Add(new C4Instance(lootItem, c4Position));
        }


        private static void RemoveItemFromPlayerInventory(Player player)
        {
            Item foundItem = player.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(x => x.TemplateId == C4ExplosiveId);
            if (foundItem == null) return;

            TraderControllerClass traderController = (TraderControllerClass)foundItem.Parent.GetOwner();
            GStruct455<GClass3200> discardResult = InteractionsHandlerClass.Discard(foundItem, traderController, false);

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
            if (c4Instance?.LootItem == null)
            {
                Logger.LogError("C4 instance or LootItem is null.");
                return;
            }

            c4Instance.ExplosionCoroutine = monoBehaviour.StartCoroutine(DelayedExplosion(door, player, c4Instance));
        }
        private static void StopExplosionCoroutine(C4Instance c4Instance)
        {
            if (componentInstance != null && c4Instance?.ExplosionCoroutine != null)
            {
                componentInstance.StopCoroutine(c4Instance.ExplosionCoroutine);
                c4Instance.ExplosionCoroutine = null;
                //Logger.LogInfo("Coroutine stopped successfully.");
            }
            else
            {
                Logger.LogError("Failed to stop coroutine: component or coroutine reference is null.");
            }
        }

        private static IEnumerator DelayedExplosion(Door door, Player player, C4Instance c4Instance)
        {
            float waitTime = DoorBreachPlugin.explosiveTimerInSec.Value;
            float timer = 0;
            float normalBeepInterval = 1.0f;  // Interval for normal beeping
            float rapidBeepStart = 5.0f;      // Time to start transitioning to rapid beeps
            float finalRapidBeepInterval = 0.3f;  // Interval for final rapid beeps
            float finalToneStart = 1.0f;      // Time to start final continuous tone

            AudioSource audioSource = c4Instance.LootItem.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = c4Instance.LootItem.gameObject.AddComponent<AudioSource>();
                audioSource.volume = 0.8f; //1.0 is max
                audioSource.spatialBlend = 1.0f; //3d sound
            }

            float currentBeepInterval = normalBeepInterval;

            while (timer < waitTime)
            {
                // Check for C4 actual in world still
                if (c4Instance == null || c4Instance.LootItem == null || c4Instance.LootItem.Item == null || !ExistsInGame(c4Instance.LootItem.Item.Id))
                {
                    StopExplosionCoroutine(c4Instance);
                    yield break;
                }

                // Only switch the clip if it's not already set correctly
                AudioClip intendedClip = (waitTime - timer <= finalToneStart) ? finalToneClip : beepClip;
                if (audioSource.clip != intendedClip || !audioSource.isPlaying)
                {
                    audioSource.clip = intendedClip;
                    audioSource.Play();
                }

                // Calculate beep interval dynamically
                if (waitTime - timer <= rapidBeepStart && waitTime - timer > finalToneStart)
                {
                    float lerpFactor = (waitTime - timer - finalToneStart) / (rapidBeepStart - finalToneStart);
                    currentBeepInterval = Mathf.Lerp(finalRapidBeepInterval, normalBeepInterval, lerpFactor);
                }
                else if (waitTime - timer > rapidBeepStart)
                {
                    currentBeepInterval = normalBeepInterval;
                }
                else
                {
                    currentBeepInterval = finalRapidBeepInterval;
                }

                float timeToNextBeep = Mathf.Min(currentBeepInterval, waitTime - timer);
                yield return new WaitForSeconds(timeToNextBeep);
                timer += timeToNextBeep;
            }

            // Trigger explosion effects, damage calculation, and cleanup
            if (c4Instance.LootItem != null && c4Instance.LootItem.gameObject != null)
            {
                TriggerExplosion(door, player, c4Instance);
            }
        }

        private static void TriggerExplosion(Door door, Player player, C4Instance c4Instance)
        {
            if (c4Instance.LootItem != null && c4Instance.LootItem.gameObject != null)
            {
                effectsInstance.EmitGrenade("big_explosion", c4Instance.LootItem.transform.position, Vector3.forward, DoorBreachPlugin.explosionRadius.Value);

                if (DoorBreachPlugin.explosionDoesDamage.Value)
                {
                    float explosionRadius = DoorBreachPlugin.explosionRadius.Value;
                    float baseDamage = DoorBreachPlugin.explosionDamage.Value;
                    Vector3 explosionPosition = c4Instance.LootItem.transform.position;

                    Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
                    foreach (Collider hitCollider in hitColliders)
                    {
                        Player tempplayer = hitCollider.GetComponentInParent<Player>();
                        if (tempplayer != null)
                        {
                            float distance = Vector3.Distance(hitCollider.transform.position, explosionPosition);
                            if (CheckLineOfSight(explosionPosition, hitCollider.transform.position))
                            {
                                float damageMultiplier = Mathf.Clamp01(1 - distance / explosionRadius);
                                float damageAmount = baseDamage * damageMultiplier;

                                DamageInfoStruct damageInfo = new DamageInfoStruct
                                {
                                    DamageType = EDamageType.Explosion,
                                    Damage = damageAmount,
                                    Direction = (tempplayer.Transform.position - explosionPosition).normalized,
                                    HitPoint = tempplayer.Transform.position,
                                    HitNormal = -(tempplayer.Transform.position - explosionPosition).normalized,
                                    Player = null,
                                    Weapon = null,
                                    ArmorDamage = damageAmount * 0.5f,
                                };

                                tempplayer.ApplyDamageInfo(damageInfo, EBodyPart.Chest, EBodyPartColliderType.Pelvis, 0f);
                            }
                        }
                    }
                }

                // Check if c4Instance and its LootItem exist
                if (c4Instance != null && c4Instance.LootItem != null)
                {
                    // Safe to destroy the C4 since it's confirmed to exist
                    UnityEngine.Object.Destroy(c4Instance.LootItem.gameObject);
                }

                // Check if door exists and is not already destroyed
                if (door != null && door.gameObject != null)
                {
                    ApplyHit.OpenDoorIfNotAlreadyOpen(door, player, EInteractionType.Breach);

                    //delete door and need to check if their is related glass
                    UnityEngine.Object.Destroy(door.gameObject);
                }

                // Clean up references
                if (c4Instances.Contains(c4Instance))
                {
                    c4Instances.Remove(c4Instance);
                }
            }
        }

        private static bool ExistsInGame(string id)
        {
            return gameWorld.FindItemById(id).Value != null;
        }

        private static bool CheckLineOfSight(Vector3 explosionPosition, Vector3 playerPosition)
        {
            RaycastHit hit;
            Vector3 direction = playerPosition - explosionPosition;
            if (Physics.Raycast(explosionPosition, direction.normalized, out hit, direction.magnitude))
            {
                return hit.collider.GetComponent<Player>() != null;
            }
            return false;
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<ExplosiveBreachComponent>();
            }

        }


    }

    
}

