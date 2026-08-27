using System.Collections.Generic;
using Verse;

namespace FleshHive;

public class ModExtension_RangedDuty : DefModExtension
{
    public override IEnumerable<string> ConfigErrors()
    {
        if (minimumRange < 0f)
        {
            yield return "minimumRange must be non-negative";
        }

        if (maximumRange < minimumRange)
        {
            yield return "maximumRange must be greater than or equal to minimumRange";
        }
    }

    public float minimumRange;
    public float maximumRange;
}
