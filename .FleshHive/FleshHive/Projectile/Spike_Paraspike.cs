using RimWorld;
using Verse;

namespace FleshHive;

public class Spike_Paraspike : Bullet
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        if (hitThing is Pawn pawn)
        {
            HealthUtility.AdjustSeverity(pawn,FleshHiveDefOf.FH_Spike_Paraspike,1f);
        }
    }
}