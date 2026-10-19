using System.Linq;
using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

[HarmonyPatch(typeof(Caravan), nameof(Caravan.AddPawn))]
public static class Patch_Caravan_AddPawn_FleshHive
{
    public static void Prefix(Pawn p, out Map? __state)
    {
        __state = p?.MapHeld;
    }

    public static void Postfix(Caravan __instance, Pawn p, Map? __state)
    {
        if (__instance.Faction != Faction.OfPlayer || p == null || !__instance.ContainsPawn(p))
        {
            return;
        }
        var pawns = __instance.PawnsListForReading;
        var nodeGroups = pawns.Where(HCFGameUtility.IsNodeUnit)
            .SelectMany(HCFGameUtility.GetNodeGroups).Distinct().ToList();
        foreach (Pawn pawn in pawns)
        {
            UnitComp comp = HCFGameUtility.GetUnitComp(pawn);
            if (pawn.Faction != Faction.OfPlayer || comp?.Props.tags.Contains(FleshHiveTags.Flesh) != true || HCFGameUtility.IsNodeUnit(pawn)
                || nodeGroups.Contains(comp.group))
            {
                continue;
            }
            UnitGroup? target = nodeGroups.FirstOrDefault(group => group.CanAccept(pawn).Accepted);
            if (target == null)
            {
                target = comp.group as UnitGroup_TemporaryFleshHive
                    ?? __state?.GetComponent<MapComponent_FleshHive>()?.group
                    ?? pawns.Select(unit => HCFGameUtility.GetUnitComp(unit)?.group)
                        .OfType<UnitGroup_TemporaryFleshHive>().FirstOrDefault();
                if (target == null)
                {
                    target = GameComponent_UnitGroup.Instance.MakeNewGroup(typeof(UnitGroup_TemporaryFleshHive));
                    target.name = "TemporaryFleshGroup".Translate();
                    target.tags.Add(FleshHiveTags.Flesh);
                    target.unitLimit = int.MaxValue;
                }
            }
            if (target != comp.group)
            {
                Lord? lord = target.lord;
                target.lord = null;
                try
                {
                    target.AcceptUnit(pawn);
                }
                finally
                {
                    target.lord = lord;
                }
            }
        }
    }
}
