using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using UnityEngine;

namespace BackdoorBandit.Patches
{
    internal class PerfectCullingNullRefPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup).GetMethod(nameof(Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup.method_1));

        [PatchPrefix]

        public static bool Prefix(Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup __instance)
        {
            // Implement checks for null references here
            if (__instance == null || __instance.bakeGroups == null)
            {
                return false; 
            }

            // Check each bake group for null or any other conditions that might lead to an exception
            foreach (var group in __instance.bakeGroups)
            {
                if (group == null || group.renderers == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
