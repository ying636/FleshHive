using HarmonyLib;
using RimWorld;

namespace FleshHive;

[HarmonyPatch(typeof(PsychicRitualDef), nameof(PsychicRitualDef.Visible), MethodType.Getter)]
public static class Patch_PsychicRitualDef_Visible_FleshHive
{
    [HarmonyPostfix]
    public static void Postfix(PsychicRitualDef __instance, ref bool __result)
    {
        if (__result && __instance is PsychicRitualDef_SummonGiantFleshbeast ritual)
        {
            __result = ritual.requiredDiscovery.Discovered;
        }
    }
}
