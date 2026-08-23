using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject), typeof(ResearchProjectDef), typeof(bool), typeof(Pawn), typeof(bool))]
public static class Patch_BonePlatingFleshBlockResearch
{
    public static void Postfix(ResearchProjectDef proj)
    {
        if (proj != FleshHiveDefOf.FH_Research_BonePlatingFleshBlock || Find.Maps == null)
        {
            return;
        }

        foreach (Map map in Find.Maps)
        {
            ReplaceBuildings(map, FleshHiveDefOf.FH_FleshBarricade, FleshHiveDefOf.FH_ChitinFleshBarricade);
            ReplaceBuildings(map, FleshHiveDefOf.FH_FleshBlock, FleshHiveDefOf.FH_ChitinFleshBlock);
            ReplaceBlueprints(map, FleshHiveDefOf.FH_Building_FleshBarricade, FleshHiveDefOf.FH_Building_ChitinFleshBarricade);
            ReplaceBlueprints(map, FleshHiveDefOf.FH_Building_FleshBlock, FleshHiveDefOf.FH_Building_ChitinFleshBlock);
        }
    }

    private static void ReplaceBuildings(Map map, ThingDef oldDef, ThingDef newDef)
    {
        List<Thing> oldBuildings = map.listerThings.ThingsOfDef(oldDef).ToList();
        foreach (Thing oldBuilding in oldBuildings)
        {
            Faction faction = oldBuilding.Faction;
            IntVec3 position = oldBuilding.Position;
            int hitPoints = oldBuilding.HitPoints;

            oldBuilding.Destroy(DestroyMode.Vanish);

            Thing newBuilding = GenSpawn.Spawn(newDef, position, map, WipeMode.VanishOrMoveAside);
            if (newBuilding.def.CanHaveFaction && faction != null)
            {
                newBuilding.SetFaction(faction);
            }

            newBuilding.HitPoints = Mathf.Clamp(
                Mathf.RoundToInt((float)hitPoints / oldDef.BaseMaxHitPoints * newBuilding.MaxHitPoints),
                1,
                newBuilding.MaxHitPoints);
        }
    }

    private static void ReplaceBlueprints(Map map, HiveBuildingDef oldDef, HiveBuildingDef newDef)
    {
        List<Blueprint_HiveBuild> blueprints = map.listerThings.AllThings
            .OfType<Blueprint_HiveBuild>()
            .Where(blueprint => blueprint.buildingDef == oldDef)
            .ToList();

        foreach (Blueprint_HiveBuild blueprint in blueprints)
        {
            bool forbidden = blueprint.IsForbidden(Faction.OfPlayer);
            IntVec3 position = blueprint.Position;
            Rot4 rotation = blueprint.Rotation;
            float workAmount = blueprint.workAmount;
            List<ResourceCount> needResources = CloneResourceCounts(blueprint.needResources);
            List<ResourceCount> innerResources = CloneResourceCounts(blueprint.innerResources);

            blueprint.Destroy(DestroyMode.Vanish);

            Blueprint_HiveBuild newBlueprint = CreateReplacementBlueprint(blueprint, newDef);
            newBlueprint.workAmount = workAmount;
            newBlueprint.needResources = needResources;
            newBlueprint.innerResources = innerResources;
            GenSpawn.Spawn(newBlueprint, position, map, rotation);
            newBlueprint.SetForbidden(forbidden, false);
        }
    }

    private static List<ResourceCount> CloneResourceCounts(List<ResourceCount> source)
    {
        List<ResourceCount> result = new List<ResourceCount>();
        foreach (ResourceCount resourceCount in source)
        {
            result.Add(new ResourceCount(resourceCount.resource, resourceCount.amount));
        }

        return result;
    }

    private static Blueprint_HiveBuild CreateReplacementBlueprint(Blueprint_HiveBuild sourceBlueprint, HiveBuildingDef newDef)
    {
        if (sourceBlueprint is Blueprint_FleshBuild)
        {
            Blueprint_HiveBuild fleshBlueprint = FleshBlueprintUtility.MakeBlueprint(newDef);
            fleshBlueprint.needResources.Clear();
            return fleshBlueprint;
        }

        Blueprint_HiveBuild blueprint = (Blueprint_HiveBuild)ThingMaker.MakeThing(HiveBuildingDef.blueprints[newDef]);
        blueprint.buildingDef = newDef;
        return blueprint;
    }
}
