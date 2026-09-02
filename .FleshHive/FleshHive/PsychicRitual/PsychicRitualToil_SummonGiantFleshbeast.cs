using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class PsychicRitualToil_SummonGiantFleshbeast : PsychicRitualToil
{
    public PsychicRitualToil_SummonGiantFleshbeast()
    {
    }

    public PsychicRitualToil_SummonGiantFleshbeast(PsychicRitualRoleDef invokerRole)
    {
        this.invokerRole = invokerRole;
    }

    public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
        if (invoker == null)
        {
            return;
        }

        PsychicRitualDef_SummonGiantFleshbeast ritualDef = (PsychicRitualDef_SummonGiantFleshbeast)psychicRitual.def;
        IntVec3 spawnCenter = FindSpawnCenter(psychicRitual, invoker);
        SpawnFleshbeasts(psychicRitual, ritualDef, spawnCenter);
        Find.LetterStack.ReceiveLetter(
            "PsychicRitualCompleteLabel".Translate(psychicRitual.def.label),
            "FH_SummonGiantFleshbeastCompleteText".Translate(invoker, psychicRitual.def.Named("RITUAL"), ritualDef.summonKind.Named("GIANT")),
            LetterDefOf.ThreatBig,
            new TargetInfo(spawnCenter, psychicRitual.Map));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref invokerRole, "invokerRole");
    }

    private static IntVec3 FindSpawnCenter(PsychicRitual psychicRitual, Pawn invoker)
    {
        List<Pawn> hostiles = (from target in psychicRitual.Map.attackTargetsCache.TargetsHostileToColony
                              where !target.Thing.Fogged()
                                    && target.Thing is Pawn pawn
                                    && !target.ThreatDisabled(invoker)
                                    && !pawn.IsOnHoldingPlatform
                              select target.Thing as Pawn).ToList();
        IEnumerable<Pawn> candidates = hostiles.Where(pawn => pawn.RaceProps.Humanlike && !pawn.IsSubhuman);
        if (!candidates.Any())
        {
            candidates = hostiles.Where(pawn => !pawn.IsSubhuman && !pawn.RaceProps.IsAnomalyEntity);
            if (!candidates.Any())
            {
                candidates = hostiles;
            }
        }

        IAttackTarget[] shuffled = candidates.ToArray();
        shuffled.Shuffle();
        IntVec3 hostileSpawnCenter = TryFindingValidHostileSpawnCenter(psychicRitual, shuffled);
        if (hostileSpawnCenter != IntVec3.Invalid)
        {
            return hostileSpawnCenter;
        }

        CellFinder.TryFindRandomCell(
            psychicRitual.Map,
            cell => cell.Walkable(psychicRitual.Map)
                    && !cell.Fogged(psychicRitual.Map)
                    && !psychicRitual.Map.thingGrid.ThingsListAtFast(cell).Any(),
            out IntVec3 result);
        return result;
    }

    private static IntVec3 TryFindingValidHostileSpawnCenter(PsychicRitual psychicRitual, IAttackTarget[] hostiles)
    {
        for (int i = 0; i < hostiles.Length && i <= 4; i++)
        {
            int walkableCells = 0;
            IntVec3 position = hostiles[i].Thing.PositionHeld;
            Map map = psychicRitual.Map;
            CellRect cellRect = CellRect.CenteredOn(position, SpawnRadius);
            cellRect.ClipInsideMap(map);
            foreach (IntVec3 cell in cellRect)
            {
                if (cell != position && cell.Walkable(map) && !cell.Fogged(map))
                {
                    walkableCells++;
                }
            }

            if (walkableCells >= MinWalkableCells)
            {
                return position;
            }
        }

        return IntVec3.Invalid;
    }

    private static void SpawnFleshbeasts(PsychicRitual psychicRitual, PsychicRitualDef_SummonGiantFleshbeast ritualDef, IntVec3 spawnCenter)
    {
        if (!ModsConfig.AnomalyActive || ritualDef.summonKind == null)
        {
            return;
        }

        IntVec3 spawnCell = IntVec3.Invalid;
        if (LargeBuildingCellFinder.TryFindCellNear(spawnCenter, psychicRitual.Map, SpawnRadius, BurrowSpawnParms.ForThing(ThingDefOf.PitBurrow), out IntVec3 cell) && cell != spawnCenter)
        {
            spawnCell = cell;
        }

        if (spawnCell == IntVec3.Invalid)
        {
            return;
        }

        float escortPoints = Mathf.Max(0f, ritualDef.escortPointsFromQualityCurve.Evaluate(psychicRitual.PowerPercent));
        List<Pawn> fleshbeasts = FleshHiveFleshbeastSpawnUtility.GenerateRandomByPoints(FleshHiveFleshbeastSpawnUtility.StandardSpawnKinds, (int)escortPoints, Faction.OfEntities, applyDefaultParasites: false);
        Pawn summonedGiant = PawnGenerator.GeneratePawn(ritualDef.summonKind, Faction.OfEntities);
        fleshbeasts.Insert(0, summonedGiant);

        ThingDef spawnerDef = ritualDef.summonKind == FleshHiveDefOf.FH_Furiousmeld
            ? FleshHiveDefOf.FH_FuriousmeldPitBurrowSpawner
            : FleshHiveDefOf.FH_GiantFleshbeastPitBurrowSpawner;
        BuildingGroundSpawner spawner = (BuildingGroundSpawner)ThingMaker.MakeThing(spawnerDef);
        spawner.emergeDelay = PitBurrowEmergenceDelayRangeTicks;
        PitBurrow pitBurrow = (PitBurrow)spawner.ThingToSpawn;
        pitBurrow.emergingFleshbeasts = fleshbeasts;
        pitBurrow.emergeDelay = FleshbeastSpawnDelayTicks.RandomInRange;
        pitBurrow.assaultColony = true;
        GenSpawn.Spawn(spawner, spawnCell, psychicRitual.Map);
    }

    private PsychicRitualRoleDef invokerRole = null!;

    private const int SpawnRadius = 10;
    private const int MinWalkableCells = 5;

    private static readonly IntRange FleshbeastSpawnDelayTicks = new(180, 180);
    private static readonly IntRange PitBurrowEmergenceDelayRangeTicks = new(420, 420);
    private static readonly LargeBuildingSpawnParms BurrowSpawnParms = new()
    {
        maxDistanceToColonyBuilding = -1f,
        minDistToEdge = 10,
        attemptSpawnLocationType = SpawnLocationType.Outdoors,
        attemptNotUnderBuildings = true,
        canSpawnOnImpassable = false
    };
}
