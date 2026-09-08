using RimWorld;
using Verse;

namespace FleshHive;

public class Bullet_BoneSpear : Bullet
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        base.Impact(hitThing, blockedByShield);
        if (!blockedByShield && hitThing is Pawn pawn && !pawn.Dead)
        {
            HealthUtility.AdjustSeverity(pawn, FleshHiveDefOf.FH_BoneSpear, 1f);
        }
    }
}
