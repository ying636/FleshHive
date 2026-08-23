using RimWorld;
using Verse;

namespace FleshHive;

public static class FleshHiveActivitySuppressionUtility
{
    public static bool TryGetSuppressionRate(Pawn pawn, out float rate)
    {
        // rate = 0f;
        // StatDef stat = StatDefOf.ActivitySuppressionRate;
        // if (stat != null)
        // {
        //     if (stat.Worker.IsDisabledFor(pawn))
        //     {
        //         return false;
        //     }
        //
        //     rate = stat.Worker.GetValue(pawn);
        //     return rate > 0f;
        // }
        //
        // rate = DefaultSuppressionRate;
        // return true;
        if (pawn == null)
        {
            rate = 0f;
            return false;
        }

        int intellectualLevel = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        int socialLevel = pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
        float psychicSensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
        rate = (BaseSuppressionRate + SuppressionRatePerIntellectualLevel * intellectualLevel + SuppressionRatePerSocialLevel * socialLevel) * psychicSensitivity;
        return rate > 0f;
    }

    // private const float DefaultSuppressionRate = 0.065f;
    private const float BaseSuppressionRate = 0.035f;
    private const float SuppressionRatePerIntellectualLevel = 0.004f;
    private const float SuppressionRatePerSocialLevel = 0.004f;
}
