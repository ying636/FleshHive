using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(Frame), nameof(Frame.FailConstruction))]
public static class Patch_Frame_FleshHiveConstruction
{
    [HarmonyPrefix]
    public static bool Prefix(Frame __instance)
    {
        return __instance.BuildDef != FleshHiveDefOf.FH_FleshHive;
    }
}
