using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class CompProperties_HiveNutritionRefuelable : CompProperties_Refuelable
{
    public CompProperties_HiveNutritionRefuelable()
    {
        compClass = typeof(CompHiveNutritionRefuelable);
        fuelFilter = new ThingFilter();
    }

    public float nutritionPerFuel = 0.1f;
}

public class CompHiveNutritionRefuelable : CompRefuelable
{
    private CompProperties_HiveNutritionRefuelable HiveProps => (CompProperties_HiveNutritionRefuelable)props;

    public override void CompTick()
    {
        base.CompTick();
        if (parent.IsHashIntervalTick(RefuelCheckInterval))
        {
            TryRefuelFromHiveNutrition();
        }
    }

    public override string CompInspectStringExtra()
    {
        string text = HiveProps.fuelFilter.AnyAllowedDef != null
            ? base.CompInspectStringExtra()
            : $"{HiveProps.FuelLabel}: {Fuel.ToStringDecimalIfSmall()} / {HiveProps.fuelCapacity.ToStringDecimalIfSmall()}";
        string nutritionText = "FH_HiveNutritionRefuelable_NutritionPerFuel".Translate(HiveProps.nutritionPerFuel.ToString("0.##"));
        return text.NullOrEmpty() ? nutritionText : text + "\n" + nutritionText;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        yield break;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Props.fuelIsMortarBarrel && Find.Storyteller.difficulty.classicMortars)
        {
            yield break;
        }

        if (Props.hideGizmosIfNotPlayerFaction && parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }

        if (Find.Selector.SelectedObjects.Count == 1)
        {
            yield return new Gizmo_HiveNutritionFuelStatus(this);
            yield break;
        }

        if (Props.targetFuelLevelConfigurable)
        {
            yield return new Command_SetTargetFuelLevel
            {
                refuelable = this,
                defaultLabel = "CommandSetTargetFuelLevel".Translate(),
                defaultDesc = "CommandSetTargetFuelLevelDesc".Translate(),
                icon = FleshHiveTex.SetTargetFuelLevelCommand
            };
        }

        if (Props.showAllowAutoRefuelToggle)
        {
            string onOff = allowAutoRefuel ? "On".Translate() : "Off".Translate();
            yield return new Command_Toggle
            {
                isActive = () => allowAutoRefuel,
                toggleAction = ToggleAutoRefuel,
                defaultLabel = "CommandToggleAllowAutoRefuel".Translate(),
                defaultDesc = "CommandToggleAllowAutoRefuelDescMult".Translate(onOff.UncapitalizeFirst().Named("ONOFF")),
                icon = allowAutoRefuel ? TexCommand.ForbidOn : TexCommand.ForbidOff,
                Order = 20f,
                hotKey = KeyBindingDefOf.Command_ItemForbid
            };
        }
    }

    private void TryRefuelFromHiveNutrition()
    {
        if (!parent.Spawned || !ShouldAutoRefuelNow || HiveProps.nutritionPerFuel <= 0f)
        {
            return;
        }

        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        if (fleshHive == null || fleshHive.nutrition < HiveProps.nutritionPerFuel)
        {
            return;
        }

        float neededFuel = TargetFuelLevel - Fuel;
        float affordableFuel = fleshHive.nutrition / HiveProps.nutritionPerFuel;
        float fuelToAdd = Mathf.Min(neededFuel, affordableFuel);
        if (fuelToAdd <= 0f)
        {
            return;
        }

        float nutritionCost = fuelToAdd * HiveProps.nutritionPerFuel;
        fleshHive.nutrition = Mathf.Max(0f, fleshHive.nutrition - nutritionCost);
        Refuel(fuelToAdd);
    }

    private void ToggleAutoRefuel()
    {
        allowAutoRefuel = !allowAutoRefuel;
        if (allowAutoRefuel)
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
        else
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
    }

    private const int RefuelCheckInterval = 250;

    private static readonly Color HiveNutritionColor = new(0.55f, 0.28f, 0.25f);

    private class Gizmo_HiveNutritionFuelStatus : Gizmo_SetFuelLevel
    {
        public Gizmo_HiveNutritionFuelStatus(CompRefuelable refuelable)
            : base(refuelable)
        {
        }

        protected override Color BarColor => HiveNutritionColor;

        protected override Color BarHighlightColor => Color.Lerp(HiveNutritionColor, Color.white, 0.18f);

        protected override Color BarDragColor => Color.Lerp(HiveNutritionColor, Color.white, 0.35f);
    }
}
