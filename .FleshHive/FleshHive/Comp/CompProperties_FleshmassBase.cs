using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_FleshmassBase : CompProperties
{
    public CompProperties_FleshmassBase()
    {
        compClass = typeof(CompFleshmassBase);
    }

    public GraphicData buildingOutlineGraphic;
    public IntVec2 size;
}

public class CompFleshmassBase : ThingComp
{
    private CompProperties_FleshmassBase Props => (CompProperties_FleshmassBase)props;

    private Graphic BaseGraphic => GetGraphic(false);

    private Graphic BuildingOutlineGraphic => Props.buildingOutlineGraphic?.Graphic;

    private Graphic OutlineGraphic => GetGraphic(true);

    public override void PostDraw()
    {
        DrawAt(parent.DrawPos);
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        Rot4 rotation = Rot4.North;
        Vector3 center = parent.DrawPos;
        Printer_Plane.PrintPlane(layer, center + new Vector3(0f, OutlineYOffset, 0f), OutlineGraphic.drawSize, OutlineGraphic.MatAt(rotation, parent), 0f, false, null, null, 0.01f, 0f);
        Printer_Plane.PrintPlane(layer, center + new Vector3(0f, BaseYOffset, 0f), BaseGraphic.drawSize, BaseGraphic.MatAt(rotation, parent), 0f, false, null, null, 0.01f, 0f);
        if (BuildingOutlineGraphic != null)
        {
            Printer_Plane.PrintPlane(layer, center + new Vector3(0f, BuildingOutlineYOffset, 0f), BuildingOutlineGraphic.drawSize, BuildingOutlineGraphic.MatAt(rotation, parent), 0f, false, null, null, 0.01f, 0f);
        }
    }

    private void DrawAt(Vector3 drawPos)
    {
        Rot4 rotation = Rot4.North;
        OutlineGraphic.Draw(drawPos.WithYOffset(OutlineYOffset), rotation, parent, 0f);
        BaseGraphic.Draw(drawPos.WithYOffset(BaseYOffset), rotation, parent, 0f);
        BuildingOutlineGraphic?.Draw(drawPos.WithYOffset(BuildingOutlineYOffset), rotation, parent, 0f);
    }

    private Graphic GetGraphic(bool outline)
    {
        return GraphicDatabase.Get<Graphic_Single>(GetTexturePath(outline), ShaderDatabase.Cutout, GetGraphicDrawSize(), Color.white);
    }

    private Vector2 GetGraphicDrawSize()
    {
        IntVec2 cellSize = GetGraphicCellSize();
        return new Vector2(cellSize.x + 1.3f, cellSize.z + 1.3f);
    }

    private string GetTexturePath(bool outline)
    {
        IntVec2 cellSize = GetGraphicCellSize();
        string path = $"Things/Building/FleshmassBase_{cellSize.x}x{cellSize.z}";
        if (ShouldUseVariant(cellSize))
        {
            path += parent.thingIDNumber % 2 == 0 ? "A" : "B";
        }

        if (outline)
        {
            path += "_Outline";
        }

        return path;
    }

    private IntVec2 GetGraphicCellSize()
    {
        IntVec2 size = Props.size;
        if (parent.Rotation == Rot4.East || parent.Rotation == Rot4.West)
        {
            return new IntVec2(size.z, size.x);
        }

        return size;
    }

    private bool ShouldUseVariant(IntVec2 cellSize)
    {
        return cellSize.x == cellSize.z && cellSize.x >= 1 && cellSize.x <= 3;
    }

    private const float OutlineYOffset = -0.45f;
    private const float BaseYOffset = -0.015f;
    private const float BuildingOutlineYOffset = -0.005f;
}
