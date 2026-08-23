using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class VerbProperties_BastionmeldTrackingLaser : VerbProperties_TrackingLaser
{
    public VerbProperties_BastionmeldTrackingLaser()
    {
        this.verbClass = typeof(Verb_BastionmeldTrackingLaser);
    }

    public ThingDef persistentTargetMote;
}
