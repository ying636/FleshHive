using RimWorld;
using Verse;

namespace FleshHive;

public class StatPart_FleshHiveCapacity : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        val += req.Thing?.TryGetComp<CompHiveGroupCapacityProvider>()?.CapacityUpgradeBonus ?? 0;
    }

    public override string ExplanationPart(StatRequest req)
    {
        return null;
    }
}
