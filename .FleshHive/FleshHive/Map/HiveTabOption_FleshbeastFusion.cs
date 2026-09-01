using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_FleshbeastFusion : HiveTabOption_FleshHive
{
    public override bool CanShow(HiveRaceCategoryDef def)
    {
        Map? map = Find.CurrentMap;
        return FleshHiveDefOf.FH_Research_FleshFusion.IsFinished
            && map != null && (map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshHive)
                .Concat(map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest)))
            .Any(thing => thing.Spawned && thing.Faction == Faction.OfPlayer);
    }

    public override void Draw(List<Pawn> pawns, HiveRaceCategoryDef def, Rect inRect)
    {
        if (DrawHungryIfNeeded(inRect))
        {
            return;
        }

        CompFleshHiveUnitFuser? fuser = FindFleshHiveFuser();
        if (fuser == null)
        {
            DrawUnavailableFuser(inRect);
            return;
        }

        if (currentFuser != fuser)
        {
            currentFuser = fuser;
            lastCacheTick = -1;
        }

        int currentTick = Find.TickManager.TicksGame;
        if (lastCacheTick < 0 || currentTick - lastCacheTick >= MaterialCacheInterval)
        {
            fuser.Cache();
            lastCacheTick = currentTick;
        }

        Rect contentRect = inRect.ContractedBy(10f, 8f);
        DrawHeader(new Rect(contentRect.x, contentRect.y, contentRect.width, HeaderHeight), fuser);
        DrawDescription(new Rect(contentRect.x, contentRect.y + HeaderHeight, contentRect.width, DescriptionHeight));
        DrawRecipe(new Rect(contentRect.x, contentRect.y + HeaderHeight + DescriptionHeight, contentRect.width, RecipeHeight), fuser);

        float templatesY = contentRect.y + HeaderHeight + DescriptionHeight + RecipeHeight + SectionGap;
        DrawTemplates(new Rect(contentRect.x, templatesY, contentRect.width, contentRect.yMax - templatesY), fuser);
    }

    private void DrawUnavailableFuser(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect.ContractedBy(24f), "FH_Fusion_FuserUnavailable".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawHeader(Rect rect, CompFleshHiveUnitFuser fuser)
    {
        Widgets.DrawBoxSolid(rect, HeaderBackgroundColor);

        Rect resultRect = new Rect(rect.x + 10f, rect.y + 10f, ResultPreviewSize, ResultPreviewSize);
        DrawResultPreview(resultRect, fuser, fuser.CurrentFusion, fuser.ResultKnown);

        Rect modeRect = new Rect(resultRect.xMax + 12f, rect.y + 2f, 180f, 32f);
        string modeLabel = (fuser.LargeFusionMode ? "FH_Fusion_SlotLarge" : "FH_Fusion_SlotMedium").Translate();
        if (Widgets.ButtonText(modeRect, modeLabel))
        {
            fuser.SetLargeFusionMode(!fuser.LargeFusionMode);
        }

        Rect detailsRect = new Rect(modeRect.x, modeRect.yMax + 8f, rect.width - ResultPreviewSize - ActionPanelWidth - 44f, rect.height - modeRect.height - 18f);
        Widgets.DrawBoxSolid(detailsRect, DetailsBackgroundColor);
        DrawFusionDetails(detailsRect.ContractedBy(8f, 5f), fuser);

        Rect fuseRect = new Rect(rect.xMax - ActionPanelWidth, rect.yMax - 52f, ActionPanelWidth - 12f, 36f);
        AcceptanceReport report = CanStartFusion(fuser);
        if (!report.Accepted && !report.Reason.NullOrEmpty())
        {
            TooltipHandler.TipRegion(fuseRect, report.Reason);
        }
        if (Widgets.ButtonText(fuseRect, "FH_Fusion_Start".Translate(), true, true,
                report.Accepted ? ColorLibrary.SkyBlue : Color.grey, report.Accepted))
        {
            AcceptanceReport startReport = fuser.TryStartFusion();
            if (!startReport.Accepted && !startReport.Reason.NullOrEmpty())
            {
                Messages.Message(startReport.Reason, MessageTypeDefOf.RejectInput, false);
            }
        }
    }

    private void DrawFusionDetails(Rect rect, CompFleshHiveUnitFuser fuser)
    {
        string resultLabel = fuser.ResultKnown && fuser.CurrentFusion != null
            ? GetFusionResultLabel(fuser.CurrentFusion)
            : "FH_Fusion_Unknown".Translate().ToString();
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 22f), "FH_Fusion_Result".Translate(resultLabel));

        string materialRequirement = fuser.LargeFusionMode
            ? "FH_Fusion_LargeMaterialRequirement".Translate(
                "FH_Fusion_SlotMedium".Translate(),
                "FH_Fusion_SlotSmall".Translate())
            : "FH_Fusion_MaterialRequirement".Translate(
                "FH_Fusion_SlotSmall".Translate(), fuser.MaterialCount);
        Widgets.Label(new Rect(rect.x, rect.y + 23f, rect.width, 22f), materialRequirement);

        string time = fuser.CurrentFusion == null
            ? "--"
            : (fuser.CurrentFusion.progressTick / (float)GenDate.TicksPerDay).ToString("0.##");
        Widgets.Label(new Rect(rect.x, rect.y + 46f, rect.width * 0.5f, 22f), "FH_Fusion_Time".Translate(time));

        string value = fuser.ResultKnown && fuser.CurrentResultKind != null
            ? fuser.CurrentResultKind.race.GetStatValueAbstract(StatDefOf.MarketValue).ToStringMoney()
            : "--";
        Widgets.Label(new Rect(rect.x + rect.width * 0.5f, rect.y + 46f, rect.width * 0.5f, 22f), "FH_Fusion_Value".Translate(value));

        if (!fuser.CurrentModeReport.Accepted)
        {
            GUI.color = ColorLibrary.RedReadable;
            Widgets.Label(new Rect(rect.x, rect.y + 69f, rect.width, 22f), fuser.CurrentModeReport.Reason);
            GUI.color = Color.white;
        }
    }

    private void DrawDescription(Rect rect)
    {
        GUI.color = DescriptionColor;
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect.ContractedBy(0f, 4f), "FH_Fusion_Description".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
    }

    private void DrawRecipe(Rect rect, CompFleshHiveUnitFuser fuser)
    {
        Widgets.DrawBoxSolid(rect, RecipeBackgroundColor);
        DrawCurrentRecipe(rect, fuser);
    }

    private void DrawCurrentRecipe(Rect rect, CompFleshHiveUnitFuser fuser)
    {
        float sequenceWidth = fuser.MaterialCount * MaterialSlotSize + (fuser.MaterialCount - 1) * MaterialGap + ResultGap + MaterialSlotSize;
        float totalWidth = sequenceWidth + ResultDetailsGap + ResultDetailsWidth;
        float x = rect.x + Mathf.Max(16f, (rect.width - totalWidth) / 2f);
        float y = rect.y + 22f;

        for (int i = 0; i < fuser.MaterialCount; i++)
        {
            Rect slotRect = new Rect(x, y, MaterialSlotSize, MaterialSlotSize);
            DrawMaterialSlot(slotRect, fuser, i);
            x = slotRect.xMax;
            if (i < fuser.MaterialCount - 1)
            {
                DrawOperator(new Rect(x + 4f, y + 13f, 24f, 30f), "+");
                x += MaterialGap;
            }
        }

        DrawOperator(new Rect(x + 10f, y + 13f, 36f, 30f), "->");
        x += ResultGap;
        Rect resultRect = new Rect(x, y, MaterialSlotSize, MaterialSlotSize);
        DrawResultPreview(resultRect, fuser, fuser.CurrentFusion, fuser.ResultKnown);
        DrawResultDetails(new Rect(resultRect.xMax + ResultDetailsGap, y + 2f, ResultDetailsWidth, 92f),
            fuser.CurrentFusion, fuser.ResultKnown);

        string resultLabel = fuser.ResultKnown && fuser.CurrentFusion != null
            ? GetFusionResultLabel(fuser.CurrentFusion)
            : "FH_Fusion_Unknown".Translate().ToString();
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(resultRect.x - 28f, resultRect.yMax + 4f, resultRect.width + 56f, 24f), resultLabel);
        Text.Anchor = TextAnchor.UpperLeft;

        if (!fuser.ResultKnown && fuser.CurrentFusion != null)
        {
            GUI.color = Color.grey;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 10f, rect.yMax - 28f, rect.width - 20f, 22f), "FH_Fusion_ResultHint".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
    }

    private void DrawMaterialSlot(Rect rect, CompFleshHiveUnitFuser fuser, int index)
    {
        DrawFrame(rect);
        Thing? material = fuser.materials.TryGetValue(index, out Thing thing) ? thing : null;
        if (material != null)
        {
            Widgets.ThingIcon(rect.ContractedBy(6f), material);
            TooltipHandler.TipRegion(rect, material.LabelCap);
        }
        else
        {
            Widgets.DrawTextureFitted(rect.ContractedBy(8f), FHUtitly.RemoveFromPlatform, 1f);
            TooltipHandler.TipRegion(rect, "FH_Fusion_SelectMaterial".Translate());
        }

        if (Widgets.ButtonInvisible(rect))
        {
            ShowMaterialMenu(fuser, index);
        }

        string label = material != null
            ? material.LabelCap.ToString()
            : (fuser.LargeFusionMode ? "FH_Fusion_SlotMedium" : "FH_Fusion_SlotSmall").Translate().ToString();
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(rect.x - 20f, rect.yMax + 4f, rect.width + 40f, 24f), label);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawTemplates(Rect rect, CompFleshHiveUnitFuser fuser)
    {
        List<(FusionDef Fusion, IReadOnlyList<ThingDef> Materials)> recipes = GetDiscoveredRecipes(fuser);
        DrawLineHorizontal(rect.x, rect.y, rect.width);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, rect.y + 8f, rect.width, 30f), "FH_Fusion_SavedTemplates".Translate());
        Text.Font = GameFont.Small;

        Rect outRect = new Rect(rect.x, rect.y + 42f, rect.width, rect.height - 42f);
        float viewHeight = Mathf.Max(outRect.height + 1f, recipes.Count * (TemplateRowHeight + TemplateRowGap));
        Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref templateScrollPosition, viewRect);

        if (recipes.Count == 0)
        {
            GUI.color = Color.grey;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 0f, viewRect.width, 70f), "FH_Fusion_NoTemplates".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            (FusionDef fusion, IReadOnlyList<ThingDef> materials) = recipes[i];
            Rect rowRect = new Rect(0f, i * (TemplateRowHeight + TemplateRowGap), viewRect.width, TemplateRowHeight);
            DrawTemplateRow(rowRect, fuser, fusion, materials);
        }

        Widgets.EndScrollView();
    }

    private void DrawTemplateRow(Rect rect, CompFleshHiveUnitFuser fuser, FusionDef fusion, IReadOnlyList<ThingDef> materialDefs)
    {
        Widgets.DrawBoxSolid(rect, TemplateBackgroundColor);
        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);

        string templateLabel = GetFusionResultLabel(fusion);

        Rect labelRect = new Rect(rect.x + 10f, rect.y + 8f, TemplateDetailsWidth, rect.height - 16f);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(labelRect.x, labelRect.y, labelRect.width, 28f), templateLabel);
        Text.Font = GameFont.Small;
        DrawResultDetails(new Rect(labelRect.x, labelRect.y + 34f, labelRect.width, rect.height - 42f), fusion, true);

        float x = labelRect.xMax + 12f;
        float y = rect.y + (rect.height - TemplateSlotSize) / 2f;
        foreach (ThingDef materialDef in materialDefs)
        {
            Rect slotRect = new Rect(x, y, TemplateSlotSize, TemplateSlotSize);
            DrawFrame(slotRect);
            Def iconDef = PawnKindsByRace.TryGetValue(materialDef, out PawnKindDef kind) ? kind : materialDef;
            Widgets.DefIcon(slotRect.ContractedBy(5f), iconDef);
            TooltipHandler.TipRegion(slotRect, iconDef.LabelCap);
            x = slotRect.xMax + 8f;
            DrawOperator(new Rect(x, y + 10f, 20f, 26f), "+");
            x += 26f;
        }

        DrawOperator(new Rect(x, y + 10f, 32f, 26f), "->");
        x += 40f;
        Rect resultRect = new Rect(x, y, TemplateSlotSize, TemplateSlotSize);
        DrawTemplateResult(resultRect, fuser, fusion);
        bool canApply = TryMatchTemplate(fuser, materialDefs, out _);
        Rect applyRect = new Rect(rect.xMax - 112f, rect.yMax - 38f, 82f, 28f);
        if (!canApply)
        {
            TooltipHandler.TipRegion(applyRect, "FH_Fusion_TemplateMissingMaterials".Translate());
        }
        if (Widgets.ButtonText(applyRect, "FH_Fusion_ApplyTemplate".Translate(), true, true,
                canApply ? ColorLibrary.SkyBlue : Color.grey, canApply))
        {
            ApplyTemplate(fuser, materialDefs);
        }

    }

    private void DrawTemplateResult(Rect rect, CompFleshHiveUnitFuser fuser, FusionDef fusion)
    {
        DrawFrame(rect);
        Def? resultDef = GetFusionResultDef(fusion);
        if (resultDef != null)
        {
            Widgets.DefIcon(rect.ContractedBy(5f), resultDef);
            TooltipHandler.TipRegion(rect, resultDef.LabelCap);
        }
        else
        {
            Widgets.DrawBoxSolid(rect.ContractedBy(4f), UnknownResultBackgroundColor);
            Widgets.DrawTextureFitted(rect.ContractedBy(7f), fuser.GetFusionSilhouetteTex(fusion.materials.Count >= 3), 1f);
        }
    }

    private void DrawResultPreview(Rect rect, CompFleshHiveUnitFuser fuser, FusionDef? fusion, bool reveal)
    {
        DrawFrame(rect);
        Def? resultDef = fusion == null ? null : GetFusionResultDef(fusion);
        if (reveal && resultDef != null)
        {
            Widgets.DefIcon(rect.ContractedBy(7f), resultDef);
            TooltipHandler.TipRegion(rect, resultDef.LabelCap);
        }
        else
        {
            Widgets.DrawBoxSolid(rect.ContractedBy(4f), UnknownResultBackgroundColor);
            Widgets.DrawTextureFitted(rect.ContractedBy(7f), fuser.GetFusionSilhouetteTex(fuser.LargeFusionMode), 1f);
        }
    }

    private void DrawResultDetails(Rect rect, FusionDef? fusion, bool reveal)
    {
        string unknown = "FH_Fusion_Unknown".Translate();
        string typeLabel = unknown;
        string traitLabel = unknown;
        string traitDescription = "";
        if (reveal && fusion?.results.FirstOrDefault()?.result is FusionResult result)
        {
            if (result is FusionResult_PawnKind pawnResult && pawnResult.kind != null)
            {
                typeLabel = pawnResult.kind.LabelCap;
                List<HediffDef> traits = result is FusionResult_PawnKindWithHediffs traitResult
                    ? traitResult.hediffs.Where(hediff => hediff != null).ToList()
                    : new List<HediffDef>();
                traitLabel = traits.Any()
                    ? string.Join(", ", traits.Select(hediff => hediff.LabelCap))
                    : "FH_Fusion_NoTrait".Translate();
                traitDescription = traits.Any()
                    ? string.Join("\n", traits.Select(hediff => hediff.description))
                    : "";
            }
            else
            {
                typeLabel = "FH_Fusion_NotFleshbeast".Translate();
                traitLabel = "FH_Fusion_NoTrait".Translate();
            }
        }

        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 22f), "FH_Fusion_ResultType".Translate(typeLabel));
        Widgets.Label(new Rect(rect.x, rect.y + 22f, rect.width, 22f), "FH_Fusion_ResultTraits".Translate(traitLabel));
        if (!traitDescription.NullOrEmpty())
        {
            Rect descriptionRect = new Rect(rect.x, rect.y + 44f, rect.width, Mathf.Max(22f, rect.height - 44f));
            Widgets.Label(descriptionRect, traitDescription);
            TooltipHandler.TipRegion(descriptionRect, traitDescription);
        }
        Text.Font = GameFont.Small;
    }

    private void DrawFrame(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, SlotBackgroundColor);
        Color color = GUI.color;
        GUI.color = FrameColor;
        Widgets.DrawBox(rect, 2);
        GUI.color = color;
    }

    private void DrawOperator(Rect rect, string label)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = OperatorColor;
        Widgets.Label(rect, label);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private void DrawLineHorizontal(float x, float y, float length)
    {
        Widgets.DrawBoxSolid(new Rect(x, y, length, 1f), LineColor);
    }

    private void ShowMaterialMenu(CompFleshHiveUnitFuser fuser, int index)
    {
        fuser.Cache();
        List<FloatMenuOption> options = new List<FloatMenuOption>
        {
            new FloatMenuOption("FH_Fusion_ClearSlot".Translate(), () => fuser.SetMaterial(index, null))
        };
        options.AddRange(GetAvailablePawns(fuser, index)
            .Select(pawn => new FloatMenuOption(pawn.LabelCap.ToString(), () => fuser.SetMaterial(index, pawn), pawn.def)));
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void ApplyTemplate(CompFleshHiveUnitFuser fuser, IReadOnlyList<ThingDef> materialDefs)
    {
        if (!TryMatchTemplate(fuser, materialDefs, out List<Pawn> materials))
        {
            Messages.Message("FH_Fusion_TemplateMissingMaterials".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        fuser.SetLargeFusionMode(materialDefs.Count >= 3);
        fuser.ClearMaterials();
        for (int i = 0; i < materials.Count; i++)
        {
            fuser.SetMaterial(i, materials[i]);
        }
    }

    private bool TryMatchTemplate(CompFleshHiveUnitFuser fuser, IReadOnlyList<ThingDef> materialDefs, out List<Pawn> materials)
    {
        List<Pawn> pool = GetAvailablePawns(fuser);
        materials = new List<Pawn>();
        for (int i = 0; i < materialDefs.Count; i++)
        {
            ThingDef materialDef = materialDefs[i];
            Pawn? pawn = pool.FirstOrDefault(candidate => candidate.def == materialDef
                && fuser.CanAcceptMaterial(i, candidate));
            if (pawn == null)
            {
                return false;
            }

            materials.Add(pawn);
            pool.Remove(pawn);
        }

        return true;
    }

    private List<Pawn> GetAvailablePawns(CompFleshHiveUnitFuser fuser, int? slotIndex = null)
    {
        MapComponent_Unit? unitMap = fuser.parent.Map?.GetComponent<MapComponent_Unit>();
        return fuser.AvailableThings
            .OfType<Pawn>()
            .Where(pawn => pawn != null && !pawn.Destroyed
                && (!slotIndex.HasValue || fuser.CanAcceptMaterial(slotIndex.Value, pawn))
                && (unitMap == null || !unitMap.FusionRequirements.ContainsKey(pawn))
                && (unitMap == null || !unitMap.UnitsInSpecialState.Contains(pawn)))
            .Distinct()
            .OrderBy(pawn => pawn.kindDef.label)
            .ThenBy(pawn => pawn.thingIDNumber)
            .ToList();
    }

    private AcceptanceReport CanStartFusion(CompFleshHiveUnitFuser fuser)
    {
        if (!fuser.CurrentModeReport.Accepted)
        {
            return fuser.CurrentModeReport;
        }
        if (fuser.CurrentFusion == null || fuser.materials.Values.Count(thing => thing != null) != fuser.MaterialCount)
        {
            return "FH_Fusion_NoValidFusion".Translate();
        }
        return true;
    }

    private List<(FusionDef Fusion, IReadOnlyList<ThingDef> Materials)> GetDiscoveredRecipes(CompFleshHiveUnitFuser fuser)
    {
        List<(FusionDef Fusion, IReadOnlyList<ThingDef> Materials)> recipes = new();
        foreach (FusionDefData data in GameComponent_UnitGroup.Instance.fusionDatas)
        {
            FusionDef? fusion = data?.def;
            if (fusion == null || fusion != fuser.Props.defaultFusion
                && fusion.category != fuser.Props.category
                && fusion.category != fuser.Props.largeCategory)
            {
                continue;
            }

            if (fusion.isDefault)
            {
                foreach (FusionRecipe recipe in data.Recipes.Where(recipe => !recipe.materials.NullOrEmpty()))
                {
                    recipes.Add((fusion, recipe.materials));
                }
                continue;
            }

            if (data.unlocked)
            {
                List<ThingDef> materialDefs = GetFusionMaterialDefs(data);
                if (materialDefs.Any())
                {
                    recipes.Add((fusion, materialDefs));
                }
            }
        }

        return recipes;
    }

    private List<ThingDef> GetFusionMaterialDefs(FusionDefData data)
    {
        List<ThingDef> materialDefs = new();
        foreach (FusionMaterial material in data.def.materials)
        {
            if (material is FusionMaterial_Def fixedMaterial && fixedMaterial.def != null)
            {
                materialDefs.Add(fixedMaterial.def);
            }
            else if (material is FusionMaterial_Random randomMaterial
                && data.datas.TryGetValue(randomMaterial.id, out ThingDef randomDef))
            {
                materialDefs.Add(randomDef);
            }
        }

        return materialDefs;
    }

    private Def? GetFusionResultDef(FusionDef fusion)
    {
        FusionResult? result = fusion.results.FirstOrDefault()?.result;
        if (result is FusionResult_PawnKind pawnResult)
        {
            return pawnResult.kind;
        }
        return result is FusionResult_Thing thingResult ? thingResult.results.FirstOrDefault()?.thingDef : null;
    }

    private string GetFusionResultLabel(FusionDef fusion)
    {
        if (fusion.isDefault)
        {
            return "FH_Fusion_Failed".Translate();
        }
        return fusion.results.FirstOrDefault()?.result?.label ?? fusion.LabelCap;
    }

    private CompFleshHiveUnitFuser? FindFleshHiveFuser()
    {
        Map? map = Find.CurrentMap;
        if (map == null)
        {
            return null;
        }

        IEnumerable<Thing> hives = map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshHive)
            .Concat(map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest));
        return hives.Where(thing => thing.Faction == Faction.OfPlayer)
            .OfType<ThingWithComps>()
            .Select(thing => thing.TryGetComp<CompFleshHiveUnitFuser>())
            .FirstOrDefault(comp => comp != null);
    }

    private Vector2 templateScrollPosition;
    private CompFleshHiveUnitFuser? currentFuser;
    private int lastCacheTick = -1;

    private const float HeaderHeight = 142f;
    private const float DescriptionHeight = 66f;
    private const float RecipeHeight = 152f;
    private const float SectionGap = 8f;
    private const float ResultPreviewSize = 112f;
    private const float ActionPanelWidth = 190f;
    private const float MaterialSlotSize = 64f;
    private const float MaterialGap = 34f;
    private const float ResultGap = 54f;
    private const float ResultDetailsGap = 18f;
    private const float ResultDetailsWidth = 250f;
    private const float TemplateRowHeight = 138f;
    private const float TemplateRowGap = 6f;
    private const float TemplateSlotSize = 54f;
    private const float TemplateDetailsWidth = 280f;
    private const float ScrollbarWidth = 16f;
    private const int MaterialCacheInterval = 60;
    private static readonly Color HeaderBackgroundColor = new Color(0f, 0f, 0f, 0.12f);
    private static readonly Color DetailsBackgroundColor = new Color(0.22f, 0.23f, 0.16f, 0.88f);
    private static readonly Color RecipeBackgroundColor = new Color(0.17f, 0.17f, 0.17f, 0.96f);
    private static readonly Color TemplateBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.78f);
    private static readonly Color SlotBackgroundColor = new Color(0.06f, 0.08f, 0.08f, 0.95f);
    private static readonly Color UnknownResultBackgroundColor = new Color(0.02f, 0.035f, 0.035f);
    private static readonly Color DescriptionColor = new Color(0.72f, 0.72f, 0.72f);
    private static readonly Color FrameColor = new Color(0.2f, 0.82f, 0.86f);
    private static readonly Color OperatorColor = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color LineColor = new Color(0.42f, 0.47f, 0.5f, 0.55f);
    private static readonly Dictionary<ThingDef, PawnKindDef> PawnKindsByRace = DefDatabase<PawnKindDef>.AllDefsListForReading
        .Where(kind => kind.race != null)
        .GroupBy(kind => kind.race)
        .ToDictionary(group => group.Key, group => group.First());
}
