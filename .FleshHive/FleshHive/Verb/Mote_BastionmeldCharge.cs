using System;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Mote_BastionmeldCharge : Mote
{
    public Action<Mote_BastionmeldCharge> UpdatePositionAndRotationAction { get; set; }

    public override void DynamicDrawPhaseAt(DrawPhase drawPhase, Vector3 drawLoc, bool flip)
    {
        this.UpdatePositionAndRotationAction?.Invoke(this);
        base.DynamicDrawPhaseAt(drawPhase, this.exactPosition, flip);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        this.UpdatePositionAndRotationAction?.Invoke(this);
        base.DrawAt(this.exactPosition, flip);
    }
}
