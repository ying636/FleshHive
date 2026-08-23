using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class HiveResource_FleshHiveNutrition : HiveResource
{
    public HiveResource_FleshHiveNutrition(CompHiveResource comp)
        : base(comp)
    {
    }

    public HiveResource_FleshHiveNutrition(HiveResourceDef def, float baseLimit, CompHiveResource comp)
        : base(def, baseLimit, comp)
    {
    }

    private MapComponent_FleshHive FleshHiveComponent => comp?.parent?.Map?.GetComponent<MapComponent_FleshHive>();
    private MapFleshHive FleshHive => FleshHiveComponent?.MapFleshHive;

    public override bool CanFill => false;

    public override float Amount
    {
        get
        {
            SynchronizeAllowedToFill();
            return FleshHive?.nutrition ?? 0f;
        }
    }

    public override float TargetValue
    {
        get
        {
            return FleshHiveComponent?.NutritionTargetValue ?? targetValue;
        }
        set
        {
            targetValue = value;
            if (FleshHiveComponent != null)
            {
                FleshHiveComponent.NutritionTargetValue = value;
                targetValue = FleshHiveComponent.NutritionTargetValue;
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        SynchronizeAllowedToFill();
        return base.GetGizmos();
    }

    public override void Draw(ref Rect resourceRect)
    {
        MapComponent_FleshHive component = FleshHiveComponent;
        if (component == null)
        {
            base.Draw(ref resourceRect);
            return;
        }

        SynchronizeAllowedToFill();
        Vector2 initialPosition = resourceRect.position;
        Rect rect = new(resourceRect.x + 5f, resourceRect.y + 5f, resourceRect.width - 10f, 25f);
        Texture icon = IconTex;
        if (icon != null)
        {
            Rect iconRect = new(rect.x, rect.y, 24f, 24f);
            GUI.DrawTexture(iconRect, icon);
            rect.xMin = iconRect.xMax + 6f;
        }

        Widgets.Label(rect, def.label);
        Rect checkRect = new(rect.xMax - 24f, rect.y, 24f, 24f);
        GUI.DrawTexture(checkRect,
            component.NutritionAllowedToFill ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);
        bool mouseOverCheckbox = false;
        if (Widgets.ButtonInvisible(checkRect, true))
        {
            component.NutritionAllowedToFill = !component.NutritionAllowedToFill;
            allowedToFill = component.NutritionAllowedToFill;
            synchronizedAllowedToFill = allowedToFill;
            if (component.NutritionAllowedToFill)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        if (Mouse.IsOver(checkRect))
        {
            Widgets.DrawHighlight(checkRect);
            TooltipHandler.TipRegion(checkRect, () => "HCF_AutoFill".Translate(), 828267373);
            mouseOverCheckbox = true;
        }

        rect.y += 30f;
        float limit = GetLimit();
        float valuePercent = limit > 0f ? Amount / limit : 0f;
        Widgets.FillableBar(rect, valuePercent, BarTex);
        float targetValue = component.NutritionTargetValue;
        Widgets.DraggableBar(rect, BarTex,
            HCFUtility.BarHighlightTex, HCFUtility.EmptyBarTex, HCFUtility.DragBarTex,
            ref nutritionDragging, valuePercent, ref targetValue,
            GetNutritionBarThresholds(limit), 20, 0f, 1f);
        component.NutritionTargetValue = targetValue;

        string amount = $"{Amount:F2}/{limit}";
        rect.x += (rect.width - Text.CalcSize(amount).x) / 2f;
        rect.y += 2.5f;
        Widgets.Label(rect, amount);
        rect.y += 30f;
        resourceRect.y = rect.y;
        Rect wholeResource = new(initialPosition,
            new Vector2(resourceRect.width, resourceRect.y - initialPosition.y));
        if (Mouse.IsOver(wholeResource) && !mouseOverCheckbox)
        {
            Widgets.DrawHighlight(wholeResource);
            TooltipHandler.TipRegion(wholeResource, TooltipText);
        }

        Widgets.DrawBox(wholeResource, 2, Window_Hive.BorderTex);
    }

    public override float GetLimit()
    {
        return FleshHiveComponent?.NutritionLimit ?? 0f;
    }

    public override void IncreaseResource(float value)
    {
        var fh = FleshHive;
        if (fh != null)
        {
            fh.nutrition = Mathf.Min(GetLimit(), fh.nutrition + value);
        }
    }

    public override void DecreaseResource(float value)
    {
        var fh = FleshHive;
        if (fh != null)
        {
            fh.nutrition = Mathf.Max(0f, fh.nutrition - value);
        }
    }

    private IEnumerable<float> GetNutritionBarThresholds(float limit)
    {
        if (limit <= 0f)
        {
            yield break;
        }

        for (float value = 10f; value < limit; value += 10f)
        {
            yield return value / limit;
        }
    }

    private void SynchronizeAllowedToFill()
    {
        MapComponent_FleshHive component = FleshHiveComponent;
        if (component == null)
        {
            return;
        }

        if (allowedToFillSynchronized && allowedToFill != synchronizedAllowedToFill)
        {
            component.NutritionAllowedToFill = allowedToFill;
        }

        allowedToFill = component.NutritionAllowedToFill;
        synchronizedAllowedToFill = allowedToFill;
        allowedToFillSynchronized = true;
    }

    private bool allowedToFillSynchronized;
    private bool nutritionDragging;
    private bool synchronizedAllowedToFill;
}
