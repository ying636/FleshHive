using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_FleshPlantBlueprint : HiveBuildingWorker_Plant
{
    public override void Place(IntVec3 loc, Rot4 rot, Map map)
    {
        if (DebugSettings.godMode)
        {
            base.Place(loc, rot, map);
            return;
        }

        AcceptanceReport report = CanPlace(loc, rot, map);
        if (!report.Accepted)
        {
            Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
            return;
        }

        Blueprint_HiveBuild blueprint = FleshBlueprintUtility.MakeBlueprint(def);
        GenSpawn.Spawn(blueprint, loc, map, rot);
        if (def.placeWorkers.NullOrEmpty())
        {
            return;
        }

        foreach (PlaceWorker placeWorker in def.placeWorkers)
        {
            placeWorker.PostPlace(map, def, loc, rot);
        }
    }
}
