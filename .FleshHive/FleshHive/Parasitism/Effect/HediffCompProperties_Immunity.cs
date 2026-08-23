using System.Collections.Generic;
using Verse;

namespace FleshHive.Effect;


public class HediffCompProperties_Immunity : HediffCompProperties
{
    public HediffCompProperties_Immunity()
    {
        this.compClass=typeof(HediffComp_Immunity);
    }
    
    public List<HediffDef> hds = new List<HediffDef>();
}

public class HediffComp_Immunity : HediffComp
{
    public HediffCompProperties_Immunity Prop => (HediffCompProperties_Immunity)this.props;
    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        foreach (var hediffSetHediff in this.Pawn.health.hediffSet.hediffs.ListFullCopy())
        {
            if (this.Prop.hds.Contains(hediffSetHediff.def))
            {
                this.Pawn.health.RemoveHediff(hediffSetHediff); 
            }
        }
    }
}
