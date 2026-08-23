using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class PlaceWorker_RequireFleshTerrain : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        ThingDef thingDef = GetThingDef(checkingDef);
        if (thingDef == null)
        {
            return AcceptanceReport.WasAccepted;
        }
        foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(loc, rot, thingDef.Size))
        {
            if (!FleshTerrainUtility.IsFleshTerrain(map, cell))
            {
                return "FH_MustPlaceOnFleshTerrain".Translate();
            }
        }
        return AcceptanceReport.WasAccepted;
    }

    private static ThingDef GetThingDef(BuildableDef checkingDef)
    {
        if (checkingDef is HiveBuildingDef hiveBuildingDef)
        {
            return hiveBuildingDef.building;
        }
        return checkingDef as ThingDef;
    }
}
