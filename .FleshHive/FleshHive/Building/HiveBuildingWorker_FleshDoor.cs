using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_FleshDoor : HiveBuildingWorker_FleshBlueprint
{
    public override AcceptanceReport CanPlace(IntVec3 loc, Rot4 rot, Map map)
    {
        if (map == null || def.Buildable == null)
        {
            return "HCF_InvalidHiveBuilding".Translate(def.defName);
        }

        foreach (ThingDef needBuilding in def.needBuildings)
        {
            if (!map.listerThings.ThingsOfDef(needBuilding).Exists(thing => thing.Faction == Faction.OfPlayer))
            {
                return "HCF_NoBuilding".Translate(needBuilding.label);
            }
        }

        Thing? wallToReplace = GetWallToReplace(loc, map);
        AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(
            def.Buildable,
            loc,
            rot,
            map,
            DebugSettings.godMode,
            wallToReplace);
        if (!report.Accepted)
        {
            return report;
        }

        if (!def.placeWorkers.NullOrEmpty())
        {
            foreach (PlaceWorker placeWorker in def.placeWorkers)
            {
                report = placeWorker.AllowsPlacing(def, loc, rot, map);
                if (!report.Accepted)
                {
                    return report;
                }
            }
        }

        return AcceptanceReport.WasAccepted;
    }

    public override void Place(IntVec3 loc, Rot4 rot, Map map)
    {
        AcceptanceReport report = CanPlace(loc, rot, map);
        if (!report.Accepted)
        {
            Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (GetWallToReplace(loc, map) is { } wallToReplace)
        {
            GenSpawn.WipeExistingThings(loc, rot, def.Buildable, map, DestroyMode.Deconstruct);
        }

        base.Place(loc, rot, map);
    }

    private Thing? GetWallToReplace(IntVec3 loc, Map map)
    {
        if (!loc.InBounds(map))
        {
            return null;
        }

        return loc.GetThingList(map).FirstOrDefault(thing =>
            thing.def.IsWall && thing.def.building.isPlaceOverableWall);
    }
}
