using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FleshHive;

public class FleshHiveSite : Site
{
    public PawnKindDef motherKind;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref motherKind, "motherKind");
    }
}
