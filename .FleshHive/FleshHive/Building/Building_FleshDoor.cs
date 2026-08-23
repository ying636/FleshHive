using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Building_FleshDoor : Building_Door
{
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        DoorPreDraw();
        Comps_PostDraw();
        if (CanDrawMovers)
        {
            Graphic graphic = Graphic;
            Vector2 drawSize = def.graphicData.drawSize;
            Vector3 drawScaleFactor = new Vector3(drawSize.x / def.size.x, 1f, drawSize.y / def.size.z);
            DrawMovers(drawLoc, 0.45f * OpenPct, graphic, AltitudeLayer.BuildingOnTop.AltitudeFor(), drawScaleFactor, graphic.ShadowGraphic);
        }
    }
}
