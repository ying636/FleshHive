using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class PsychicRitualDef_SummonGiantFleshbeast : PsychicRitualDef_InvocationCircle
{
    public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph parent)
    {
        List<PsychicRitualToil> toils = base.CreateToils(psychicRitual, parent);
        toils.Add(new PsychicRitualToil_SummonGiantFleshbeast(InvokerRole));
        return toils;
    }

    public PawnKindDef summonKind = null!;
    public SimpleCurve escortPointsFromQualityCurve = null!;
}
