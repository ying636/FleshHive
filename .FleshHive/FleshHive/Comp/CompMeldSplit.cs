using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_MeldSplit : CompProperties
{
    public CompProperties_MeldSplit()
    {
        this.compClass = typeof(CompMeldSplit);
    }

    public List<PawnKindDef> spawnOptions;

    public IntRange firstHitSpawnCountRange = new IntRange(1, 3);

    public int firstHitSpawnRadius = 5;

    public IntRange thresholdSpawnPointsRange = new IntRange(100, 300);

    public float damageThreshold = 200f;
}

public class CompMeldSplit : ThingComp
{
    private CompProperties_MeldSplit Props
    {
        get
        {
            return (CompProperties_MeldSplit)this.props;
        }
    }

    private List<PawnKindDef> cachedSpawnOptions;

    private float totalDamageTaken;

    private List<PawnKindDef> GetSpawnOptions()
    {
        if (cachedSpawnOptions == null)
        {
            if (Props.spawnOptions != null && Props.spawnOptions.Count > 0)
            {
                cachedSpawnOptions = Props.spawnOptions;
            }
            else
            {
                cachedSpawnOptions = new List<PawnKindDef>();
            }
        }
        return cachedSpawnOptions;
    }

    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        if (!this.parent.Spawned)
        {
            return;
        }
        float previousTotal = this.totalDamageTaken;
        this.totalDamageTaken += totalDamageDealt;
        if (previousTotal == 0f)
        {
            SpawnFirstHit();
        }
        if ((int)(previousTotal / this.Props.damageThreshold) < (int)(this.totalDamageTaken / this.Props.damageThreshold))
        {
            SpawnThreshold();
        }
    }

    private void SpawnFirstHit()
    {
        Map map = this.parent.MapHeld;
        if (map == null)
        {
            return;
        }
        List<PawnKindDef> options = GetSpawnOptions();
        if (options.Count == 0)
        {
            return;
        }
        int count = this.Props.firstHitSpawnCountRange.RandomInRange;
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByCount(options, count, this.parent.Faction, this.parent.PositionHeld, map, this.Props.firstHitSpawnRadius);
    }

    private void SpawnThreshold()
    {
        if (!TwistedFleshUtility.ConsumeTwistedFlesh((Pawn)this.parent, 100))
        {
            return;
        }
        Map map = this.parent.MapHeld;
        if (map == null)
        {
            return;
        }
        List<PawnKindDef> options = GetSpawnOptions();
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(options, this.Props.thresholdSpawnPointsRange, this.parent.Faction, this.parent.PositionHeld, map, this.Props.firstHitSpawnRadius);
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref this.totalDamageTaken, "totalDamageTaken", 0f, false);
    }
}
