using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_UnitDivideSmoke : DeathActionProperties_UnitDivide
{
    public DeathActionProperties_UnitDivideSmoke()
    {
        workerClass = typeof(DeathActionWorker_UnitDivideSmoke);
    }

    public float smokeRadius = 4.9f;
}

public class DeathActionWorker_UnitDivideSmoke : DeathActionWorker_UnitDivide
{
    public new DeathActionProperties_UnitDivideSmoke Props => (DeathActionProperties_UnitDivideSmoke)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        Map map = corpse.MapHeld;
        IntVec3 position = corpse.PositionHeld;
        base.PawnDied(corpse, prevLord);

        if (map != null)
        {
            GenExplosion.DoExplosion(
                position,
                map,
                Props.smokeRadius,
                DamageDefOf.Smoke,
                null,
                -1,
                -1f,
                postExplosionGasType: GasType.BlindSmoke,
                postExplosionGasRadiusOverride: Props.smokeRadius);
            SpawnSmoke(map, position);
            ApplyBerserk(map, position);
        }
    }

    private void SpawnSmoke(Map map, IntVec3 position)
    {
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, Props.smokeRadius, true))
        {
            if (!cell.InBounds(map) || !map.gasGrid.GasCanMoveTo(cell))
            {
                continue;
            }

            Gas smoke = (Gas)ThingMaker.MakeThing(FleshHiveDefOf.FH_SynbulbSmoke);
            GenSpawn.Spawn(smoke, cell, map, WipeMode.VanishOrMoveAside);
        }
    }

    private void ApplyBerserk(Map map, IntVec3 position)
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Dead
                || pawn.TryGetComp<CompFleshBeastCache>() == null
                || !pawn.Position.InHorDistOf(position, Props.smokeRadius))
            {
                continue;
            }

            Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_Berserk);
            if (existingHediff == null)
            {
                pawn.health.AddHediff(FleshHiveDefOf.FH_Berserk);
            }
            else
            {
                existingHediff.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
            }
        }
    }

}
