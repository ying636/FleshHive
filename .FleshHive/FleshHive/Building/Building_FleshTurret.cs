using RimWorld;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class Building_FleshTurret : Building_TurretGun
{
    public override LocalTargetInfo TryFindNewTarget()
    {
        bool hadTarget = CurrentTarget.IsValid;
        LocalTargetInfo target = base.TryFindNewTarget();
        if (!hadTarget && target.IsValid && Map != null)
        {
            SoundDef.Named("Pawn_Fleshbeast_Attack_Spike").PlayOneShot(new TargetInfo(Position, Map));
        }

        return target;
    }
}
