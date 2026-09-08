using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class ParasitismHediff : HediffWithComps
{
    public bool CanDraw => Comp?.Props.drawIcon == true;

    public ParasitismComp Comp
    {
        get
        {
            if (comp == null && flesh != null)
            {
                comp = flesh.TryGetComp<ParasitismComp>();
            }
            return comp;
        }
    }

    public int Count
    {
        get
        {
            return spaceCost;
        }
    }

    public override HediffStage CurStage
    {
        get
        {
            HediffStage stage = base.CurStage;
            if (stage == null || pawn?.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_FleshAdaptation) != true
                || stage.statOffsets?.Any(offset => offset.stat == StatDefOf.SocialImpact && offset.value < 0f) != true)
            {
                return stage;
            }

            if (adaptedSourceStage != stage)
            {
                adaptedStage = Gen.MemberwiseClone(stage);
                adaptedStage.statOffsets = stage.statOffsets
                    .Where(offset => offset.stat != StatDefOf.SocialImpact || offset.value >= 0f)
                    .ToList();
                adaptedSourceStage = stage;
            }

            return adaptedStage;
        }
    }

    public override string LabelInBrackets
    {
        get
        {
            string parasiteLabel = flesh?.LabelShort;
            if (parasiteLabel.NullOrEmpty())
            {
                return base.LabelInBrackets;
            }
            return parasiteLabel;
        }
    }

    public override void PreRemoved()
    {
        base.PreRemoved();
        if (this.flesh != null && !this.flesh.Spawned && this.pawn.MapHeld is { } map)
        {
            GenSpawn.Spawn(this.flesh, this.pawn.Position, map);
        }
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        (flesh as FleshReplicaUnit)?.ClearSync(this);
        if (this.pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is ParasitismSystem system)
        {
            system.SetDirty();
        }
    }

    public override bool TryMergeWith(Hediff other)
    {
        return false;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.spaceCost, "spaceCost");
        Scribe_Values.Look(ref this.parentChildParasite, "parentChildParasite", false);
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            fleshIsReference = flesh is FleshReplicaUnit;
        }

        Scribe_Values.Look(ref fleshIsReference, "fleshIsReference", false);
        if (fleshIsReference)
        {
            Scribe_References.Look(ref flesh, "pawn");
        }
        else
        {
            Scribe_Deep.Look(ref flesh, "pawn");
        }
        Scribe_References.Look(ref this.lord, "lord");
    }

    ParasitismComp comp;
    public Pawn flesh;
    public Lord lord;
    public bool parentChildParasite;
    private bool fleshIsReference;
    public int spaceCost = 1;
    private HediffStage? adaptedSourceStage;
    private HediffStage? adaptedStage;
}
