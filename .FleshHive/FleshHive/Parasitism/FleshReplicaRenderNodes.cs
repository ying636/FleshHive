using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnRenderNode_FleshReplicaBody(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Body(pawn, props, tree)
{
    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        if (pawn is FleshReplicaUnit { Host: { } host })
        {
            return base.MeshSetFor(host);
        }

        Vector2 drawSize = pawn.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize;
        return MeshPool.GetMeshSetForSize(drawSize.x, drawSize.y);
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        if (pawn is FleshReplicaUnit { Host: { } host })
        {
            return base.GraphicFor(host);
        }

        return pawn.ageTracker.CurKindLifeStage.bodyGraphicData.Graphic;
    }
}

public class PawnRenderNode_FleshReplicaHead(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Head(pawn, props, tree)
{
    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.MeshSetFor(host) : null;
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.GraphicFor(host) : null;
    }
}

public class PawnRenderNode_FleshReplicaHair(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Hair(pawn, props, tree)
{
    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.MeshSetFor(host) : null;
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.GraphicFor(host) : null;
    }
}

public class PawnRenderNode_FleshReplicaBeard(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Beard(pawn, props, tree)
{
    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.MeshSetFor(host) : null;
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        return pawn is FleshReplicaUnit { Host: { } host } ? base.GraphicFor(host) : null;
    }
}
