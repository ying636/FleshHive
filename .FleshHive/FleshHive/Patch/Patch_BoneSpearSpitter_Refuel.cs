using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(RefuelWorkGiverUtility), nameof(RefuelWorkGiverUtility.CanRefuel))]
public static class Patch_BoneSpearSpitter_Refuel
{
    public static bool Prefix(Thing t, ref bool __result)
    {
        if (t is not Building_BoneSpearSpitter)
        {
            return true;
        }

        __result = false;
        return false;
    }
}
