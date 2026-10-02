using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Bullet_BoneSpearCE : BulletCE
{
    public override void Impact(Thing hitThing)
    {
        base.Impact(hitThing);
        if (hitThing is Pawn { Dead: false } pawn)
        {
            HealthUtility.AdjustSeverity(pawn, FleshHiveDefOf.FH_BoneSpear, 1f);
        }
    }
}
