using System.Reflection;
using HarmonyLib;
using Verse;

namespace FleshHive.CE;

[HarmonyPatch]
internal static class Patch_ScarletFieldPawnRegistry
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.RegisterPawn));
        yield return AccessTools.Method(typeof(MapPawns), nameof(MapPawns.DeRegisterPawn));
    }

    private static void Postfix(MapPawns __instance, Pawn p)
    {
        ScarletFieldMapIndex.Refresh(__instance, p);
    }
}
