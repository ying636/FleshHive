using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(Need_Beauty), nameof(Need_Beauty.CurrentInstantBeauty))]
public static class Patch_NeedBeauty_FleshAdaptation
{
    static bool Prefix(Need_Beauty __instance, ref float __result)
    {
        Pawn pawn = PawnField.GetValue(__instance) as Pawn;
        if (!FleshAdaptationUtility.HasAdaptation(pawn))
        {
            return true;
        }

        if (pawn is not { SpawnedOrAnyParentSpawned: true })
        {
            __result = 0.5f;
            return false;
        }

        __result = AverageBeautyPerceptible(pawn.PositionHeld, pawn.MapHeld);
        return false;
    }

    static float AverageBeautyPerceptible(IntVec3 root, Map map)
    {
        if (!root.IsValid || !root.InBounds(map))
        {
            return 0f;
        }

        countedThings.Clear();
        float beauty = 0f;
        int cellCount = 0;
        BeautyUtility.FillBeautyRelevantCells(root, map);
        for (int i = 0; i < BeautyUtility.beautyRelevantCells.Count; i++)
        {
            beauty += CellBeauty(BeautyUtility.beautyRelevantCells[i], map, countedThings);
            cellCount++;
        }
        countedThings.Clear();
        if (cellCount == 0)
        {
            return 0f;
        }
        return beauty / cellCount;
    }

    static float CellBeauty(IntVec3 c, Map map, HashSet<Thing> countedThings)
    {
        float beauty = 0f;
        float fullFillageBeauty = 0f;
        bool hasFullFillage = false;
        bool outdoors = c.GetRoom(map)?.PsychologicallyOutdoors ?? true;
        bool roofed = map.roofGrid.Roofed(c);
        List<Thing> things = map.thingGrid.ThingsListAt(c);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (!BeautyUtility.BeautyRelevant(thing.def.category) || countedThings.Contains(thing))
            {
                continue;
            }

            countedThings.Add(thing);
            SlotGroup slotGroup = thing.GetSlotGroup();
            if (thing.def.EverHaulable && slotGroup != null && slotGroup.parent != thing && slotGroup.parent.IgnoreStoredThingsBeauty)
            {
                continue;
            }

            float thingBeauty = thing.GetBeauty(outdoors);
            if (FleshAdaptationUtility.IsFleshBeautyThing(thing) && thingBeauty <= 0f)
            {
                thingBeauty = Mathf.Max(1f, Mathf.Abs(thingBeauty));
            }
            if (thing.def.filth != null && roofed)
            {
                thingBeauty *= 0.3f;
            }
            if (thing.def.Fillage == FillCategory.Full)
            {
                hasFullFillage = true;
                fullFillageBeauty += thingBeauty;
            }
            else
            {
                beauty += thingBeauty;
            }
        }

        if (hasFullFillage)
        {
            return fullFillageBeauty;
        }

        TerrainDef terrainDef = map.terrainGrid.TerrainAt(c);
        if (ModsConfig.BiotechActive && !terrainDef.BuildableByPlayer && c.IsPolluted(map))
        {
            beauty -= 1f;
        }
        if (outdoors && terrainDef.StatBaseDefined(StatDefOf.BeautyOutdoors))
        {
            return beauty + terrainDef.GetStatValueAbstract(StatDefOf.BeautyOutdoors);
        }
        return beauty + terrainDef.GetStatValueAbstract(StatDefOf.Beauty);
    }

    static readonly FieldInfo PawnField = AccessTools.Field(typeof(Need), "pawn");
    static readonly HashSet<Thing> countedThings = new HashSet<Thing>();
}
