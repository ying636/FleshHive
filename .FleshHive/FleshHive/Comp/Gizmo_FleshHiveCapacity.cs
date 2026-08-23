using UnityEngine;
using Verse;

namespace FleshHive;

public class Gizmo_FleshHiveCapacity : Gizmo
{
    public Gizmo_FleshHiveCapacity(MapComponent_FleshHive mapComp)
    {
        this.mapComp = mapComp;
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
        Widgets.DrawWindowBackground(rect);

        int current = mapComp.CurrentHiveGroupCost;
        int limit = Mathf.Max(0, mapComp.HiveGroupCostLimit);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x + Padding, rect.y + 5f, 78f, 25f),
            "FH_FleshManagement_FleshbeastScale".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(new Rect(rect.xMax - 52f, rect.y + 5f, 44f, 25f), $"{current} / {limit}");
        Text.Anchor = TextAnchor.UpperLeft;

        DrawCapacityCells(new Rect(rect.x + Padding, rect.y + 36f, rect.width - Padding * 2f, 29f),
            current, limit);
        TooltipHandler.TipRegion(rect, "FH_FleshManagement_HiveCapacityTooltip".Translate(current, limit));
        return new GizmoResult(Mouse.IsOver(rect) ? GizmoState.Mouseover : GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        return Width;
    }

    private static void DrawCapacityCells(Rect rect, int current, int limit)
    {
        if (limit <= 0)
        {
            return;
        }

        int unitsPerCell = Mathf.Max(1, Mathf.CeilToInt(limit / (float)MaxCells));
        int cellCount = Mathf.CeilToInt(limit / (float)unitsPerCell);
        int filledCellCount = Mathf.Clamp(Mathf.CeilToInt(current / (float)unitsPerCell), 0, cellCount);
        for (int i = 0; i < cellCount; i++)
        {
            int column = i % CellsPerRow;
            int row = i / CellsPerRow;
            Rect cellRect = new Rect(
                rect.x + column * (CellSize + CellGap),
                rect.y + row * (CellSize + CellGap),
                CellSize,
                CellSize);
            Widgets.DrawBoxSolid(cellRect, i < filledCellCount ? FilledCellColor : EmptyCellColor);
        }
    }

    private const float GizmoHeight = 75f;
    private const float Width = 130f;
    private const float Padding = 8f;
    private const float CellSize = 7f;
    private const float CellGap = 2f;
    private const int CellsPerRow = 13;
    private const int MaxCells = 39;

    private static readonly Color FilledCellColor = new Color(0.86f, 0.03f, 0.08f);
    private static readonly Color EmptyCellColor = new Color(0.28f, 0.28f, 0.28f);

    private readonly MapComponent_FleshHive mapComp;
}
