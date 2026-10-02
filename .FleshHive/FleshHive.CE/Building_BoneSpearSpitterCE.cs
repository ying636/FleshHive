using CombatExtended;
using RimWorld;
using Verse;
using Verse.Sound;

namespace FleshHive.CE;

public class Building_BoneSpearSpitterCE : Building_TurretGunCE
{
    public override bool Active => base.Active && (this.TryGetComp<CompRefuelable>()?.HasFuel ?? true);

    public override string GetInspectString()
    {
        return base.GetInspectString() + "\n" + "FH_BoneSpearSpitter_RefuelCost".Translate();
    }

    public override LocalTargetInfo TryFindNewTarget()
    {
        bool hadTarget = CurrentTarget.IsValid;
        LocalTargetInfo target = base.TryFindNewTarget();
        if (!hadTarget && target.IsValid && Map != null)
        {
            SoundDef.Named("Pawn_Fleshbeast_Bulbfreak_Call").PlayOneShot(new TargetInfo(Position, Map));
        }
        return target;
    }
}
