using System.Linq;
using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(ThingUtility), nameof(ThingUtility.CheckAutoRebuildOnDestroyed))]
public static class Patch_ThingUtility_AutoRebuild_FleshBlueprint
{
    public static bool Prefix(Thing thing, DestroyMode mode, Map map, BuildableDef buildingDef)
    {
        if (thing?.def?.tradeTags?.Contains(FleshHiveTags.FleshBuilding) != true)
        {
            return true;
        }

        if (!Find.PlaySettings.autoRebuild
            || mode != DestroyMode.KillFinalize
            || thing.Faction != Faction.OfPlayer
            || buildingDef == null
            || !buildingDef.IsResearchFinished
            || map == null
            || !map.areaManager.Home[thing.Position]
            || !GenConstruct.CanPlaceBlueprintAt(buildingDef, thing.Position, thing.Rotation, map, godMode: false, null, null, thing.Stuff).Accepted)
        {
            return true;
        }

        HiveBuildingDef hiveBuildingDef = DefDatabase<HiveBuildingDef>.AllDefsListForReading
            .FirstOrDefault(def => def.Buildable == buildingDef);
        if (hiveBuildingDef == null
            || !HiveBuildingDef.blueprints.TryGetValue(hiveBuildingDef, out ThingDef blueprintDef)
            || blueprintDef == null
            || hiveBuildingDef.worker == null
            || !hiveBuildingDef.worker.CanShow(map))
        {
            return false;
        }

        Blueprint_HiveBuild blueprint = FleshBlueprintUtility.MakeBlueprint(hiveBuildingDef);
        GenSpawn.Spawn(blueprint, thing.Position, map, thing.Rotation);
        return false;
    }
}
