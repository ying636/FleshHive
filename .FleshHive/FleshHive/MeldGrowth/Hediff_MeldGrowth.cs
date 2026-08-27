using UnityEngine;
using Verse;

namespace FleshHive;

public class Hediff_MeldGrowth : HediffWithComps
{
    public static int MaximumLevel => 5;

    public int Level => Mathf.Clamp(Mathf.RoundToInt(Severity), 1, MaximumLevel);

    public bool CanUpgrade => Level < MaximumLevel;

    public override string LabelInBrackets => "FH_MeldGrowth_Level".Translate(Level, MaximumLevel);

    public override string TipStringExtra => "FH_MeldGrowth_Effects".Translate(
        Level,
        MaximumLevel,
        Level,
        Level * 100,
        Level * 25,
        Level * 50,
        Level * 20,
        Level * 15);

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
        {
            return false;
        }

        Severity = Level + 1;
        return true;
    }
}
