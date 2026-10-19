using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(UnitGroup), nameof(UnitGroup.Draw))]
public static class Patch_UnitGroup_Draw_FleshHive
{
    [HarmonyPrefix]
    public static void Prefix(Rect wholeRect, out Rect __state)
    {
        __state = wholeRect;
    }

    [HarmonyPostfix]
    public static void Postfix(UnitGroup __instance, Rect __state)
    {
        if (__instance is not UnitGroup_FleshHive group || group is UnitGroup_TemporaryFleshHive
            || group.hive != null && group.hive.Faction != Faction.OfPlayer)
        {
            return;
        }
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && group.CanUngroupAll;
        try
        {
            if (Widgets.ButtonText(new Rect(__state.x + 95f, __state.y + 30f, 100f, 25f), "FH_Group_UngroupAll".Translate()))
            {
                group.UngroupAll();
            }
        }
        finally
        {
            GUI.enabled = enabled;
        }
    }
}
