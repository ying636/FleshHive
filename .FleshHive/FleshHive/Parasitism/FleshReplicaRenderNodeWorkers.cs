using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnRenderNodeWorker_FleshReplicaHead : PawnRenderNodeWorker_FlipWhenCrawling
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        if (base.CanDrawNow(node, parms))
        {
            return !parms.flags.FlagSet(PawnRenderFlags.HeadStump);
        }
        return false;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        if (parms.pawn is FleshReplicaUnit { Host: { } host }
            && host.Drawer.renderer.renderTree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Head, out PawnRenderNode hostNode))
        {
            parms.pawn = host;
            return hostNode.Worker.OffsetFor(hostNode, parms, out pivot);
        }
        return base.OffsetFor(node, parms, out pivot);
    }

    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (parms.pawn is FleshReplicaUnit { Host: { } host }
            && host.Drawer.renderer.renderTree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Head, out PawnRenderNode hostNode))
        {
            parms.pawn = host;
            return hostNode.Worker.ScaleFor(hostNode, parms);
        }
        return base.ScaleFor(node, parms);
    }
}

public class PawnRenderNodeWorker_FleshReplicaBeard : PawnRenderNodeWorker_Beard
{
    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        if (parms.pawn is FleshReplicaUnit { Host: { } host })
        {
            parms.pawn = host;
        }

        return base.OffsetFor(node, parms, out pivot);
    }
}
