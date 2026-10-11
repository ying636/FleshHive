using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnRenderNodeWorker_FleshReplicaBody : PawnRenderNodeWorker_Body
{
    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (parms.pawn is FleshReplicaUnit { Host: { } host }
            && host.Drawer.renderer.renderTree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out PawnRenderNode hostNode))
        {
            parms.pawn = host;
            return hostNode.Worker.ScaleFor(hostNode, parms);
        }
        return base.ScaleFor(node, parms);
    }
}
