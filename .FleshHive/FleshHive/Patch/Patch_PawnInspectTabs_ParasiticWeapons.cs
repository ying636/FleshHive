using HarmonyLib;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(Thing), nameof(Thing.GetInspectTabs))]
public static class Patch_PawnInspectTabs_ParasiticWeapons
{
    public static IEnumerable<InspectTabBase> Postfix(IEnumerable<InspectTabBase> __result, Thing __instance)
    {
        if (__result != null)
        {
            foreach (InspectTabBase tab in __result)
            {
                yield return tab;
            }
        }

        if (__instance is Pawn pawn && HediffComp_ParasitismWeaponMounts.GetFirst(pawn) != null)
        {
            yield return InspectTabManager.GetSharedInstance(typeof(ITab_ParasiticWeapons));
        }
    }
}
