using RimWorld;

namespace FleshHive;

public class Thought_FoodEaten_FleshAdapted : Thought_FoodEaten
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
