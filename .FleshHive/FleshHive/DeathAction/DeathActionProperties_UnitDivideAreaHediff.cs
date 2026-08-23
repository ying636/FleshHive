using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_UnitDivideAreaHediff : DeathActionProperties_UnitDivide
{
    public DeathActionProperties_UnitDivideAreaHediff()
    {
        workerClass = typeof(DeathActionWorker_UnitDivideAreaHediff);
    }

    public HediffDef hediff = null!;

    public float radius = 12f;
}

public class DeathActionWorker_UnitDivideAreaHediff : DeathActionWorker_UnitDivide
{
    public new DeathActionProperties_UnitDivideAreaHediff Props => (DeathActionProperties_UnitDivideAreaHediff)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        Map map = corpse.MapHeld;
        IntVec3 position = corpse.PositionHeld;
        Faction faction = corpse.InnerPawn.Faction;
        base.PawnDied(corpse, prevLord);

        if (map == null || Props.hediff == null)
        {
            return;
        }

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Dead
                || pawn.Faction != faction
                || !pawn.Position.InHorDistOf(position, Props.radius)
                || pawn.TryGetComp<CompFleshBeastCache>() == null)
            {
                continue;
            }

            ApplyHediff(pawn);
        }
    }

    private void ApplyHediff(Pawn pawn)
    {
        Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
        if (existingHediff == null)
        {
            pawn.health.AddHediff(Props.hediff);
            return;
        }

        existingHediff.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
    }
}
