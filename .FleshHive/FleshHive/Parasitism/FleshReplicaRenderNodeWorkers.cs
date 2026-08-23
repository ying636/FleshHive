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
        Vector3 result = base.OffsetFor(node, parms, out pivot);
        if (parms.pawn is not FleshReplicaUnit { Host: not null } replica)
        {
            return result;
        }

        Pawn host = replica.Host;
        if (host.story?.bodyType != null)
        {
            Vector2 vector = host.story.bodyType.headOffset * Mathf.Sqrt(host.ageTracker.CurLifeStage.bodySizeFactor);
            if (parms.facing == Rot4.North || parms.facing == Rot4.South)
            {
                result += new Vector3(0f, 0f, vector.y);
            }
            else if (parms.facing == Rot4.East)
            {
                result += new Vector3(vector.x, 0f, vector.y);
            }
            else if (parms.facing == Rot4.West)
            {
                result += new Vector3(0f - vector.x, 0f, vector.y);
            }
        }

        if (host.story?.headType?.narrow == true && node.Props.narrowCrownHorizontalOffset != 0f && parms.facing.IsHorizontal)
        {
            if (parms.facing == Rot4.East)
            {
                result.x -= node.Props.narrowCrownHorizontalOffset;
            }
            else if (parms.facing == Rot4.West)
            {
                result.x += node.Props.narrowCrownHorizontalOffset;
            }
            result.z -= node.Props.narrowCrownHorizontalOffset;
        }

        if (!parms.Portrait && parms.swimming)
        {
            result.z -= 0.5f;
        }
        return result;
    }
}
