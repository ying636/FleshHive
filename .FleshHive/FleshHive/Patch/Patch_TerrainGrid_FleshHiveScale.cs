using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.SetTerrain))]
public static class Patch_TerrainGrid_FleshHiveScale
{
    public static void Prefix(TerrainGrid __instance, IntVec3 c, out TerrainDef __state)
    {
        __state = __instance.TerrainAt(c);
    }

    public static void Postfix(Map ___map, TerrainDef newTerr, TerrainDef __state)
    {
        bool wasFlesh = FleshTerrainUtility.IsFleshTerrain(__state);
        bool isFlesh = FleshTerrainUtility.IsFleshTerrain(newTerr);
        if (wasFlesh == isFlesh)
        {
            return;
        }
        MapComponent_FleshHive component = ___map.GetComponent<MapComponent_FleshHive>();
        if (component == null)
        {
            return;
        }
        component.Notify_FleshTerrainChanged(isFlesh ? 1 : -1);
    }

}
