using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FleshSpreadOnDetonation : CompProperties
{
    public CompProperties_FleshSpreadOnDetonation()
    {
        compClass = typeof(CompFleshSpreadOnDetonation);
    }

    public float radius = 3.9f;
}

public class CompFleshSpreadOnDetonation : ThingComp
{
    private CompProperties_FleshSpreadOnDetonation Props => (CompProperties_FleshSpreadOnDetonation)props;

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (previousMap == null || parent.TryGetComp<CompExplosive>()?.destroyedThroughDetonation != true)
        {
            return;
        }

        SpreadFleshTerrain(previousMap);
    }

    private void SpreadFleshTerrain(Map map)
    {
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(parent.Position, Props.radius, true))
        {
            if (!CanSpreadTo(cell, map))
            {
                continue;
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Flesh);
        }
    }

    private bool CanSpreadTo(IntVec3 cell, Map map)
    {
        return cell.InBounds(map) && !cell.Impassable(map) && !FleshTerrainUtility.IsFleshTerrain(map, cell);
    }
}
