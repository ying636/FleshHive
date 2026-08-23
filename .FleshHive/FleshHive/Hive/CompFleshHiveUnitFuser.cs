using HiveCreatureFramework;
using RimWorld;
using HiveCreatureFramework;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class CompProperties_FleshHiveUnitFuser : CompPropertiesUnitFuser
{
    public CompProperties_FleshHiveUnitFuser()
    {
        compClass = typeof(CompFleshHiveUnitFuser);
    }

    public string largeCategory = "LargeFlesh";
}

public class CompFleshHiveUnitFuser : CompUnitFuser, IHivePage
{
    public new CompProperties_FleshHiveUnitFuser Props => (CompProperties_FleshHiveUnitFuser)props;

    public new bool CanShow => FleshHiveDefOf.FH_Research_FleshFusion.IsFinished;

    public Texture2D DreadmeldSilhouetteTex => dreadmeldSilhouetteTex ??= ContentFinder<Texture2D>.Get("UI/CodexEntries/Dreadmeld_Silhouette");

    public bool LargeFusionMode => largeFusionMode;

    public int MaterialCount => RequiredMaterialCount;

    public FusionDef? CurrentFusion => cachedFusion;

    public bool ResultKnown => know;

    public PawnKindDef? CurrentResultKind => ResultKind();

    public AcceptanceReport CurrentModeReport => CanUseCurrentFusionMode();

    public IReadOnlyList<Thing> AvailableThings => cachedThings;

    private string CurrentCategory => largeFusionMode ? Props.largeCategory : Props.category;

    private int RequiredMaterialCount => largeFusionMode ? 3 : 2;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref largeFusionMode, "largeFusionMode");
    }

    public void SetLargeFusionMode(bool value)
    {
        if (largeFusionMode == value)
        {
            return;
        }

        largeFusionMode = value;
        ClearMaterials();
    }

    public void SetMaterial(int index, Thing? thing)
    {
        if (index < 0 || index >= RequiredMaterialCount)
        {
            return;
        }

        if (thing != null && !CanAcceptMaterial(index, thing))
        {
            return;
        }

        if (thing != null && materials.FirstOrDefault(pair => pair.Value == thing) is { Value: not null } existing)
        {
            materials[existing.Key] = null;
        }

        materials[index] = thing;
        UpdateFusion();
    }

    public bool CanAcceptMaterial(int index, Thing thing)
    {
        if (index < 0 || index >= RequiredMaterialCount)
        {
            return false;
        }

        return thing is not Pawn pawn
            || FleshBeastKindUtility.IsSize(pawn.kindDef, ExpectedMaterialSize(index));
    }

    public void ClearMaterials()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i] = null;
        }

        UpdateFusion();
    }

    public AcceptanceReport TryStartFusion()
    {
        if (cachedFusion == null)
        {
            return "FH_Fusion_NoValidFusion".Translate();
        }

        AcceptanceReport modeReport = CanUseCurrentFusionMode();
        if (!modeReport.Accepted)
        {
            return modeReport;
        }

        MapComponent_Unit unitMap = parent.Map.GetComponent<MapComponent_Unit>();
        List<Pawn> pawns = materials.Values.OfType<Pawn>().ToList();

        if (pawns.Any(pawn => unitMap.FusionRequirements.ContainsKey(pawn) || unitMap.UnitsInSpecialState.Contains(pawn)))
        {
            return "UnitIsFusingOrModifying".Translate();
        }

        FusionProgress_FleshHive progress = new FusionProgress_FleshHive
        {
            time = cachedFusion.progressTick,
            totalTime = cachedFusion.progressTick,
            fusionDef = cachedFusion
        };
        Progress.progresses.Add(progress);

        foreach (Thing thing in materials.Values.Where(thing => thing != null))
        {
            QueueParticipant(progress, thing, unitMap);
        }

        ClearMaterials();
        Cache();
        Messages.Message("AddFusionProgress".Translate(), parent, MessageTypeDefOf.PositiveEvent);
        return true;
    }

    public override void DrawFusion(Rect inRect)
    {
        Rect contentRect = inRect.ContractedBy(16f, 12f);
        AcceptanceReport researchReport = CanUseCurrentFusionMode();

        float slotSize = 64f;
        float labelHeight = 24f;
        float materialGap = 34f;
        float resultGap = 58f;
        float rowWidth = largeFusionMode
            ? slotSize * 4f + materialGap * 2f + resultGap
            : slotSize * 3f + materialGap + resultGap;
        float centerY = contentRect.y + 50f;
        float x = contentRect.x + Mathf.Max(0f, (contentRect.width - rowWidth) / 2f);
        float rowCenterX = x + rowWidth / 2f;

        DrawModeButton(contentRect, rowCenterX);

        Rect first = new Rect(x, centerY, slotSize, slotSize);
        x = first.xMax + materialGap;
        Rect second = new Rect(x, centerY, slotSize, slotSize);
        Rect? third = null;
        if (largeFusionMode)
        {
            x = second.xMax + materialGap;
            third = new Rect(x, centerY, slotSize, slotSize);
        }

        x = (third ?? second).xMax + resultGap;
        Rect result = new Rect(x, centerY, slotSize, slotSize);

        DrawMaterialSlot(0, first, largeFusionMode ? "FH_Fusion_SlotMedium".Translate() : "FH_Fusion_SlotSmall".Translate(), labelHeight);
        DrawOperatorLabel(new Rect(first.xMax + 5f, first.y + 8f, 24f, 32f), "+");
        DrawMaterialSlot(1, second, largeFusionMode ? "FH_Fusion_SlotMedium".Translate() : "FH_Fusion_SlotSmall".Translate(), labelHeight);
        if (third.HasValue)
        {
            DrawOperatorLabel(new Rect(second.xMax + 5f, second.y + 8f, 24f, 32f), "+");
            DrawMaterialSlot(2, third.Value, "FH_Fusion_SlotSmall".Translate(), labelHeight);
        }

        DrawOperatorLabel(new Rect((third ?? second).xMax + 15f, result.y + 8f, 32f, 32f), "->");
        DrawResultSlot(result, largeFusionMode ? "FH_Fusion_SlotLarge".Translate() : "FH_Fusion_SlotMedium".Translate(), labelHeight);

        Rect buttonRect = new Rect(rowCenterX - 60f, Mathf.Min(contentRect.yMax - 40f, result.yMax + labelHeight + 16f), 120f, 30f);
        bool canFuse = cachedFusion != null && researchReport.Accepted;
        if (!researchReport.Accepted)
        {
            TooltipHandler.TipRegion(buttonRect, researchReport.Reason);
        }
        if (Widgets.ButtonText(buttonRect, "Fuse".Translate(), true, true, canFuse) && cachedFusion != null)
        {
            AcceptanceReport report = TryStartFusion();
            if (!report.Accepted && !report.Reason.NullOrEmpty())
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
            }
        }
    }

    public override void UpdateFusion()
    {
        cachedFusion = null;
        know = false;
        if (!CanUseCurrentFusionMode().Accepted)
        {
            return;
        }
        if (!HCFGameUtility.FusionDefs.TryGetValue(CurrentCategory, out List<FusionDef> fusions))
        {
            return;
        }

        List<Thing> provided = materials.Values.Where(t => t != null).ToList();
        if (provided.Count != RequiredMaterialCount)
        {
            return;
        }

        if (materials.Any(pair => pair.Value != null && !CanAcceptMaterial(pair.Key, pair.Value)))
        {
            return;
        }

        foreach (FusionDef fusionDef in fusions)
        {
            List<Thing> available = new List<Thing>(provided);
            bool match = true;
            foreach (FusionMaterial required in fusionDef.materials)
            {
                bool found = false;
                for (int i = 0; i < available.Count; i++)
                {
                    if (required.CanAccept(fusionDef, available[i]))
                    {
                        available.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    match = false;
                    break;
                }
            }

            if (match && !available.Any() && (cachedFusion == null || fusionDef.priority > cachedFusion.priority))
            {
                cachedFusion = fusionDef;
            }
        }

        if (cachedFusion == null)
        {
            cachedFusion = Props.defaultFusion;
        }

        if (cachedFusion != null)
        {
            know = true;
            if (cachedFusion.hiden && !GameComponent_UnitGroup.Instance.fusionDatas.Exists(d => d.def == cachedFusion && d.unlocked))
            {
                know = false;
            }

            if (cachedFusion.isDefault && !GameComponent_UnitGroup.Instance.fusionDatas.Exists(d => d.def == cachedFusion && d.IsKnown(materials)))
            {
                know = false;
            }
        }
    }

    public override void DropInSelectableZone(object o)
    {
        if (o is Thing t && materials.ToList().Find(m => m.Value == t) is { } existing)
        {
            materials[existing.Key] = null;
            UpdateFusion();
        }
    }

    private void DrawModeButton(Rect contentRect, float centerX)
    {
        string label = (largeFusionMode ? "FH_Fusion_SwitchToMedium" : "FH_Fusion_SwitchToLarge").Translate();
        float width = Mathf.Max(120f, Text.CalcSize(label).x + 24f);
        Rect buttonRect = new Rect(centerX - width / 2f, contentRect.y - 4f, width, 30f);
        if (Widgets.ButtonText(buttonRect, label))
        {
            SetLargeFusionMode(!largeFusionMode);
        }
    }

    private void DrawMaterialSlot(int index, Rect rect, string label, float labelHeight)
    {
        DrawFrame(rect);
        DrawMaterial(index, rect.ContractedBy(5f));
        DrawCenteredLabel(new Rect(rect.x - 18f, rect.yMax + 4f, rect.width + 36f, labelHeight), label);
    }

    private void DrawMaterial(int index, Rect rect)
    {
        Thing target = materials[index];
        if (target == null)
        {
            Widgets.DrawTextureFitted(rect.ContractedBy(4f), FHUtitly.RemoveFromPlatform, 1f);
        }
        else
        {
            if (DragAndDropWidget.Draggable(dragAndDropGroup, rect, target))
            {
                curDraw = target;
            }
            else
            {
                Widgets.ThingIcon(rect, target);
            }

            TooltipHandler.TipRegion(rect, target.Label);
        }

        DragAndDropWidget.DropArea(dragAndDropGroup, rect, o =>
        {
            if (o is Thing t)
            {
                SetMaterial(index, t);
            }
        }, null);
    }

    private FleshBeastSize ExpectedMaterialSize(int index)
    {
        return largeFusionMode && index < 2 ? FleshBeastSize.Medium : FleshBeastSize.Small;
    }

    private void DrawResultSlot(Rect rect, string label, float labelHeight)
    {
        DrawFrame(rect);
        Rect iconRect = rect.ContractedBy(7f);
        PawnKindDef? kind = ResultKind();
        if (kind != null && know)
        {
            Widgets.DefIcon(iconRect, kind);
        }
        else
        {
            DrawMotherCodexPreview(iconRect);
        }

        DrawCenteredLabel(new Rect(rect.x - 18f, rect.yMax + 4f, rect.width + 36f, labelHeight), label);
    }

    private void DrawOperatorLabel(Rect rect, string label)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = OperatorColor;
        Widgets.Label(rect, label);
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private void DrawMotherCodexPreview(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, UnknownResultBackgroundColor);
        Widgets.DrawTextureFitted(rect.ContractedBy(2f), DreadmeldSilhouetteTex, 1f);
    }

    private void DrawCenteredLabel(Rect rect, string label)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, label);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void QueueParticipant(FusionProgress progress, Thing thing, MapComponent_Unit unitMap)
    {
        progress.requiredThings.Add(thing);
        if (thing is Pawn pawn)
        {
            if (pawn.ParentHolder is CompHiveContainer container)
            {
                container.units.Remove(pawn);
                progress.requiredThings.Remove(thing);
                progress.inners.Add(pawn);
                unitMap.UnitsInSpecialState.Add(pawn);
            }
            else
            {
                unitMap.FusionRequirements.Add(pawn, parent);
                if (pawn.Spawned && pawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
                {
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(HCFDefOf.HCF_EnterFuser, parent));
                }
            }
        }
        else if (Resource.SpecialItems.Contains(thing))
        {
            Resource.SpecialItems.Remove(thing);
            progress.inners.Add(thing);
            progress.requiredThings.Remove(thing);
        }
    }

    private FusionResult? FirstResult()
    {
        return cachedFusion?.results.FirstOrDefault()?.result;
    }

    private PawnKindDef? ResultKind()
    {
        return FirstResult() is FusionResult_PawnKind pawnKindResult ? pawnKindResult.kind : null;
    }

    private AcceptanceReport CanUseCurrentFusionMode()
    {
        // ResearchProjectDef research = largeFusionMode ? FleshHiveDefOf.FH_Research_AdvancedFleshHive : FleshHiveDefOf.FH_Research_ComplexFleshHive;
        ResearchProjectDef research = largeFusionMode ? FleshHiveDefOf.FH_Research_ComplexFleshFusion : FleshHiveDefOf.FH_Research_FleshFusion;
        return research.IsFinished ? true : "MissingRequiredResearch".Translate(research.LabelCap);
    }

    private bool largeFusionMode;
    private bool know;
    private FusionDef? cachedFusion;
    private Texture2D? dreadmeldSilhouetteTex;
    private static readonly Color UnknownResultColor = new Color(0.12f, 0.12f, 0.12f, 0.82f);
    private static readonly Color UnknownResultBackgroundColor = new Color(0.02f, 0.035f, 0.035f);
    private static readonly Color OperatorColor = new Color(0.55f, 0.55f, 0.55f);
}
