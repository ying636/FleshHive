using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public static class FleshBlueprintUtility
{
    public static Blueprint_HiveBuild MakeBlueprint(HiveBuildingDef buildingDef)
    {
        ThingDef blueprintDef = GetBlueprintDef(buildingDef);
        Blueprint_HiveBuild blueprint = (Blueprint_HiveBuild)ThingMaker.MakeThing(blueprintDef);
        blueprint.buildingDef = buildingDef;
        foreach (ResourceCount resourcesCost in buildingDef.resourcesCosts)
        {
            blueprint.needResources.Add(new ResourceCount(resourcesCost.resource, resourcesCost.amount));
        }
        foreach (ThingDefCountClass requirement in buildingDef.Requirements)
        {
            blueprint.needThings.Add(new ThingDefCountClass(requirement.thingDef, requirement.count));
        }

        return blueprint;
    }

    public static void ConfigureBlueprint(HiveBuildingDef buildingDef)
    {
        ThingDef blueprintDef = GetBlueprintDef(buildingDef);
        blueprintDef.thingClass = typeof(Blueprint_FleshBuild);
        blueprintDef.size = buildingDef.Buildable.Size;
        blueprintDef.entityDefToBuild = buildingDef.Buildable;
        blueprintDef.graphicData = new GraphicData
        {
            texPath = GetBlueprintTexPath(buildingDef),
            graphicClass = typeof(Graphic_Single),
            shaderType = ShaderTypeDefOf.Transparent,
            drawSize = new Vector2(buildingDef.Buildable.Size.x, buildingDef.Buildable.Size.z) * 1.2f,
            linkType = LinkDrawerType.None,
            linkFlags = LinkFlags.None,
            asymmetricLink = null,
            color = Color.white,
            colorTwo = Color.white
        };
    }

    public static ThingDef GetBlueprintDef(HiveBuildingDef buildingDef)
    {
        if (HiveBuildingDef.blueprints.TryGetValue(buildingDef, out ThingDef blueprintDef))
        {
            return blueprintDef;
        }

        throw new MissingFieldException("Missing HCF blueprint def for " + buildingDef.defName);
    }

    private static string GetBlueprintTexPath(HiveBuildingDef buildingDef)
    {
        IntVec2 size = buildingDef.Buildable.Size;
        string sizePath = "Things/Building/FleshmassBase_" + size.x + "x" + size.z;
        if (SupportsVariant(size))
        {
            return sizePath + GetVariantSuffix(buildingDef);
        }

        return sizePath;
    }

    private static string GetVariantSuffix(HiveBuildingDef buildingDef)
    {
        int hash = Mathf.Abs(GenText.StableStringHash(buildingDef.defName));
        return Rand.Bool ? "A" : "B";
    }

    private static bool SupportsVariant(IntVec2 size)
    {
        return size.x == size.z && size.x >= 1 && size.x <= 3;
    }
}
