using RimWorld;
using Verse;

namespace FleshHive;

public class Projectile_FleshHiveBoneSpike : Bullet
{
    public override float ArmorPenetration
    {
        get
        {
            if (launcher is Pawn pawn && IsModBoneSpikeSkill(pawn)
                && pawn.Faction == Faction.OfPlayer
                && pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hediff_Upgrade_BoneSpikePenetration) == true)
            {
                return 0.9f;
            }

            return base.ArmorPenetration;
        }
    }

    private bool IsModBoneSpikeSkill(Pawn pawn)
    {
        return def == FleshHiveDefOf.FH_Spike_Fingerspike
                && pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Fingerspike) != null
            || def == FleshHiveDefOf.FH_Spike_Toughspike
                && pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Toughspike) != null
            || def == FleshHiveDefOf.FH_Spike_Whipspike
                && pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Whipspike) != null
            || def == FleshHiveDefOf.FH_Projectile_Spike_Paraspike
                && pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Paraspike) != null
            || def == FleshHiveDefOf.FH_Spike_Shatterspike
                && pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Shatterspike) != null;
    }

}
