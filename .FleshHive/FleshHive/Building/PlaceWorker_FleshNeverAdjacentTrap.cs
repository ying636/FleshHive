using RimWorld;
using Verse;

namespace FleshHive;

public class PlaceWorker_FleshNeverAdjacentTrap : PlaceWorker_NeverAdjacentTrap
{
    public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing? thingToIgnore = null, Thing? thing = null)
    {
        foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, def.Size).ExpandedBy(1))
        {
            if (!cell.InBounds(map))
            {
                continue;
            }

            foreach (Thing adjacent in map.thingGrid.ThingsListAt(cell))
            {
                if (adjacent != thingToIgnore &&
                    ((adjacent.def.category == ThingCategory.Building && adjacent.def.building?.isTrap == true) ||
                     ((adjacent.def.IsBlueprint || adjacent.def.IsFrame) && adjacent.def.entityDefToBuild is ThingDef buildingDef && buildingDef.building?.isTrap == true)))
                {
                    return "CannotPlaceAdjacentTrap".Translate();
                }
            }
        }

        return true;
    }
}
