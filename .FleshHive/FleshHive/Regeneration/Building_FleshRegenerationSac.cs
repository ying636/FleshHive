using UnityEngine;
using Verse;

namespace FleshHive;

public class Building_FleshRegenerationSac : Building
{
    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        CompFleshRegenerationSac container = GetComp<CompFleshRegenerationSac>();
        if (container.units.Count > 0)
        {
            Vector3 pawnLoc = DrawPos;
            pawnLoc.y = container.HeldPawnDrawPos_Y;
            container.units[0].Drawer.renderer.DynamicDrawPhaseAt(
                phase, pawnLoc, Rot4.South, neverAimWeapon: true);
        }
        if (phase == DrawPhase.Draw)
        {
            Vector3 topLoc = drawLoc;
            topLoc.y = AltitudeLayer.MetaOverlays.AltitudeFor(0.9f);
            container.Props.topGraphicData.Graphic.Draw(topLoc, Rotation, this);
        }
    }
}
