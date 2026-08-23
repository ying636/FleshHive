using RimWorld;
using Verse;

namespace FleshHive;

public class DynamicPawnRenderNodeSetup_Parasitism : DynamicPawnRenderNodeSetup
{
    public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
    { 
        if (pawn?.health?.hediffSet == null)
        {
            yield break;
        }

        PawnRenderNode node;
        pawn.Drawer.renderer.renderTree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body,out node);
        if (node == null)
        {
            node = pawn.Drawer.renderer.renderTree.rootNode;
        }
        foreach (var h in pawn.health.hediffSet.hediffs)
        {
            if (h is HediffWithComps hd)
            {
                foreach (var c in hd.comps)
                {
                    if (c is HediffComp_Parasitism comp)
                    {
                        foreach (var compRenderNode in comp.CompRenderNodes())
                        {
                            yield return (compRenderNode, node);
                        }
                    }
                }
            }
        }
        yield break;
    }

    public override bool HumanlikeOnly => false;
}
