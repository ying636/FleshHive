using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Blueprint_FleshBuild : Blueprint_HiveBuild
{
    public override bool CanBeBuilt => false; 
    public bool HasPendingMaterials => !MaterialsSatisfied;
    public bool HasPendingWork => workAmount < buildingDef.workAmount;
    private Graphic OutlineGraphic => cachedOutlineGraphic ??= CreateOutlineGraphic();
 

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<MapComponent_FleshHive>()?.RegisterFleshBlueprint(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map currentMap = Map;
        currentMap?.GetComponent<MapComponent_FleshHive>()?.UnregisterFleshBlueprint(this);
        base.DeSpawn(mode);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        OutlineGraphic?.Draw(drawLoc.WithYOffset(-0.1f), flip ? Rotation.Opposite : Rotation, this, 0f);
        base.DrawAt(drawLoc, flip);
    }

    public override void Print(SectionLayer layer)
    {
        base.Print(layer);
            OutlineGraphic?.Print(layer, this, -1f);
    }

    protected override void Tick()
    {
        base.Tick();
        if (HasPendingMaterials)
        {
            buildTicks = BuildTicksPerWorkStep;
            return;
        }

        if (!HasPendingWork)
        {
            Complete(null);
            return;
        }

        buildTicks--;
        if (buildTicks > 0)
        {
            return;
        }

        workAmount = workAmount + WorkPerBuildStep;
        buildTicks = BuildTicksPerWorkStep;
        if (!HasPendingWork)
        {
            Complete(null);
        }
    }

    public ResourceCount GetNextNeededResource()
    {
        return needResources.FirstOrDefault(resourceCount => resourceCount.amount > 0f);
    }

    public ThingDefCountClass GetNextNeededThing()
    {
        return needThings.FirstOrDefault(thingCount => thingCount.count > 0);
    }

    public float ReceiveResource(HiveResourceDef resourceDef, float amount)
    {
        if (resourceDef == null || amount <= 0f)
        {
            return 0f;
        }

        ResourceCount needed = needResources.FirstOrDefault(resourceCount => resourceCount.resource == resourceDef && resourceCount.amount > 0f);
        if (needed == null)
        {
            return 0f;
        }

        float accepted = Mathf.Min(amount, needed.amount);
        if (accepted <= 0f)
        {
            return 0f;
        }

        needed.amount -= accepted;
        AddResource(resourceDef, accepted);
        return accepted;
    }

    public int ReceiveThing(ThingDef thingDef, int count)
    {
        if (thingDef == null || count <= 0)
        {
            return 0;
        }

        ThingDefCountClass needed = needThings.FirstOrDefault(thingCount => thingCount.thingDef == thingDef && thingCount.count > 0);
        if (needed == null)
        {
            return 0;
        }

        int accepted = Mathf.Min(count, needed.count);
        if (accepted <= 0)
        {
            return 0;
        }

        needed.count -= accepted;
        if (innerThings.FirstOrDefault(thingCount => thingCount.thingDef == thingDef) is { } innerThing)
        {
            innerThing.count += accepted;
        }
        else
        {
            innerThings.Add(new ThingDefCountClass(thingDef, accepted));
        }

        return accepted;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref buildTicks, "buildTicks", BuildTicksPerWorkStep);
    }

    private Graphic CreateOutlineGraphic()
    {
        string outlineTexPath = GetOutlineTexPath();
        if (outlineTexPath.NullOrEmpty() || def?.graphicData == null)
        {
            return null;
        }

        return GraphicDatabase.Get<Graphic_Single>(outlineTexPath, ShaderDatabase.Transparent, def.graphicData.drawSize, Color.white);
    }

    private string GetOutlineTexPath()
    {
        string texPath = def?.graphicData?.texPath;
        if (texPath.NullOrEmpty())
        {
            return null;
        }

        return texPath + "_Outline";
    }

    private int buildTicks = BuildTicksPerWorkStep;
    private Graphic cachedOutlineGraphic;

    private const int BuildTicksPerWorkStep = 10;
    private const float WorkPerBuildStep = 20f;
}
