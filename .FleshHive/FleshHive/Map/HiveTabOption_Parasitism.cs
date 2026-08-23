using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_Parasitism : HiveTabOption_FleshHive
{
    public override void Draw(List<Pawn> pawns, HiveRaceCategoryDef def, Rect inRect)
    {
        if (DrawHungryIfNeeded(inRect))
        {
            return;
        }

        Map? map = Find.CurrentMap;
        if (map == null)
        {
            return;
        }

        List<FleshParasitePod> vats = map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshParasiteVat)
            .OfType<FleshParasitePod>()
            .Where(vat => vat.Spawned && vat.Faction == Faction.OfPlayer)
            .OrderBy(vat => vat.Position.z)
            .ThenBy(vat => vat.Position.x)
            .ToList();
        if (selectedVat == null || !vats.Contains(selectedVat))
        {
            selectedVat = vats.FirstOrDefault();
        }

        Rect contentRect = inRect.ContractedBy(8f);
        Rect selectorRect = new Rect(contentRect.x, contentRect.y, SelectorWidth, contentRect.height);
        Rect vatRect = new Rect(selectorRect.xMax + PanelGap, contentRect.y,
            contentRect.width - SelectorWidth - PanelGap, contentRect.height);
        DrawVatSelector(selectorRect, vats);
        if (selectedVat == null)
        {
            DrawNoVats(vatRect);
            return;
        }

        selectedVat.Draw(vatRect);
    }

    private void DrawVatSelector(Rect rect, List<FleshParasitePod> vats)
    {
        Widgets.DrawMenuSection(rect);
        Rect innerRect = rect.ContractedBy(8f);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, HeaderHeight),
            "FH_Parasitism_VatSelector".Translate());
        Text.Font = GameFont.Small;

        Rect outRect = new Rect(innerRect.x, innerRect.y + HeaderHeight + 4f,
            innerRect.width, innerRect.height - HeaderHeight - 4f);
        float viewHeight = Mathf.Max(outRect.height + 1f, vats.Count * (VatRowHeight + VatRowGap));
        Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        for (int i = 0; i < vats.Count; i++)
        {
            DrawVatRow(new Rect(0f, i * (VatRowHeight + VatRowGap), viewRect.width, VatRowHeight), vats[i]);
        }
        Widgets.EndScrollView();
    }

    private void DrawVatRow(Rect rect, FleshParasitePod vat)
    {
        bool selected = selectedVat == vat;
        Widgets.DrawBoxSolid(rect, selected ? SelectedBackgroundColor : RowBackgroundColor);

        Rect iconRect = new Rect(rect.x + 6f, rect.y + 6f, VatIconSize, VatIconSize);
        Widgets.ThingIcon(iconRect, vat);
        Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y + 7f,
            rect.width - iconRect.width - 20f, 26f);
        Widgets.Label(labelRect, vat.LabelCap);

        string status = vat.start
            ? "FH_Parasitism_VatWorking".Translate((vat.progress / (float)vat.TickToParasite).ToStringPercent())
            : vat.curQuest != null
                ? "FH_Parasitism_VatQueued".Translate()
                : "FH_Parasitism_VatIdle".Translate();
        Text.Font = GameFont.Tiny;
        GUI.color = Color.grey;
        Widgets.Label(new Rect(labelRect.x, labelRect.yMax + 3f, labelRect.width, 22f), status);
        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        if (Widgets.ButtonInvisible(rect))
        {
            selectedVat = vat;
        }
        TooltipHandler.TipRegion(rect, vat.GetInspectString());

        if (selected)
        {
            Color color = GUI.color;
            GUI.color = SelectedBorderColor;
            Widgets.DrawBox(rect.ContractedBy(1f), 2);
            GUI.color = color;
        }
    }

    private void DrawNoVats(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = Color.grey;
        Widgets.Label(rect, "FH_Parasitism_NoVats".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private Vector2 scrollPosition;
    private FleshParasitePod? selectedVat;

    private const float SelectorWidth = 240f;
    private const float PanelGap = 10f;
    private const float HeaderHeight = 34f;
    private const float VatRowHeight = 70f;
    private const float VatRowGap = 6f;
    private const float VatIconSize = 58f;
    private const float ScrollbarWidth = 16f;
    private static readonly Color RowBackgroundColor = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color SelectedBackgroundColor = new Color32(70, 70, 70, 255);
    private static readonly Color SelectedBorderColor = new Color(0.2f, 0.75f, 0.85f, 0.95f);
}
