using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class CompFleshtitanReversion : ThingComp
{
    public CompProperties_FleshtitanReversion Props =>
        (CompProperties_FleshtitanReversion)props;

    private bool IsWildTitan => parent.def == Props.wildTitanRace;

    private bool IsTrackedTitan => IsWildTitan || parent.def == Props.controlledTitanRace;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref revertAtTick, "revertAtTick", -1);
        Scribe_Values.Look(ref sourceHeartThreatPoints, "sourceHeartThreatPoints", 0f);
        Scribe_Values.Look(ref sourceHeartBiosignature, "sourceHeartBiosignature", -1);
        Scribe_References.Look(ref escortLord, "escortLord");
        Scribe_References.Look(ref responseLord, "responseLord");
        Scribe_Values.Look(ref assaultPending, "assaultPending", defaultValue: false);
        Scribe_Values.Look(ref responseReadyAtTick, "responseReadyAtTick", -1);
        Scribe_Values.Look(ref assemblyPoint, "assemblyPoint", IntVec3.Invalid);
        Scribe_Values.Look(ref nextAssemblyAttemptTick, "nextAssemblyAttemptTick", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            MigrateLegacyResponseLord();
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (IsTrackedTitan && revertAtTick < 0)
        {
            revertAtTick = Find.TickManager.TicksGame + Props.revertAfterTicks;
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (assaultPending)
        {
            TryStartAssault();
        }

        if (IsTrackedTitan && revertAtTick >= 0 && Find.TickManager.TicksGame >= revertAtTick)
        {
            RevertToHeart();
        }
    }

    public override string CompInspectStringExtra()
    {
        if (!IsTrackedTitan || revertAtTick < 0)
        {
            return null!;
        }

        int remainingTicks = Math.Max(revertAtTick - Find.TickManager.TicksGame, 0);
        return "FH_FleshtitanReversionCountdown".Translate(remainingTicks.ToStringTicksToPeriod());
    }

    public void InitializeFromHeart(float heartThreatPoints, Lord? titanEscortLord, int heartBiosignature = -1)
    {
        sourceHeartThreatPoints = heartThreatPoints;
        sourceHeartBiosignature = heartBiosignature;
        escortLord = titanEscortLord;
        assaultPending = titanEscortLord != null;
        responseReadyAtTick = -1;
        assemblyPoint = IntVec3.Invalid;
        nextAssemblyAttemptTick = -1;
    }

    private void TryStartAssault()
    {
        if (parent is not Pawn titan || parent.Map == null || escortLord == null)
        {
            assaultPending = false;
            return;
        }

        MigrateLegacyResponseLord();
        int currentTick = Find.TickManager.TicksGame;
        if (!assemblyPoint.IsValid)
        {
            if (nextAssemblyAttemptTick > currentTick)
            {
                return;
            }

            if (!TryPrepareAssemblyPoint(titan, out assemblyPoint))
            {
                nextAssemblyAttemptTick = currentTick + AssemblyRetryIntervalTicks;
                return;
            }

            escortLord.SetJob(new LordJob_FleshtitanAssembly(assemblyPoint));
            escortLord.GotoToil(escortLord.Graph.StartingToil);
            return;
        }

        SpawnRequest request = parent.Map.deferredSpawner.GetRequestByLord(escortLord);
        if (request != null && !request.done)
        {
            responseReadyAtTick = -1;
            return;
        }

        if (!AllPawnsAtAssemblyPoint(parent.Map))
        {
            responseReadyAtTick = -1;
            return;
        }

        if (responseReadyAtTick < 0)
        {
            responseReadyAtTick = currentTick + PostResponseAssemblyTicks;
            return;
        }
        if (currentTick < responseReadyAtTick)
        {
            return;
        }

        escortLord.SetJob(new LordJob_FleshtitanAssault(titan));
        escortLord.GotoToil(escortLord.Graph.StartingToil);
        responseLord = null;
        assaultPending = false;
    }

    private bool TryPrepareAssemblyPoint(Pawn titan, out IntVec3 destination)
    {
        destination = IntVec3.Invalid;
        Map map = titan.Map;
        IntVec3 raidDestination = GenAI.RandomRaidDest(titan.Position, map);
        if (!raidDestination.IsValid)
        {
            return false;
        }

        using PawnPath path = map.pathFinder.FindPathNow(
            titan.Position,
            raidDestination,
            TraverseParms.For(titan, Danger.Deadly, TraverseMode.PassAllDestroyableThings));
        if (!path.Found)
        {
            return false;
        }

        bool alreadyHasOpenRoute = titan.CanReach(
            raidDestination,
            PathEndMode.OnCell,
            Danger.Deadly,
            canBashDoors: false,
            canBashFences: false);
        List<Building> fleshBlockers = new();
        List<IntVec3> nodes = path.NodesReversed;
        bool foundFleshmass = false;
        for (int index = nodes.Count - 2; index >= 0; index--)
        {
            IntVec3 cell = nodes[index];
            if (!cell.InBounds(map)
                || cell.DistanceToSquared(titan.Position)
                > MaxAssemblyPointSearchDistance * MaxAssemblyPointSearchDistance)
            {
                break;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null
                && (edifice.def == ThingDefOf.Fleshmass || edifice.def == ThingDefOf.Fleshmass_Active))
            {
                foundFleshmass = true;
                if (!fleshBlockers.Contains(edifice))
                {
                    fleshBlockers.Add(edifice);
                }
                continue;
            }

            if (edifice?.def.passability == Traversability.Impassable)
            {
                break;
            }

            if (!cell.Standable(map))
            {
                continue;
            }

            if ((foundFleshmass || alreadyHasOpenRoute)
                && cell.DistanceToSquared(titan.Position)
                >= MinimumAssemblyPointDistance * MinimumAssemblyPointDistance
                && IsAssemblyClearing(cell, map))
            {
                destination = cell;
                break;
            }
        }

        if (!destination.IsValid)
        {
            return false;
        }

        foreach (Building blocker in fleshBlockers)
        {
            IntVec3 position = blocker.Position;
            blocker.Destroy(DestroyMode.Vanish);
            EffecterDefOf.MeatExplosionSmall.Spawn(position, map).Cleanup();
        }
        return destination.Standable(map);
    }

    private bool AllPawnsAtAssemblyPoint(Map map)
    {
        foreach (Pawn pawn in escortLord!.ownedPawns)
        {
            if (pawn.Dead || pawn.Downed)
            {
                continue;
            }
            if (!pawn.Spawned
                || pawn.Map != map
                || !pawn.Position.InHorDistOf(assemblyPoint, AssemblyArrivalRadius))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsAssemblyClearing(IntVec3 center, Map map)
    {
        int standableCells = 0;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, AssemblyClearingRadius, useCenter: true))
        {
            if (!cell.InBounds(map))
            {
                continue;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null
                && (edifice.def == ThingDefOf.Fleshmass || edifice.def == ThingDefOf.Fleshmass_Active))
            {
                return false;
            }
            if (cell.Standable(map))
            {
                standableCells++;
            }
        }
        return standableCells >= MinimumAssemblyClearingCells;
    }

    private void MigrateLegacyResponseLord()
    {
        if (responseLord == null || escortLord == null || parent.Map == null)
        {
            return;
        }

        SpawnRequest request = parent.Map.deferredSpawner.GetRequestByLord(responseLord);
        if (request != null)
        {
            request.lord = escortLord;
        }

        List<Pawn> responsePawns = responseLord.ownedPawns
            .Where(pawn => !pawn.Dead)
            .ToList();
        responseLord.RemovePawns(responsePawns);
        escortLord.AddPawns(responsePawns, updateDuties: false);
        responseLord = null;
    }

    private void RevertToHeart()
    {
        Map map = parent.Map;
        if (map == null)
        {
            return;
        }

        IntVec3 position = parent.Position;
        ThingDef heartDef = IsWildTitan ? Props.heartDef : Props.controlledHeartDef;
        Thing heart = ThingMaker.MakeThing(heartDef);
        if (sourceHeartBiosignature >= 0 && heart is ThingWithComps heartWithComps)
        {
            CompBiosignatureOwner? biosignatureOwner = heartWithComps.GetComp<CompBiosignatureOwner>();
            if (biosignatureOwner != null)
            {
                biosignatureOwner.biosignature = sourceHeartBiosignature;
            }
        }
        if (heart is Building_FleshmassHeart fleshmassHeart)
        {
            fleshmassHeart.GetComp<CompFleshmassHeart>().threatPoints = sourceHeartThreatPoints;
        }
        else if (parent.Faction != null)
        {
            heart.SetFaction(parent.Faction);
        }

        List<Pawn> escorts = escortLord?.ownedPawns
            .Where(pawn => pawn != parent && pawn.Spawned && pawn.Map == map && !pawn.Dead)
            .ToList() ?? new List<Pawn>();
        List<Pawn> lordPawns = escortLord?.ownedPawns
            .Where(pawn => pawn.Spawned && pawn.Map == map && !pawn.Dead)
            .ToList() ?? new List<Pawn>();
        escortLord?.RemovePawns(lordPawns);

        parent.Destroy(DestroyMode.Vanish);
        GenSpawn.Spawn(heart, position, map, Rot4.North, WipeMode.Vanish);
        if (heart is Building_FleshmassHeart restoredHeart && escorts.Count > 0)
        {
            Lord defendHeartLord = restoredHeart.DefendHeartLord;
            defendHeartLord.AddPawns(escorts);
            foreach (Pawn escort in escorts)
            {
                if (escort.mindState?.duty == null)
                {
                    PawnDuty duty = new(DutyDefOf.DefendFleshmassHeart, restoredHeart.Position)
                    {
                        focusSecond = restoredHeart.Position,
                        radius = HeartDefenseRadius,
                        wanderRadius = HeartDefenseWanderRadius
                    };
                    escort.mindState!.duty = duty;
                }
                if (escort.CurJob != null)
                {
                    escort.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }
        }
        EffecterDefOf.MeatExplosionExtraLarge.Spawn(position, map).Cleanup();
    }

    private int revertAtTick = -1;

    private const int PostResponseAssemblyTicks = 300;

    private const int AssemblyRetryIntervalTicks = 250;

    private const int MaxAssemblyPointSearchDistance = 30;

    private const int MinimumAssemblyPointDistance = 14;

    private const float AssemblyClearingRadius = 5f;

    private const int MinimumAssemblyClearingCells = 24;

    private const float AssemblyArrivalRadius = 10f;

    private const float HeartDefenseRadius = 50f;

    private const float HeartDefenseWanderRadius = 12f;

    private float sourceHeartThreatPoints;

    private int sourceHeartBiosignature = -1;

    private Lord? escortLord;

    private Lord? responseLord;

    private bool assaultPending;

    private int responseReadyAtTick = -1;

    private IntVec3 assemblyPoint = IntVec3.Invalid;

    private int nextAssemblyAttemptTick = -1;
}
