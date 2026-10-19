using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class Alert_UngroupedFleshbeasts : Alert
{
    public Alert_UngroupedFleshbeasts()
    {
        defaultLabel = "FH_Alert_UngroupedFleshbeasts".Translate();
    }

    public override TaggedString GetExplanation()
    {
        return string.Join("\n", culprits.Select(pawn => pawn.LabelShortCap));
    }

    public override AlertReport GetReport()
    {
        culprits.Clear();
        foreach (Pawn pawn in Find.Maps.SelectMany(map => map.mapPawns.AllPawnsSpawned)
                     .Concat(GameComponent_UnitGroup.Instance?.groups
                         .OfType<UnitGroup_TemporaryFleshHive>().SelectMany(group => group.units)
                         ?? Enumerable.Empty<Pawn>()).Distinct())
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Faction != Faction.OfPlayer
                || HCFGameUtility.IsNodeUnit(pawn))
            {
                continue;
            }
            UnitComp comp = HCFGameUtility.GetUnitComp(pawn);
            if (comp?.Props.tags.Contains(FleshHiveTags.Flesh) == true
                && (comp.group == null || comp.group is UnitGroup_TemporaryFleshHive))
            {
                culprits.Add(pawn);
            }
        }
        return AlertReport.CulpritsAre(culprits);
    }

    private readonly List<Pawn> culprits = new();
}
