namespace FleshHive.CE;

public class CompProperties_AbilityFleshPulseCE : CompProperties_AbilityBastionmeldFleshPulse
{
    public CompProperties_AbilityFleshPulseCE()
    {
        compClass = typeof(CompAbilityEffect_FleshPulseCE);
    }

    public int damageAmount = 150;
}
