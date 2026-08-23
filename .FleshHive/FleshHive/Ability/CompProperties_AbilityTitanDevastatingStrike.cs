using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityTitanDevastatingStrike : CompProperties_AbilityEffect
{
    public CompProperties_AbilityTitanDevastatingStrike()
    {
        compClass = typeof(CompAbilityEffect_TitanDevastatingStrike);
    }

    public HediffDef hediffDef = null!;
}
