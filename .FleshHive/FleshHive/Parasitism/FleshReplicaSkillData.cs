using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class FleshReplicaSkillData : UnitSkillData
{
    public FleshReplicaSkillData()
    {
        useFixedSkill = true;
    }

    public override bool TryGetLevel(Pawn pawn, UnitComp comp, SkillDef skillDef, out int level)
    {
        if (TryGetHost(pawn, comp, out Pawn host) && host.skills != null)
        {
            level = host.skills.GetSkill(skillDef).GetLevel();
            return true;
        }

        level = 0;
        return true;
    }

    public override bool TryGetPassion(Pawn pawn, UnitComp comp, SkillDef skillDef, out Passion passion)
    {
        if (TryGetHost(pawn, comp, out Pawn host) && host.skills != null)
        {
            passion = host.skills.GetSkill(skillDef).passion;
            return true;
        }

        passion = Passion.None;
        return true;
    }

    public override bool WorkIsDisabled(Pawn pawn, UnitComp comp, WorkTypeDef work)
    {
        if (work.defName is "Warden" or "Research")
        {
            return true;
        }

        return !TryGetHost(pawn, comp, out Pawn host) || host.WorkTypeIsDisabled(work);
    }

    private static bool TryGetHost(Pawn pawn, UnitComp comp, out Pawn host)
    {
        host = null!;
        FleshReplicaUnit? replica = pawn as FleshReplicaUnit ?? comp?.parent as FleshReplicaUnit;
        if (replica?.Active != true || replica.Host?.Destroyed != false)
        {
            return false;
        }

        host = replica.Host;
        return true;
    }
}
