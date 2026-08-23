using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnRenderNode_AnimalPart_Paraspike(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_AnimalPart(pawn, props, tree)
{
    public override Graphic GraphicFor(Pawn pawn)
    {
        if (pawn.health.hediffSet.HasHediff(FleshHiveDefOf.FH_LostSpike_Paraspike))
        {
            return GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Fleshbeast/FH_Paraspike/FH_Paraspike_lost",
                ShaderTypeDefOf.Cutout.Shader,new Vector2(2,2),Color.white);
        }
        return base.GraphicFor(pawn);
    }
}