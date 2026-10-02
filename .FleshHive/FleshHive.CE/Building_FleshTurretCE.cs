using CombatExtended;
using RimWorld;
using Verse;
using Verse.Sound;

namespace FleshHive.CE;

public class Building_FleshTurretCE : Building_TurretGunCE
{
    public override bool Active => base.Active && (this.TryGetComp<CompRefuelable>()?.HasFuel ?? true);

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
