using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_HarnessedFleshmassHeartTransform : CompProperties
{
    public CompProperties_HarnessedFleshmassHeartTransform()
    {
        compClass = typeof(CompHarnessedFleshmassHeartTransform);
    }

    public PawnKindDef titanKind = null!;

    public float nutritionCost = 1000f;

    public string iconPath = string.Empty;
}
