using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_FleshCarapaceFloor : HiveBuildingWorker_FleshBlueprint
{
    public override AcceptanceReport CanPlace(IntVec3 loc, Rot4 rot, Map map)
    {
        if (map == null || !loc.InBounds(map))
        {
            return "OutOfBounds".Translate();
        }
        TerrainDef terrain = loc.GetTerrain(map);
        if (terrain != TerrainDefOf.Flesh && !terrain.HasTag(FleshHiveTags.FleshTerrain))
        {
            return "FH_FleshCarapaceFloor_MustPlaceOnFlesh".Translate();
        }

        return base.CanPlace(loc, rot, map);
    }

    public override void DrawGhost(IntVec3 center, Rot4 rot, Map map, Color ghostCol)
    {
        GenDraw.DrawFieldEdges(new List<IntVec3> { center }, ghostCol);
    }
}
