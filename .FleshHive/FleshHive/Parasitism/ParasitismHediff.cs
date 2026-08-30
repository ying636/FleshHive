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
}
