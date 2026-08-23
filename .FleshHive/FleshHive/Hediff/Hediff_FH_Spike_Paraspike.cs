using RimWorld;
using Verse;

namespace FleshHive;

public class Hediff_FH_Spike_Paraspike : HediffWithComps
{
    public override void Tick()
    {
        base.Tick();
        if (this.pawn.IsHashIntervalTick(60))
        {
            this.pawn.TakeDamage(new DamageInfo(DamageDefOf.Bite, 1f));
        }
    }
    
}
