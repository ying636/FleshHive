using RimWorld;

namespace FleshHive;

public class Thought_Memory_FleshAdapted : Thought_Memory
{
    public override float MoodOffset()
    {
        if (FleshAdaptationUtility.HasAdaptation(pawn))
        {
            return 0f;
        }
        return base.MoodOffset();
    }
}
