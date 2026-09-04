using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints))]
public static class Patch_PawnGroupMakerUtility_ChoosePawnGenOptionsByPoints_FleshParasites
{
    public static bool Prefix(float pointsTotal, List<PawnGenOption> options, PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOptionWithXenotype> __result)
    {
        return FleshParasiteRaidGenerator.TryChoosePawnGenOptionsByPoints(pointsTotal, options, groupParms, ref __result);
    }
}

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public static class Patch_PawnGenerator_GeneratePawn_FleshParasites
{
    public static void Postfix(PawnGenerationRequest request, Pawn __result)
    {
        if (__result is FleshReplicaUnit replica
            && request.ForceGenerateNewPawn
            && request.FixedBiologicalAge == 0f)
        {
            replica.MarkAsSplitSpawn();
        }
        if (!FleshParasiteUtility.IsApplying)
        {
            FleshParasiteRaidGenerator.TryApplyParasites(request, __result);
        }
    }
}

public static class FleshParasiteRaidGenerator
{
    public static bool TryChoosePawnGenOptionsByPoints(float pointsTotal, List<PawnGenOption> options, PawnGroupMakerParms groupParms, ref IEnumerable<PawnGenOptionWithXenotype> result)
    {
        if (groupParms?.faction == null)
        {
            return true;
        }
        if (groupParms.seed.HasValue)
        {
            Rand.PushState(groupParms.seed.Value);
        }
        try
        {
            List<PlannedPawnGenOption> candidates = new List<PlannedPawnGenOption>();
            List<PawnGenOptionWithXenotype> chosenOptions = new List<PawnGenOptionWithXenotype>();
            List<PawnKindDef> chosenHostKinds = new List<PawnKindDef>();
            List<PawnKindDef[]> chosenParasites = new List<PawnKindDef[]>();
            float pointsLeft = pointsTotal;
            bool leaderChosen = false;
            float highestCost = -1f;
            while (true)
            {
            candidates.Clear();
            foreach (PawnGenOptionWithXenotype option in PawnGroupMakerUtility.GetOptions(groupParms, groupParms.faction.def, options, pointsTotal, pointsLeft, null, chosenOptions, leaderChosen))
            {
                PawnKindDef[] parasites = FleshParasiteUtility.GenerateParasitePlan(option.Option.kind, groupParms.faction, true);
                float cost = option.Cost + ParasiteCost(parasites);
                if (cost > pointsLeft)
                {
                        continue;
                    }
                    if (cost > highestCost)
                    {
                        highestCost = cost;
                    }
                    candidates.Add(new PlannedPawnGenOption(option, parasites, cost));
                }
                Func<PlannedPawnGenOption, float> weightSelector = candidate =>
                    !PawnGroupMakerUtility.PawnGenOptionValid(candidate.Option.Option, groupParms, chosenOptions)
                        ? 0f
                        : candidate.Option.SelectionWeight * PawnWeightFactorByMostExpensivePawnCostFractionCurve.Evaluate(candidate.Cost / highestCost);
                if (!candidates.TryRandomElementByWeight(weightSelector, out PlannedPawnGenOption chosen))
                {
                    break;
                }
                chosenOptions.Add(chosen.Option);
                chosenHostKinds.Add(chosen.Option.Option.kind);
                chosenParasites.Add(chosen.Parasites);
                pointsLeft -= chosen.Cost;
                if (chosen.Option.Option.kind.factionLeader)
                {
                    leaderChosen = true;
                }
            }
            if (chosenOptions.Count == 1 && pointsLeft > pointsTotal / 2f)
            {
                Log.Warning($"Used only {pointsTotal - pointsLeft} / {pointsTotal} points generating for {groupParms.faction}");
            }
            plannedParasites.Clear();
            plannedHostKinds.Clear();
            plannedHostKinds.AddRange(chosenHostKinds);
            plannedParasites.AddRange(chosenParasites);
            plannedFaction = groupParms.faction;
            result = chosenOptions;
            return false;
        }
        finally
        {
            if (groupParms.seed.HasValue)
            {
                Rand.PopState();
            }
        }
    }

    public static void TryApplyParasites(PawnGenerationRequest request, Pawn pawn)
    {
        if (FleshParasiteUtility.IsApplying || pawn == null || request.KindDef != pawn.kindDef)
        {
            return;
        }
        if (request.KindDef == FleshHiveDefOf.FH_Fleshwind && (request.Faction != plannedFaction || plannedParasites.Count == 0 || plannedHostKinds.Count == 0 || plannedHostKinds[0] != request.KindDef))
        {
            FleshParasiteUtility.ApplyParasites(pawn, FleshParasiteUtility.GenerateParasitePlan(request.KindDef, request.Faction, false, true));
            return;
        }
        if (request.Faction != plannedFaction || plannedParasites.Count == 0 || plannedHostKinds.Count == 0)
        {
            if (request.Faction == Faction.OfEntities)
            {
                FleshParasiteUtility.TryApplyDefaultParasites(pawn);
            }
            return;
        }
        int planIndex = plannedHostKinds.IndexOf(request.KindDef);
        if (planIndex < 0)
        {
            if (request.Faction == Faction.OfEntities)
            {
                FleshParasiteUtility.TryApplyDefaultParasites(pawn);
            }
            return;
        }
        PawnKindDef[] parasites = plannedParasites[planIndex];
        plannedHostKinds.RemoveAt(planIndex);
        plannedParasites.RemoveAt(planIndex);
        if (plannedParasites.Count == 0)
        {
            plannedFaction = null;
        }
        if (parasites.NullOrEmpty())
        {
            return;
        }
        FleshParasiteUtility.ApplyParasites(pawn, parasites);
    }

    private static float ParasiteCost(PawnKindDef[] parasites)
    {
        float cost = 0f;
        for (int i = 0; i < parasites.Length; i++)
        {
            cost += parasites[i].combatPower;
        }
        return cost;
    }


    private static readonly SimpleCurve PawnWeightFactorByMostExpensivePawnCostFractionCurve = new SimpleCurve
    {
        new CurvePoint(0.2f, 0.01f),
        new CurvePoint(0.3f, 0.3f),
        new CurvePoint(0.5f, 1f)
    };

    private static readonly List<PawnKindDef[]> plannedParasites = new List<PawnKindDef[]>();
    private static readonly List<PawnKindDef> plannedHostKinds = new List<PawnKindDef>();

    private static Faction plannedFaction;
    private readonly struct PlannedPawnGenOption
    {
        public PlannedPawnGenOption(PawnGenOptionWithXenotype option, PawnKindDef[] parasites, float cost)
        {
            Option = option;
            Parasites = parasites;
            Cost = cost;
        }

        public PawnGenOptionWithXenotype Option { get; }
        public PawnKindDef[] Parasites { get; }
        public float Cost { get; }
    }
}
