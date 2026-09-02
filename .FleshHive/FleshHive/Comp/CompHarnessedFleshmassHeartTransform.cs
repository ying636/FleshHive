using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompHarnessedFleshmassHeartTransform : ThingComp
{
    public CompProperties_HarnessedFleshmassHeartTransform Props =>
        (CompProperties_HarnessedFleshmassHeartTransform)props;

    private Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(Props.iconPath);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        if (parent.Faction != null && parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }

        Command_Action command = new Command_Action
        {
            defaultLabel = "FH_HarnessedFleshmassHeart_TransformLabel".Translate(),
            defaultDesc = "FH_HarnessedFleshmassHeart_TransformDesc".Translate(Props.nutritionCost.ToString("0")),
            icon = Icon,
            action = ConfirmTransform
        };

        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(parent.Map);
        if (fleshHive == null || fleshHive.nutrition < Props.nutritionCost)
        {
            float available = fleshHive?.nutrition ?? 0f;
            command.Disable("FH_HarnessedFleshmassHeart_TransformDisabled".Translate(
                Props.nutritionCost.ToString("0"),
                available.ToString("0.##")));
        }

        yield return command;

        if (DebugSettings.ShowDevGizmos)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Transform into harnessed fleshtitan now",
                defaultDesc = "Immediately transform this heart without consuming hive nutrition.",
                action = TransformIgnoringNutritionCost
            };
        }
    }

    private void ConfirmTransform()
    {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            "FH_HarnessedFleshmassHeart_TransformConfirm".Translate(Props.nutritionCost.ToString("0")),
            Transform));
    }

    private void Transform()
    {
        Transform(consumeNutrition: true);
    }

    private void TransformIgnoringNutritionCost()
    {
        Transform(consumeNutrition: false);
    }

    private void Transform(bool consumeNutrition)
    {
        Map map = parent.Map;
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(map);
        if (map == null || consumeNutrition && (fleshHive == null || fleshHive.nutrition < Props.nutritionCost))
        {
            if (consumeNutrition)
            {
                Messages.Message(
                    "FH_HarnessedFleshmassHeart_TransformFailed".Translate(),
                    parent,
                    MessageTypeDefOf.RejectInput);
            }

            return;
        }

        IntVec3 position = parent.Position;
        Faction faction = parent.Faction ?? Faction.OfPlayer;
        Pawn titan = FleshHiveFleshbeastSpawnUtility.GeneratePawn(Props.titanKind, faction);
        int biosignature = parent.GetComp<CompBiosignatureOwner>()?.biosignature ?? -1;

        if (consumeNutrition)
        {
            fleshHive!.nutrition -= Props.nutritionCost;
        }

        bool allowDestroyNonDestroyable = Thing.allowDestroyNonDestroyable;
        Thing.allowDestroyNonDestroyable = true;
        try
        {
            parent.Destroy(DestroyMode.Vanish);
        }
        finally
        {
            Thing.allowDestroyNonDestroyable = allowDestroyNonDestroyable;
        }

        GenSpawn.Spawn(titan, position, map);
        titan.TryGetComp<CompFleshtitanReversion>()?.InitializeFromHeart(
            0f,
            null,
            biosignature);
        Messages.Message(
            "FH_HarnessedFleshmassHeart_Transformed".Translate(titan.Named("TITAN")),
            titan,
            MessageTypeDefOf.PositiveEvent);
    }

    private Texture2D? icon;
}
