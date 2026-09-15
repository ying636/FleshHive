using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class PsychicRitualDef_SummonGiantFleshbeast : PsychicRitualDef_InvocationCircle
{
    public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
    {
        foreach (string issue in base.BlockingIssues(assignments, map))
        {
            yield return issue;
        }
        if (!requiredDiscovery.Discovered)
        {
            yield return $"{requiredDiscovery.LabelCap}: {"NotYetDiscovered".Translate()}";
        }
    }

    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        List<PsychicRitualToil> toils = base.CreateToils(psychicRitual, parent);
        toils.Add(new PsychicRitualToil_SummonGiantFleshbeast(InvokerRole));
        return toils;
    }

    public PawnKindDef summonKind = null!;
    public EntityCodexEntryDef requiredDiscovery = null!;
    public SimpleCurve escortPointsFromQualityCurve = null!;
}
