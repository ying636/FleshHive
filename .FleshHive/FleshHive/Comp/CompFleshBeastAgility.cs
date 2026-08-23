using Verse;

namespace FleshHive;

public class CompProperties_FleshBeastAgility : CompProperties
{
    public CompProperties_FleshBeastAgility()
    {
        compClass = typeof(CompFleshBeastAgility);
    }
}

public class CompFleshBeastAgility : ThingComp
{
    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (dinfo.Amount <= 0f || parent is not Pawn pawn || !FleshBeastKindUtility.IsSmall(pawn.kindDef))
        {
            return;
        }

        absorbed = pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hediff_Upgrade_Agility) == true
            && Rand.Chance(0.2f);
    }
}
