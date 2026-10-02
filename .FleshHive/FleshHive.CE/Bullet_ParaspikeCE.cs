using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Bullet_ParaspikeCE : BulletCE
{
    public override void Impact(Thing hitThing)
    {
        base.Impact(hitThing);
        if (hitThing is Pawn pawn)
        {
            HealthUtility.AdjustSeverity(pawn, FleshHiveDefOf.FH_Spike_Paraspike, 1f);
        }
    }
}
