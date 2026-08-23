using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class Ability_FleshRebirth : Ability
{
    public Ability_FleshRebirth(Pawn pawn) : base(pawn)
    {
    }

    public Ability_FleshRebirth(Pawn pawn, AbilityDef def) : base(pawn, def)
    {
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (CanCooldown && OnCooldown && (!def.cooldownPerCharge || RemainingCharges == 0))
        {
            reason = "AbilityOnCooldown".Translate(CooldownTicksRemaining.ToStringTicksToPeriod()).Resolve();
            return true;
        }
        if (UsesCharges && RemainingCharges <= 0)
        {
            reason = "AbilityNoCharges".Translate();
            return true;
        }
        if (!comps.NullOrEmpty())
        {
            foreach (AbilityComp comp in comps)
            {
                if (comp.GizmoDisabled(out reason))
                {
                    return true;
                }
            }
        }

        AcceptanceReport canCast = CanCast;
        if (!canCast.Accepted)
        {
            reason = canCast.Reason;
            return true;
        }
        Lord lord = pawn.GetLord();
        if (lord != null)
        {
            AcceptanceReport acceptanceReport = lord.AbilityAllowed(this);
            if (!acceptanceReport)
            {
                reason = acceptanceReport.Reason;
                return true;
            }
        }
        if (!pawn.Drafted && def.disableGizmoWhileUndrafted && pawn.GetCaravan() == null && !DebugSettings.ShowDevGizmos)
        {
            reason = "AbilityDisabledUndrafted".Translate();
            return true;
        }
        if (pawn.DevelopmentalStage.Baby())
        {
            reason = "IsIncapped".Translate(pawn.LabelShort, pawn);
            return true;
        }
        if (pawn.Deathresting)
        {
            reason = "CommandDisabledDeathresting".Translate(pawn);
            return true;
        }
        if (def.casterMustBeCapableOfViolence && pawn.WorkTagIsDisabled(WorkTags.Violent))
        {
            reason = "IsIncapableOfViolence".Translate(pawn.LabelShort, pawn);
            return true;
        }
        if (!CanQueueCast)
        {
            reason = "AbilityAlreadyQueued".Translate();
            return true;
        }

        reason = null;
        return false;
    }
}
