using System;
using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public static class FleshHiveFleshbeastSpawnUtility
{
    public static IEnumerable<PawnKindDef> StandardSpawnKinds
    {
        get
        {
            foreach (PawnKindDef kind in FleshBeastKindUtility.SmallKinds)
            {
                yield return kind;
            }
            foreach (PawnKindDef kind in FleshBeastKindUtility.MediumKinds)
            {
                yield return kind;
            }
            foreach (PawnKindDef kind in FleshBeastKindUtility.LargeKinds)
            {
                yield return kind;
            }
        }
    }

    public static Pawn GeneratePawn(PawnKindDef kind, Faction faction)
    {
        return PawnGenerator.GeneratePawn(GenerateRequest(kind, faction));
    }

    public static Pawn GenerateRandomPawn(FleshBeastSize size, Faction faction)
    {
        return GeneratePawn(FleshBeastKindUtility.RandomKind(size), faction);
    }

    public static List<Pawn> GenerateRandomByPoints(IEnumerable<PawnKindDef> options, IntRange pointsRange, Faction faction)
    {
        return GenerateRandomByPoints(options, pointsRange.RandomInRange, faction);
    }

    public static List<Pawn> GenerateRandomByPoints(IEnumerable<PawnKindDef> options, int targetPoints, Faction faction)
    {
        List<Pawn> pawns = new List<Pawn>();
        List<PawnKindDef> spawnOptions = ValidOptions(options);
        if (spawnOptions.Count == 0 || targetPoints <= 0)
        {
            return pawns;
        }

        int accumulatedPoints = 0;
        int iterations = 0;
        while (accumulatedPoints < targetPoints && iterations < MaxSpawnIterations)
        {
            iterations++;
            PawnKindDef kind = spawnOptions.RandomElement();
            if (accumulatedPoints + kind.combatPower > targetPoints * 1.5f && iterations < MinOvershootAttempts)
            {
                continue;
            }

            accumulatedPoints += (int)kind.combatPower;
            pawns.Add(GeneratePawn(kind, faction));
        }

        return pawns;
    }

    public static bool TryRandomKind(IEnumerable<PawnKindDef> options, out PawnKindDef kind)
    {
        return TryRandomKind(options, null, out kind);
    }

    public static bool TryRandomKind(IEnumerable<PawnKindDef> options, Predicate<PawnKindDef>? validator, out PawnKindDef kind)
    {
        List<PawnKindDef> spawnOptions = ValidOptions(options, validator);
        return spawnOptions.TryRandomElement(out kind);
    }

    public static void SpawnRandomByCount(IEnumerable<PawnKindDef> options, int count, Faction faction, IntVec3 position, Map map, int spawnRadius, bool makeFilth = true)
    {
        List<PawnKindDef> spawnOptions = ValidOptions(options);
        if (spawnOptions.Count == 0 || map == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            PawnKindDef kind = spawnOptions.RandomElement();
            Pawn pawn = GeneratePawn(kind, faction);
            SpawnPawnAsFlyer(pawn, position, map, spawnRadius);
        }

        if (makeFilth)
        {
            MakeSplitFilth(position, map);
        }
    }

    public static void SpawnRandomBySize(FleshBeastSize size, int count, Faction faction, IntVec3 position, Map map, int spawnRadius, Pawn? sourcePawn = null, bool makeFilth = true, bool tryAssignEnemyLord = false)
    {
        if (map == null || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Pawn pawn = GenerateRandomPawn(size, faction);
            SpawnPawnAsFlyer(pawn, position, map, spawnRadius, sourcePawn, tryAssignEnemyLord);
        }

        if (makeFilth)
        {
            MakeSplitFilth(position, map);
        }
    }

    public static void SpawnRandomByPoints(IEnumerable<PawnKindDef> options, IntRange pointsRange, Faction faction, IntVec3 position, Map map, int spawnRadius, bool makeFilth = true)
    {
        SpawnRandomByPoints(options, pointsRange.RandomInRange, faction, position, map, spawnRadius, makeFilth);
    }

    public static void SpawnRandomByPoints(IEnumerable<PawnKindDef> options, int targetPoints, Faction faction, IntVec3 position, Map map, int spawnRadius, bool makeFilth = true)
    {
        if (map == null)
        {
            return;
        }

        foreach (Pawn pawn in GenerateRandomByPoints(options, targetPoints, faction))
        {
            SpawnPawnAsFlyer(pawn, position, map, spawnRadius);
        }

        if (makeFilth)
        {
            MakeSplitFilth(position, map);
        }
    }

    public static void SpawnPawnAsFlyer(Pawn pawn, IntVec3 position, Map map, int spawnRadius, Pawn? sourcePawn = null, bool tryAssignEnemyLord = false)
    {
        GenSpawn.Spawn(pawn, position, map, WipeMode.VanishOrMoveAside);
        if (sourcePawn != null && pawn.TryGetComp<CompInspectStringEmergence>() is { } emergence)
        {
            emergence.sourcePawn = sourcePawn;
        }
        HCFGameUtility.AssignGroup(pawn, map, true);
        FleshbeastUtility.SpawnPawnAsFlyer(pawn, map, position, spawnRadius, true);
        if (tryAssignEnemyLord)
        {
            TryAssignEnemyLord(pawn, map);
        }
    }

    public static void MakeSplitFilth(IntVec3 position, Map map)
    {
        if (map == null)
        {
            return;
        }

        FleshbeastUtility.MeatSplatter(BloodFilthCountRange.RandomInRange, position, map, FleshbeastUtility.MeatExplosionSize.Normal);
        FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_TwistedFlesh, 1, FilthSourceFlags.None, true);
    }

    private static PawnGenerationRequest GenerateRequest(PawnKindDef kind, Faction faction)
    {
        return new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, null, false, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, 0f, 0f, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false);
    }

    private static List<PawnKindDef> ValidOptions(IEnumerable<PawnKindDef> options, Predicate<PawnKindDef>? validator = null)
    {
        if (options == null)
        {
            return new List<PawnKindDef>();
        }

        return options.Where(kind => kind != null && (validator == null || validator(kind))).ToList();
    }

    private static void TryAssignEnemyLord(Pawn pawn, Map map)
    {
        if (pawn.Faction == null || pawn.Faction.IsPlayer || !pawn.Faction.HostileTo(Faction.OfPlayer))
        {
            return;
        }
        Lord lord = map.lordManager.lords.FirstOrDefault(l => l.faction == pawn.Faction && l.CanAddPawn(pawn));
        lord?.AddPawn(pawn);
    }

    private const int MaxSpawnIterations = 50;

    private const int MinOvershootAttempts = 5;

    private static readonly IntRange BloodFilthCountRange = new IntRange(1, 2);
}
