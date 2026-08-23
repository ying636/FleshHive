using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnNodeRender_ScarletField : PawnRenderNode
{
    public PawnNodeRender_ScarletField(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
    {
    }

    protected override string TexPathFor(Pawn pawn) => "Other/FleshShieldBubble";
    public override Color ColorFor(Pawn pawn) => new Color(1f, 0.15f, 0.15f, 0.6f);
    public override Graphic GraphicFor(Pawn pawn) => GraphicDatabase.Get<Graphic_Multi>(TexPathFor(pawn), ShaderDatabase.Transparent, Vector2.one, ColorFor(pawn));
    public override GraphicMeshSet MeshSetFor(Pawn pawn) => MeshPool.GetMeshSetForSize(1f, 1f);

    public HediffComp_ScarletField comp;
    public float? absoluteAltitude;
}

public class PawnRenderNodeWorker_ScarletField : PawnRenderNodeWorker
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node is PawnNodeRender_ScarletField n)
        {
            return n.comp?.Active == true;
        }
        return false;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        Vector3 result = base.OffsetFor(node, parms, out pivot);
        if (node is PawnNodeRender_ScarletField n && n.absoluteAltitude.HasValue)
        {
            float rootAltitude = parms.matrix.GetColumn(3).y;
            float layerAltitude = PawnRenderUtility.AltitudeForLayer(LayerFor(node, parms));
            result.y = n.absoluteAltitude.Value - rootAltitude - layerAltitude;
        }
        return result;
    }
}
