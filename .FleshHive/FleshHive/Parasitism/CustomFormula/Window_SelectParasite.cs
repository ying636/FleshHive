using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Window_SelectParasite : Window
{
    public Window_SelectParasite(Thing hive, FormulaMaterialCategory_Parasite category, UnitDef selectedUnit)
    {
        this.category = category;
        this.selectedUnit = selectedUnit;
        this.doCloseX = true;
        this.forcePause = false;
        this.focusWhenOpened = false;
        this.preventCameraMotion = false;

        hostBodySize = selectedUnit?.kind?.race?.race?.baseBodySize ?? 1f;
        capacity = Mathf.Min((int)(hostBodySize + 1), 14);
        usedSpace = 0;
        foreach (FormulaMaterial m in category.ChosenMaterials)
        {
            if (m is FormulaMaterial_Parasite p && p.Props != null)
            {
                usedSpace += p.Props.cost;
            }
        }

        Dictionary<PawnKindDef, UnitDef> kindToUnit = new Dictionary<PawnKindDef, UnitDef>();
        HashSet<PawnKindDef> producibleKinds = new HashSet<PawnKindDef>();
        CompHiveSpawner spawner = hive.TryGetComp<CompHiveSpawner>();
        if (spawner != null)
        {
            foreach (SpawnCategoryDef spawnCategory in spawner.GetUnitCategories())
            {
                foreach (UnitDef unitDef in spawnCategory.units)
                {
                    if (unitDef.kind != null && unitDef.IsUnlocked(spawner))
                    {
                        producibleKinds.Add(unitDef.kind);
                        kindToUnit[unitDef.kind] = unitDef;
                    }
                }
            }
        }

        choices = new List<FormulaMaterial_Parasite>();
        var allParasites = FormulaMaterial.GetAllAvailable();
        if (allParasites.TryGetValue("Parasite", out var list))
        {
            foreach (FormulaMaterial mat in list)
            {
                if (mat is FormulaMaterial_Parasite p && producibleKinds.Contains(p.parasiteKind))
                {
                    if (kindToUnit.TryGetValue(p.parasiteKind, out var unitDef))
                    {
                        p.sourceUnitDef = unitDef;
                    }
                    choices.Add(p);
                }
            }
        }
    }

    public override Vector2 InitialSize => new Vector2(450f, 520f);

    public override void PreClose()
    {
        base.PreClose();
        FormulaMaterialCategory_Parasite.NotifyWindowClosed();
    }

    public override void DoWindowContents(Rect inRect)
    {
        float curY = inRect.y + 5f;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x + 5f, curY, inRect.width - 10f, 30f), "HCF_SelectParasite".Translate());
        Text.Font = GameFont.Small;
        curY += 35f;

        Widgets.Label(new Rect(inRect.x + 5f, curY, inRect.width - 10f, 25f),
            "HCF_Parasite_CapacityInfo".Translate(usedSpace, capacity));
        curY += 28f;

        float listHeight = inRect.yMax - curY - 50f;
        if (listHeight < 100f) listHeight = 100f;

        Widgets.BeginScrollView(new Rect(inRect.x, curY, inRect.width, listHeight),
            ref scrollPos, new Rect(0f, 0f, inRect.width - 20f, choices.Count * 96f + 5f));

        float entryY = 0f;
        float entryWidth = inRect.width - 20f;

        foreach (FormulaMaterial_Parasite choice in choices)
        {
            DrawChoiceEntry(0f, ref entryY, entryWidth, choice);
        }

        if (choices.Count == 0)
        {
            Widgets.Label(new Rect(0f, entryY, entryWidth, 30f), "HCF_Parasite_NoChoices".Translate());
        }

        Widgets.EndScrollView();
        curY += listHeight + 5f;

        Text.Anchor = TextAnchor.MiddleCenter;
        Rect closeRect = new Rect(inRect.x + inRect.width / 2f - 80f, curY, 160f, 35f);
        if (Widgets.ButtonText(closeRect, "Close".Translate(), true, true, true))
        {
            Close();
        }
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawChoiceEntry(float entryX, ref float entryY, float entryWidth, FormulaMaterial_Parasite choice)
    {
        ParasitismCompProperties props = choice.Props;
        int cost = props?.cost ?? 1;
        bool hasSpace = capacity - usedSpace >= cost;

        Rect rowRect = new Rect(entryX + 5f, entryY, entryWidth - 10f, 90f);
        Widgets.DrawBox(rowRect);

        Rect iconRect = new Rect(rowRect.x + 5f, rowRect.y + 7f, 50f, 50f);
        Widgets.DefIcon(iconRect, choice.parasiteKind);

        float labelX = iconRect.xMax + 8f;
        float labelWidth = rowRect.width - labelX + rowRect.x - 38f;

        string costText = "ParasitismCapacityCost".Translate() + cost;
        string nameLine = choice.parasiteKind.LabelCap + "  (" + costText + ")";
        Widgets.Label(new Rect(labelX, rowRect.y + 3f, labelWidth, 22f), nameLine);

        if (props != null && !props.effect.NullOrEmpty())
        {
            bool prevWrap = Text.WordWrap;
            Text.WordWrap = true;
            Widgets.Label(new Rect(labelX, rowRect.y + 28f, labelWidth, 56f), props.effect);
            Text.WordWrap = prevWrap;
        }

        Rect addBtnRect = new Rect(rowRect.xMax - 30f, rowRect.y + (90f - 25f) / 2f, 25f, 25f);
        if (hasSpace)
        {
            if (Widgets.ButtonText(addBtnRect, "+", true, true, ColorLibrary.Green, true))
            {
                category.ChosenMaterials.Add(choice);
                category.SyncToFormula();
                Close();
            }
        }
        else
        {
            GUI.color = Color.grey;
            Widgets.ButtonText(addBtnRect, "+", true, true, Color.grey, false);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rowRect, "HCF_Parasite_NoSpace".Translate(capacity - usedSpace, cost));
        }

        entryY += 96f;
    }

    private readonly FormulaMaterialCategory_Parasite category;
    private readonly UnitDef selectedUnit;
    private readonly List<FormulaMaterial_Parasite> choices;
    private readonly float hostBodySize;
    private readonly int capacity;
    private readonly int usedSpace;
    private Vector2 scrollPos;
}
