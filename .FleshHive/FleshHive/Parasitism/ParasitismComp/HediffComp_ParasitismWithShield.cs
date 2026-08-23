using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HediffCompProperties_ParasitismWithShield : HediffCompProperties_Parasitism
{
    public HediffCompProperties_ParasitismWithShield()
    {
        this.compClass = typeof(HediffComp_ParasitismWithShield);
    }
}

public class HediffComp_ParasitismWithShield : HediffComp_Parasitism
{
    public override List<PawnRenderNode> CompRenderNodes()
    {
        List<PawnRenderNode> nodes = base.CompRenderNodes();
        Pawn pawn = this.Pawn;
        if (pawn == null)
        {
            return nodes;
        }
        HediffComp_ScarletField comp = this.parent.TryGetComp<HediffComp_ScarletField>();
        if (comp == null)
        {
            return nodes;
        }

        float smallRadius = 0.5f + pawn.BodySize * 0.3f;
        PawnRenderNodeProperties smallProps = new PawnRenderNodeProperties
        {
            workerClass = typeof(PawnRenderNodeWorker_ScarletField),
            drawSize = new Vector2(smallRadius * 2f, smallRadius * 2f),
            baseLayer = 200
        };
        PawnNodeRender_ScarletField smallNode = new PawnNodeRender_ScarletField(pawn, smallProps, pawn.Drawer.renderer.renderTree)
        {
            comp = comp
        };
        nodes.Add(smallNode);

        float bigRadius = comp.Props.areaShieldRadius;
        PawnRenderNodeProperties bigProps = new PawnRenderNodeProperties
        {
            workerClass = typeof(PawnRenderNodeWorker_ScarletField),
            drawSize = new Vector2(bigRadius * 2f, bigRadius * 2f),
            baseLayer = 1500
        };
        PawnNodeRender_ScarletField bigNode = new PawnNodeRender_ScarletField(pawn, bigProps, pawn.Drawer.renderer.renderTree)
        {
            comp = comp,
            absoluteAltitude = AltitudeLayer.MoteOverhead.AltitudeFor()
        };
        nodes.Add(bigNode);

        return nodes;
    }
}
