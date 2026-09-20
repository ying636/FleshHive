using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch]
public static class Patch_Designator_Cancel_FleshBlueprint
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.CanDesignateThing))]
    public static void CanDesignateThingPostfix(Thing t, ref AcceptanceReport __result)
    {
        if (t is Blueprint_FleshBuild)
        {
            __result = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.DesignateThing))]
    public static bool DesignateThingPrefix(Thing t)
    {
        if (t is not Blueprint_FleshBuild blueprint)
        {
            return true;
        }

        blueprint.Cancel();
        return false;
    }
}
