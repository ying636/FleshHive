using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_FleshFloor : HiveBuildingWorker_FleshBlueprint
{
    public override AcceptanceReport CanPlace(IntVec3 loc, Rot4 rot, Map map)
    {
        if (map == null || !loc.InBounds(map))
        {
            return "OutOfBounds".Translate();
        }
        if (FleshTerrainUtility.IsFleshTerrain(map, loc))
        {
            return "FH_FleshFloor_AlreadyFlesh".Translate();
        }
        if (!GenConstruct.CanBuildOnTerrain(TerrainDefOf.Flesh, loc, map, Rot4.North))
        {
            return "FH_FleshFloor_UnsupportedTerrain".Translate();
        }
        if (!HasFleshConnection(map, loc))
        {
            return "FH_FleshFloor_MustConnect".Translate();
        }

        AcceptanceReport report = base.CanPlace(loc, rot, map);
        if (!report.Accepted)
        {
            return report;
        }

        return AcceptanceReport.WasAccepted;
    }

    public override void DrawGhost(IntVec3 center, Rot4 rot, Map map, Color ghostCol)
    {
        GenDraw.DrawFieldEdges(new List<IntVec3> { center }, ghostCol);
    }

    private bool HasFleshConnection(Map map, IntVec3 loc)
    {
        foreach (IntVec3 offset in GenAdj.CardinalDirections)
        {
            IntVec3 adjacent = loc + offset;
            if (FleshTerrainUtility.IsFleshTerrain(map, adjacent))
            {
                return true;
            }
            if (adjacent.InBounds(map) && adjacent.GetThingList(map)
                    .OfType<Blueprint_HiveBuild>()
                    .Any(blueprint => blueprint.buildingDef == def))
            {
                return true;
            }
        }

        return false;
    }
}
