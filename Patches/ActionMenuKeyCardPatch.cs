using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;

namespace BackdoorBandit.Patches
{
    internal class ActionMenuKeyCardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_9));


        [PatchPostfix]
        public static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, Door door)
        {
            if (__result != null && __result.Actions != null)
            {
                __result.Actions.Add(new ActionsTypesClass
                {
                    Name = "Plant Explosive",
                    Action = new Action(() =>
                    {
                        BackdoorBandit.ExplosiveBreachComponent.StartExplosiveBreach(door, owner.Player);

                    }),
                    Disabled = (!door.IsBreachAngle(owner.Player.Position) || !BackdoorBandit.ExplosiveBreachComponent.IsValidDoorState(door) ||
                        !BackdoorBandit.ExplosiveBreachComponent.hasTNTExplosives(owner.Player))
                });
            }
        }
    }
}
