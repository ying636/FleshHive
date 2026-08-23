using RimWorld;
using Verse;

namespace FleshHive;

public static class FleshTerrainUtility
{
    public static bool IsFleshTerrain(Map map, IntVec3 cell)
    {
        return map != null && cell.InBounds(map) && IsFleshTerrain(cell.GetTerrain(map));
    }

    public static bool IsFleshTerrain(TerrainDef terrain)
    {
        return terrain == TerrainDefOf.Flesh || terrain == FleshHiveDefOf.FH_FleshCarapaceFloor;
    }

    public static bool CanFleshSpreadTo(Map map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map))
        {
            return false;
        }

        TerrainDef terrain = cell.GetTerrain(map);
        return terrain.natural && !IsFleshTerrain(terrain);
    }

    public static bool HasLargeFleshEcosystem(Map map)
    {
        return map?.GetComponent<MapComponent_FleshHive>()?.HiveScale > LargeFleshEcosystemThreshold;
    }

    private const int LargeFleshEcosystemThreshold = 1000;
}
