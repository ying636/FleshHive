using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class UnitGroup_FleshHive : UnitGroup, IExposable
{
    public bool AllowHuntUndesignatedAnimals
    {
        get => allowHuntUndesignatedAnimals;
        set => allowHuntUndesignatedAnimals = value;
    }

    public int MinimumHealthyHunters
    {
        get => minimumHealthyHunters;
        set => minimumHealthyHunters = Mathf.Max(1, value);
    }

    public override bool Controllable => base.Controllable && !FleshHiveHungerUtility.IsHungry(hive);

    public override bool CanReturnHive => base.CanReturnHive && !FleshHiveHungerUtility.IsHungry(hive);

    public override void AcceptUnit(Pawn unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.TryGetComp<UnitComp>()?.group == this && this.units.Contains(unit))
        {
            return;
        }

        base.AcceptUnit(unit);
        Map?.GetComponent<MapComponent_FleshHive>()?.EnforceHiveGroupCapacity();
    }

    public override AcceptReason CanAccept(Pawn unit)
    {
        if (FleshHiveHungerUtility.IsHungry(hive))
        {
            return AcceptReason.False("FH_GroupReject_Hungry".Translate());
        }

        AcceptReason baseReason = base.CanAccept(unit);
        if (!baseReason.Accepted)
        {
            return baseReason;
        }

        return Map?.GetComponent<MapComponent_FleshHive>()?.CanAcceptIntoHiveGroup(this, unit) == false
            ? AcceptReason.False("FH_GroupReject_HivePopulationCapacity".Translate())
            : AcceptReason.True;
    }

    public override void OpenWorkSettings()
    {
        Find.WindowStack.TryRemove(typeof(Window_GroupWorkSetting));
        Find.WindowStack.Add(new Window_GroupWorkSetting(this, new Vector2(260f, 440f)));
    }

    public override void DrawWorkSettings(Rect inRect, ref Vector2 scrollPosition, ref float contentHeight)
    {
        string groupName = name ?? string.Empty;
        float nameX = Mathf.Max(0f, (inRect.width - groupName.GetWidthCached()) / 2f);
        Widgets.Label(new Rect(nameX, 5f, inRect.width, 25f), groupName);

        Rect scrollRect = new Rect(5f, 35f, inRect.width - 10f, inRect.height - 40f);
        Widgets.DrawBox(scrollRect);
        Widgets.BeginScrollView(scrollRect, ref scrollPosition,
            new Rect(0f, 0f, scrollRect.width, contentHeight));

        Rect settingRect = new Rect(15f, 10f, scrollRect.width - 55f, 25f);
        Widgets.CheckboxLabeled(settingRect,
            "FH_GroupHunt_AllowUndesignatedAnimals".Translate().Truncate(settingRect.width - 30f),
            ref allowHuntUndesignatedAnimals);
        Rect tipRect = new Rect(scrollRect.width - 35f, settingRect.y, 24f, 24f);
        Widgets.ButtonImage(tipRect, TexButton.Info);
        TooltipHandler.TipRegion(tipRect, "FH_GroupHunt_AllowUndesignatedAnimalsTip".Translate());

        Rect minimumRect = new Rect(15f, settingRect.yMax + 10f, scrollRect.width - 30f, 25f);
        Rect minimumLabelRect = new Rect(minimumRect.x, minimumRect.y, minimumRect.width - 80f, minimumRect.height);
        Widgets.Label(minimumLabelRect, "FH_GroupHunt_MinimumHealthyHunters".Translate().Truncate(minimumLabelRect.width));
        Rect minimumInputRect = new Rect(minimumRect.xMax - 70f, minimumRect.y, 70f, minimumRect.height);
        Widgets.TextFieldNumeric(minimumInputRect, ref minimumHealthyHunters, ref minimumHealthyHuntersBuffer, 1, 99);
        TooltipHandler.TipRegion(minimumRect, "FH_GroupHunt_MinimumHealthyHuntersTip".Translate());

        Rect workRect = new Rect(15f, minimumRect.yMax + 10f, scrollRect.width - 30f, 25f);
        List<UnitWorkDef>? specialWorks = hive?.TryGetComp<CompHiveGroup>()?.Props.specialWorks;
        foreach (UnitWorkDef work in DefDatabase<UnitWorkDef>.AllDefsListForReading)
        {
            if (work.isSpecial && specialWorks?.Contains(work) != true)
            {
                continue;
            }

            Widgets.Label(workRect, work.label);
            TooltipHandler.TipRegion(workRect, work.description);
            Rect toggleRect = new Rect(workRect.xMax - 30f, workRect.y - 5f, 25f, 25f);
            if (Widgets.ButtonImage(toggleRect,
                    disabledWorks.Contains(work) ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex))
            {
                if (!disabledWorks.Add(work))
                {
                    disabledWorks.Remove(work);
                }
            }

            workRect.y += 30f;
        }

        contentHeight = workRect.yMax;
        Widgets.EndScrollView();
    }

    void IExposable.ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref allowHuntUndesignatedAnimals, "allowHuntUndesignatedAnimals");
        Scribe_Values.Look(ref minimumHealthyHunters, "minimumHealthyHunters", 3);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            minimumHealthyHunters = Mathf.Max(1, minimumHealthyHunters);
        }
    }

    private bool allowHuntUndesignatedAnimals;

    private int minimumHealthyHunters = 3;

    private string minimumHealthyHuntersBuffer;
}
