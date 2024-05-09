using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using UnityEngine;

namespace BackdoorBandit.Patches
{
    internal class PerfectCullingNullRefPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PerfectCullingBakeGroup).GetMethod(nameof(PerfectCullingBakeGroup.Toggle));

        [PatchPrefix]

        static bool Prefix(PerfectCullingBakeGroup __instance, bool rendererEnabled, int ___int_0, PerfectCullingBakeGroup.RuntimeGroupContent[] ___runtimeGroupContent_0)
        {
            // Safely handle Renderer[] array
            if (__instance.runtimeProxies != null)
            {
                foreach (var renderer in __instance.runtimeProxies)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = !rendererEnabled;
                    }
                    else
                    {
                        Debug.LogWarning("Skipped null Renderer in runtimeProxies.");
                    }
                }
            }

            // Safely handle CullingObject array
            if (__instance.cullingLightObjects != null)
            {
                foreach (var cullingObject in __instance.cullingLightObjects)
                {
                    if (cullingObject != null)
                    {
                        cullingObject.SetAutocullVisibility(rendererEnabled);
                    }
                    else
                    {
                        Debug.LogWarning("Skipped null CullingObject in cullingLightObjects.");
                    }
                }
            }

            // Safely handle AnalyticSource array
            if (__instance.analyticSources != null)
            {
                foreach (var analyticSource in __instance.analyticSources)
                {
                    if (analyticSource != null)
                    {
                        analyticSource.IsAutocullVisible = rendererEnabled;
                    }
                    else
                    {
                        Debug.LogWarning("Skipped null AnalyticSource in analyticSources.");
                    }
                }
            }

            // Safely handle ScreenDistanceSwitcher
            if (__instance.screenDistanceSwitcher != null)
            {
                __instance.screenDistanceSwitcher.IsBakedAutocullVisible = rendererEnabled;
            }
            else
            {
                Debug.LogWarning("screenDistanceSwitcher is null.");
            }

            // Safely handle RuntimeGroupContent
            for (int j = 0; j < ___int_0; j++)
            {
                if (___runtimeGroupContent_0[j].Renderer != null)
                {
                    ___runtimeGroupContent_0[j].Renderer.enabled = rendererEnabled;
                }
                else
                {
                    Debug.LogWarning($"Skipped null element at index {j} in runtimeGroupContent_0.");
                }
            }

            return false; // Skip original method execution
        }
    }
}
