using RimWorld;

namespace FleshHive;

public class Thought_FleshParasitism : Thought_Situational
{
    public override float MoodOffset()
    {
        return FleshAdaptationUtility.HasAdaptation(pawn) ? 0f : base.MoodOffset();
    }
}
