using System.Linq;
using UnityEngine;
using Verse;

namespace FleshHive;

public class PlaceWorker_FleshTurretRadius : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing? thing = null)
    {
        VerbProperties verb = def.building.turretGunDef.Verbs.First(properties => properties.defaultProjectile != null);
        if (verb.range > 0f)
        {
            GenDraw.DrawRadiusRing(loc, verb.range);
        }
        if (verb.minRange > 0f)
        {
            GenDraw.DrawRadiusRing(loc, verb.minRange);
        }
    }
}
