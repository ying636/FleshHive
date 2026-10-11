using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace FleshHive.CE;

internal sealed class ScarletFieldMapIndex
{
    private ScarletFieldMapIndex(Map map)
    {
        this.map = map;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (HasField(pawn))
            {
                candidates.Add(pawn);
            }
        }
    }

    internal static IReadOnlyList<Pawn> GetCandidates(Map map)
    {
        return indices.GetValue(map, key => new ScarletFieldMapIndex(key)).candidates;
    }

    internal static void Refresh(MapPawns registry, Pawn pawn)
    {
        Map map = registryMap(registry);
        if (indices.TryGetValue(map, out ScarletFieldMapIndex index))
        {
            index.RefreshPawn(pawn);
        }
    }

    internal static void Refresh(Pawn pawn)
    {
        if (pawn?.Map is Map map && indices.TryGetValue(map, out ScarletFieldMapIndex index))
        {
            index.RefreshPawn(pawn);
        }
    }

    private void RefreshPawn(Pawn pawn)
    {
        if (!pawn.Spawned || pawn.Map != map || !HasField(pawn))
        {
            candidates.Remove(pawn);
            return;
        }

        int position = PositionOf(pawn);
        if (position < 0)
        {
            candidates.Remove(pawn);
            return;
        }
        if (candidates.Contains(pawn))
        {
            return;
        }

        int index = 0;
        while (index < candidates.Count && PositionOf(candidates[index]) < position)
        {
            index++;
        }
        candidates.Insert(index, pawn);
    }

    private int PositionOf(Pawn pawn)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] == pawn)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool HasField(Pawn pawn)
    {
        return pawn.TryGetComp<CompScarletField>() != null || HediffComp_ScarletField.FindOnPawn(pawn) != null;
    }

    private readonly Map map;
    private readonly List<Pawn> candidates = new();
    private static readonly ConditionalWeakTable<Map, ScarletFieldMapIndex> indices = new();
    private static readonly AccessTools.FieldRef<MapPawns, Map> registryMap = AccessTools.FieldRefAccess<MapPawns, Map>("map");
}
