using System;
using System.Linq;
using Aki.Reflection.Patching;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using DoorBreach;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
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

                //maybe check for door material later for more hitpoints.


                //add hitpoints component to door
                var hitpoints = door.gameObject.GetOrAddComponent<Hitpoints>();

                hitpoints.hitpoints = randhitpoints;

/*                // Find the first child transform that has the word "ballistic" in its name and has a BallisticCollider component
                Transform childTransform = door.gameObject.GetComponentsInChildren<Transform>(true)
                    .First(t => t.name.ToLower().Contains("ballistic") && t.GetComponent<BallisticCollider>() != null);
*/

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
            
            //only want to apply damage to doors if the player is the one shooting
            if (player.IsYourPlayer)
            {
                //setup colliders and check if we have the right components
                var collider = damageInfo.HittedBallisticCollider as BallisticCollider;
                bool isDoor = collider.transform.parent?.gameObject?.GetComponent<Door>() != null;
                bool hasHitPoints = collider.transform.parent?.gameObject?.GetComponent<Hitpoints>() != null;

                if (isDoor && hasHitPoints)
                {
                    //Logger.LogInfo($"BackdoorBandit: Applying Hit Damage {damageInfo.Damage} hitpoints");
                    var hitpoints = collider.transform.parent?.gameObject?.GetComponent<Hitpoints>() as Hitpoints;

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

}

    

