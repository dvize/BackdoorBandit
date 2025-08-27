using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;

namespace BackdoorBandit.Patches
{
    internal class ActionMenuKeyCardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_13));


        // Check if an action is already added. Hopefully door's action takes precedence
        public static bool IsActionAdded(List<ActionsTypesClass> actions, string actionName)
        {
            return actions.Any(action => action.Name.ToLower() == actionName.ToLower());
        }

        [PatchPostfix]
        public static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, Door door, bool isProxy)
        {
            if (__result == null || __result.Actions == null || IsActionAdded(__result.Actions, "Plant Explosive"))
            {
                return;
            }

            // Add new action after existing actions
            __result.Actions.Add(new ActionsTypesClass
            {
                Name = "Plant Explosive",
                Action = new Action(() =>
                {
                    BackdoorBandit.ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);
                }),
                Disabled = (!door.IsBreachAngle(owner.Player.Position) || !BackdoorBandit.ExplosiveBreachComponent.IsValidDoorState(door) ||
                            !BackdoorBandit.ExplosiveBreachComponent.hasC4Explosives(owner.Player))
            });
        }
    }
}
