using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.CanRefuel))]
public static class Patch_BoneSpearSpitter_Refuel
{
    public static bool Prefix(Thing t, ref bool __result)
    {
        if (t.def.defName != "FH_BoneSpearSpitter")
        {
            return true;
        }

        __result = false;
        return false;
    }
}
