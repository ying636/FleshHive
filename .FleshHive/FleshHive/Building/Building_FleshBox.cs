using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Building_FleshBox : Building_Storage
{
    public Building_FleshBox()
    {
        squashNStretchProps = new Vector4(0.9f, 0.95f, 0.5f, -0.45f);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshBox(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map currentMap = Map;
        currentMap?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBox(this);
        base.DeSpawn(mode);
    }

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        ConvertNutrition(newItem);
    }

    public override void TickRare()
    {
        base.TickRare();
        ConvertStoredNutrition();
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        FleshHiveTex.FleshBoxOverlay.SetFloat(ShaderPropertyIDs.AgeSecs, (Find.TickManager.TicksGame - TickSpawned) / 60f);
        overlayPropertyBlock.SetVector(ShaderPropertyIDs.SquashNStretch, squashNStretchProps);
        overlayPropertyBlock.SetFloat(ShaderPropertyIDs.RandomPerObject, thingIDNumber.HashOffset());
        DrawLayer(drawLoc, AltitudeLayer.MetaOverlays.AltitudeFor(0.9f), FleshHiveTex.FleshBoxOverlay);
    }

    private void ConvertStoredNutrition()
    {
        if (!Spawned)
        {
            return;
        }

        foreach (Thing thing in GetSlotGroup().HeldThings.ToList())
        {
            ConvertNutrition(thing);
        }
    }

    private void ConvertNutrition(Thing thing)
    {
        if (!Spawned || thing == null || thing.Destroyed)
        {
            return;
        }

        float nutritionPerItem = thing.GetStatValue(StatDefOf.Nutrition, true, -1);
        if (nutritionPerItem <= 0f)
        {
            return;
        }

        MapComponent_FleshHive component = Map.GetComponent<MapComponent_FleshHive>();
        if (component == null)
        {
            return;
        }

        if (!component.NutritionAllowedToFill)
        {
            return;
        }

        float nutritionTarget = component.NutritionTargetValue * component.NutritionLimit;
        float availableNutrition = nutritionTarget - component.MapFleshHive.nutrition;
        int countToConvert = Mathf.Min(thing.stackCount, Mathf.FloorToInt(availableNutrition / nutritionPerItem));
        if (countToConvert <= 0)
        {
            return;
        }

        MapComponent_FleshHive.AddNutrition(Map, countToConvert * nutritionPerItem);
        if (countToConvert >= thing.stackCount)
        {
            thing.Destroy(DestroyMode.Vanish);
            return;
        }

        Thing splitThing = thing.SplitOff(countToConvert);
        splitThing.Destroy(DestroyMode.Vanish);
    }

    private void DrawLayer(Vector3 drawLoc, float altitude, Material material)
    {
        drawLoc.y = altitude;
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawLoc, Quaternion.identity, new Vector3(DrawScale, 1f, DrawScale)), material, 0, null, 0, overlayPropertyBlock);
    }

    private const float DrawScale = 1f;
    private readonly Vector4 squashNStretchProps;
    private static readonly MaterialPropertyBlock overlayPropertyBlock = new();
}
