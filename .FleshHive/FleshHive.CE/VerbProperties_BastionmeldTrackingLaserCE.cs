using Verse;

namespace FleshHive.CE;

public class VerbProperties_BastionmeldTrackingLaserCE : VerbProperties_BastionmeldTrackingLaser
{
    public VerbProperties_BastionmeldTrackingLaserCE()
    {
        verbClass = typeof(Verb_BastionmeldTrackingLaserCE);
    }

    public ThingDef ceProjectile = null!;
    public float ceFireChancePerDamageTick;
}
