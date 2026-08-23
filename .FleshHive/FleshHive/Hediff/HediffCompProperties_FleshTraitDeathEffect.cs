using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_FleshTraitDeathEffect : HediffCompProperties
{
    public HediffCompProperties_FleshTraitDeathEffect()
    {
        compClass = typeof(HediffComp_FleshTraitDeathEffect);
    }

    public bool acidExplosion;

    public int acidDamageAmount = 10;

    public float acidExplosionRadius = 1.9f;

    public bool spawnSmallFleshbeast;

    public int spawnCount = 1;
}

public class HediffComp_FleshTraitDeathEffect : HediffComp
{
    public new HediffCompProperties_FleshTraitDeathEffect Props => (HediffCompProperties_FleshTraitDeathEffect)props;

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        Map map = Pawn.MapHeld;
        if (map == null)
        {
            return;
        }

        IntVec3 position = Pawn.PositionHeld;
        if (Props.acidExplosion)
        {
            DoAcidExplosion(position, map);
        }

        if (Props.spawnSmallFleshbeast)
        {
            SpawnSmallFleshbeasts(position, map);
        }
    }

    private void DoAcidExplosion(IntVec3 position, Map map)
    {
        GenExplosion.DoExplosion(
            position,
            map,
            Props.acidExplosionRadius,
            DamageDefOf.AcidBurn,
            Pawn,
            Props.acidDamageAmount,
            -1f,
            DefDatabase<SoundDef>.GetNamed("SpitterSpitLands"),
            projectile: FleshHiveDefOf.FH_Bullet_Shell_AcidSpit,
            postExplosionSpawnThingDef: DefDatabase<ThingDef>.GetNamed("Filth_SpentAcid"),
            postExplosionSpawnChance: 1f,
            postExplosionSpawnThingCount: 1,
            doVisualEffects: false);
    }

    private void SpawnSmallFleshbeasts(IntVec3 position, Map map)
    {
        FleshHiveFleshbeastSpawnUtility.SpawnRandomBySize(FleshBeastSize.Small, Props.spawnCount, Pawn.Faction, position, map, 5, makeFilth: false, tryAssignEnemyLord: true);
    }
}
