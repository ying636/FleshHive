using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class CompFleshmassHeartTitanCycle : CompFleshmassHeart
{
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref transformAtTick, "transformAtTick", -1);
        Scribe_Values.Look(ref consecutiveFailedGrowths, "consecutiveFailedGrowths", 0);
    }

    public override void CompTick()
    {
        base.CompTick();

        if (transformAtTick < 0 && (GrowthPoints <= 0 || consecutiveFailedGrowths >= BlockedGrowthThreshold))
        {
            StartTransformCountdown();
        }

        if (transformAtTick >= 0 && Find.TickManager.TicksGame >= transformAtTick)
        {
            TransformIntoTitan();
        }
    }

    public override string CompInspectStringExtra()
    {
        string baseString = base.CompInspectStringExtra();
        if (transformAtTick < 0)
        {
            return baseString;
        }

        int remainingTicks = Math.Max(transformAtTick - Find.TickManager.TicksGame, 0);
        string countdown = "FH_FleshmassHeartTitanCountdown".Translate(remainingTicks.ToStringTicksToPeriod());
        return baseString.NullOrEmpty() ? countdown : baseString + "\n" + countdown;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        if (DebugSettings.ShowDevGizmos)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Exhaust growth points",
                defaultDesc = "Set the remaining fleshmass growth points to zero.",
                action = delegate
                {
                    growthPoints = 0;
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: Transform into fleshtitan now",
                defaultDesc = "Immediately transform this fleshmass heart into a fleshtitan.",
                action = TransformIntoTitan
            };
        }
    }

    protected override void Grow()
    {
        Map map = parent.Map;
        int activeFleshmassBefore = map.listerThings.ThingsOfDef(ThingDefOf.Fleshmass_Active).Count;
        int nerveBundlesBefore = map.listerThings.ThingsOfDef(ThingDefOf.NerveBundle).Count;
        int spittersBefore = map.listerThings.ThingsOfDef(ThingDefOf.FleshmassSpitter).Count;

        base.Grow();

        bool grew = map.listerThings.ThingsOfDef(ThingDefOf.Fleshmass_Active).Count > activeFleshmassBefore
            || map.listerThings.ThingsOfDef(ThingDefOf.NerveBundle).Count > nerveBundlesBefore
            || map.listerThings.ThingsOfDef(ThingDefOf.FleshmassSpitter).Count > spittersBefore;
        consecutiveFailedGrowths = grew ? 0 : consecutiveFailedGrowths + 1;
    }

    private void StartTransformCountdown()
    {
        transformAtTick = Find.TickManager.TicksGame + TransformDelayTicks;
        Find.LetterStack.ReceiveLetter(
            "FH_FleshmassHeartTitanWarningLabel".Translate(),
            "FH_FleshmassHeartTitanWarningText".Translate(TransformDelayTicks.ToStringTicksToPeriod()),
            LetterDefOf.ThreatSmall,
            parent);
    }

    private void TransformIntoTitan()
    {
        Map map = parent.Map;
        if (map == null)
        {
            return;
        }

        IntVec3 position = parent.Position;
        float sourceThreatPoints = threatPoints;
        Lord heartLord = Heart.DefendHeartLord;
        List<Pawn> escorts = heartLord.ownedPawns
            .Where(pawn => pawn.Spawned && pawn.Map == map && !pawn.Dead)
            .ToList();
        heartLord.RemovePawns(escorts);

        List<Lord> lordsBeforeResponse = map.lordManager.lords.ToList();
        FleshbeastUtility.DoFleshbeastResponse(this, position);
        Lord? responseLord = map.lordManager.lords.FirstOrDefault(lord =>
            !lordsBeforeResponse.Contains(lord) && lord.LordJob is LordJob_FleshbeastAssault);
        Pawn titan = PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fleshtitan, Faction.OfEntities);
        int biosignature = parent.GetComp<CompBiosignatureOwner>()?.biosignature ?? -1;

        bool allowDestroyNonDestroyable = Thing.allowDestroyNonDestroyable;
        Thing.allowDestroyNonDestroyable = true;
        try
        {
            parent.Destroy(DestroyMode.Vanish);
        }
        finally
        {
            Thing.allowDestroyNonDestroyable = allowDestroyNonDestroyable;
        }

        GenSpawn.Spawn(titan, position, map);
        Lord assemblyLord = LordMaker.MakeNewLord(
            Faction.OfEntities,
            new LordJob_FleshtitanAssembly(),
            map);
        assemblyLord.AddPawns(new List<Pawn> { titan }, updateDuties: false);
        assemblyLord.AddPawns(escorts);

        if (responseLord != null)
        {
            SpawnRequest responseRequest = map.deferredSpawner.GetRequestByLord(responseLord);
            if (responseRequest != null)
            {
                responseRequest.lord = assemblyLord;
            }

            List<Pawn> responsePawns = responseLord.ownedPawns
                .Where(pawn => !pawn.Dead)
                .ToList();
            responseLord.RemovePawns(responsePawns);
            assemblyLord.AddPawns(responsePawns, updateDuties: false);
        }

        titan.TryGetComp<CompFleshtitanReversion>()?.InitializeFromHeart(
            sourceThreatPoints,
            assemblyLord,
            biosignature);
        EffecterDefOf.MeatExplosionExtraLarge.Spawn(position, map).Cleanup();
    }

    private const int TransformDelayTicks = GenDate.TicksPerHour;

    private const int BlockedGrowthThreshold = 30;

    private int transformAtTick = -1;

    private int consecutiveFailedGrowths;
}
