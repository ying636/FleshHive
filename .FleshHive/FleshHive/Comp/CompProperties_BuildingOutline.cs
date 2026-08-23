using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_BuildingOutline : CompProperties
{
    public CompProperties_BuildingOutline()
    {
        compClass = typeof(CompBuildingOutline);
    }

    public GraphicData graphicData;
}

public class CompBuildingOutline : ThingComp
{
    private CompProperties_BuildingOutline Props => (CompProperties_BuildingOutline)props;

    private Graphic OutlineGraphic => Props.graphicData?.Graphic;

    public override void PostDraw()
    {
        OutlineGraphic?.Draw(parent.DrawPos.WithYOffset(OutlineYOffset), parent.Rotation, parent, 0f);
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        Graphic graphic = OutlineGraphic;
        if (graphic == null)
        {
            return;
        }

        Printer_Plane.PrintPlane(layer, parent.DrawPos + new Vector3(0f, OutlineYOffset, 0f), graphic.drawSize, graphic.MatAt(parent.Rotation, parent), 0f, false, null, null, 0.01f, 0f);
    }

    private const float OutlineYOffset = -0.005f;
}
