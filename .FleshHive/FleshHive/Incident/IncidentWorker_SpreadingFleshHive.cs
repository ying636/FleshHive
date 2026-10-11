using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class IncidentWorker_SpreadingFleshHive : IncidentWorker
{
    private IncidentDef_SpreadingFleshHive Definition => (IncidentDef_SpreadingFleshHive)def;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && parms.target is Map map
            && CanCreateHive(map) && TryFindCell(map, out _, out _);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map || !CanCreateHive(map))
        {
            Log.Warning($"[FleshHive] {def.defName} cannot create a hostile hive on the incident target.");
            return false;
        }
        if (!TryFindCell(map, out IntVec3 cell, out List<IntVec3> unitCells))
        {
            Log.Warning($"[FleshHive] {def.defName} found no valid hive location on {map}.");
            return false;
        }

        Faction faction = Faction.OfEntities;
        Thing hive = ThingMaker.MakeThing(Definition.hive);
        hive.SetFaction(faction);
        GenSpawn.Spawn(hive, cell, map, Definition.hive.defaultPlacingRot);
        HiveLord lord = map.GetComponent<MapComponent_HiveLord>().CreateLord(Definition.hiveLord, new[] { hive });
        map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord)
            .AddNutrition(((HiveLordDef_HostileFleshHive)lord.def).initialNutrition);
        float averagePower = Definition.initialUnits.Average(unit => unit.kind.combatPower);
        int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, parms.points)
            * Definition.initialPointsFraction / Mathf.Max(1f, averagePower)),
            Definition.minInitialUnits, Definition.maxInitialUnits);
        List<Pawn> pawns = new(count);
        for (int index = 0; index < count; index++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(Definition.initialUnits.RandomElement().kind, faction);
            GenSpawn.Spawn(pawn, unitCells[index], map);
            pawns.Add(pawn);
        }
        lord.AcceptProducedUnits(hive, pawns);
        if (pawns.Any(pawn => HCFGameUtility.GetUnitComp(pawn)?.group is not { } group
            || !lord.groups.Contains(group)))
        {
            throw new InvalidOperationException($"[FleshHive] {def.defName} failed to assign initial defenders to its HiveLord.");
        }

        SendStandardLetter(parms, hive);
        Find.TickManager.slower.SignalForceNormalSpeedShort();
        return true;
    }

    private bool CanCreateHive(Map map)
    {
        return ModsConfig.AnomalyActive && map.IsPlayerHome
            && Faction.OfEntities is { } faction && faction.HostileTo(Faction.OfPlayer)
            && !map.GetComponent<MapComponent_HiveLord>().lords.Any(lord => lord.def == Definition.hiveLord)
            && map.mapPawns.FreeColonistsSpawnedCount > 0;
    }

    private bool TryFindCell(Map map, out IntVec3 result, out List<IntVec3> unitCells)
    {
        result = IntVec3.Invalid;
        unitCells = new List<IntVec3>();
        List<Thing> colony = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Where(thing => thing.Faction == Faction.OfPlayer).ToList();
        colony.AddRange(map.mapPawns.FreeColonistsSpawned);
        List<Thing> hives = map.GetComponent<MapComponent_HiveLord>().lords.SelectMany(lord => lord.hives).ToList();
        ThingDef building = Definition.hive;
        Rot4 rotation = building.defaultPlacingRot;
        for (int attempt = 0; attempt < Definition.cellSearchAttempts; attempt++)
        {
            IntVec3 cell = new(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
            if (cell.Fogged(map) || colony.Any(thing => thing.Position.DistanceToSquared(cell)
                    < Definition.minColonyDistance * Definition.minColonyDistance)
                || hives.Any(hive => hive.Position.DistanceToSquared(cell)
                    < Definition.minHiveDistance * Definition.minHiveDistance))
            {
                continue;
            }
            if (cell.x < 22 || cell.z < 22 || cell.x >= map.Size.x - 22 || cell.z >= map.Size.z - 22)
                continue;

            CellRect occupiedRect = GenAdj.OccupiedRect(cell, rotation, building.size);
            if (occupiedRect.Cells.Any(occupied => !occupied.InBounds(map) || occupied.InNoBuildEdgeArea(map)
                || occupied.Fogged(map) || !occupied.Standable(map)
                || map.terrainGrid.FoundationAt(occupied)?.IsSubstructure == true
                || occupied.GetThingList(map).Any(thing => thing is Pawn || thing.def.category == ThingCategory.Building
                    || thing.def.IsBlueprint || thing.def.IsFrame
                    || thing is not Plant && thing is not Filth && GenSpawn.SpawningWipes(building, thing.def)))
                || !GenConstruct.CanBuildOnTerrain(building, cell, map, rotation)
                || !GenConstruct.InteractionCellStandable(building, cell, rotation, map).Accepted
                || !GenConstruct.NotBlockingAnyInteractionCells(building, cell, rotation, map).Accepted
                || building.PlaceWorkers?.Any(worker => !worker.AllowsPlacing(building, cell, rotation, map).Accepted) == true
                || map.GetComponent<MapComponent_HiveLord>().lords.Any(lord => lord.Expanding
                    && lord.ExpansionRect.Cells.Any(occupiedRect.Contains))
                || !map.mapPawns.FreeColonistsSpawned.Any(pawn => map.reachability.CanReach(cell, pawn,
                    PathEndMode.Touch, TraverseMode.PassDoors, Danger.Deadly)))
            {
                continue;
            }

            unitCells = GenRadial.RadialCellsAround(cell, 8f, true)
                .Where(position => position.InBounds(map) && !occupiedRect.Contains(position)
                    && !position.Fogged(map) && position.Standable(map)
                    && !position.GetThingList(map).Any(thing => thing is Pawn || thing.def.category == ThingCategory.Building)
                    && map.reachability.CanReach(cell, position, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly))
                .Take(Definition.maxInitialUnits).ToList();
            if (unitCells.Count < Definition.maxInitialUnits)
            {
                continue;
            }
            result = cell;
            return true;
        }
        return false;
    }
}
