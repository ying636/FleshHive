using UnityEngine;
using Verse;

namespace FleshHive;

public class Graphic_Single_SquashNStretchSafe : Graphic_WithPropertyBlock
{
    public override void Init(GraphicRequest req)
    {
        base.Init(req);
        squashNStretchProps = new Vector4(data.maxSnS.x, data.maxSnS.y, data.offsetSnS.x, data.offsetSnS.y);
    }

    public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
    {
        mat.SetFloat(ShaderPropertyIDs.AgeSecs, AgeSecs(thing));
        propertyBlock.SetVector(ShaderPropertyIDs.SquashNStretch, squashNStretchProps);
        propertyBlock.SetFloat(ShaderPropertyIDs.RandomPerObject, thing?.thingIDNumber.HashOffset() ?? 0f);
        base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }

    private static float AgeSecs(Thing thing)
    {
        if (thing == null)
        {
            return 0f;
        }

        return (Find.TickManager.TicksGame - thing.TickSpawned) / 60f;
    }

    private Vector4 squashNStretchProps;
}
