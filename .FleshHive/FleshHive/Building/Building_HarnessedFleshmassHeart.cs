using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class Building_HarnessedFleshmassHeart : Building
{
    private Graphic CenterPartGraphic => cachedCenterPartGraphic ??=
        GraphicDatabase.Get<Graphic_Multi>(
            "Things/Building/FH_HarnessedFleshmassHeart/HarnessedFleshmassHeart_CentralPart",
            ShaderDatabase.Cutout,
            PartDrawSize,
            Color.white);

    private Graphic LeftPartGraphic => cachedLeftPartGraphic ??=
        GraphicDatabase.Get<Graphic_Multi>(
            "Things/Building/FH_HarnessedFleshmassHeart/HarnessedFleshmassHeart_LeftPart",
            ShaderDatabase.Cutout,
            PartDrawSize,
            Color.white);

    private Graphic RightPartGraphic => cachedRightPartGraphic ??=
        GraphicDatabase.Get<Graphic_Multi>(
            "Things/Building/FH_HarnessedFleshmassHeart/HarnessedFleshmassHeart_RightPart",
            ShaderDatabase.Cutout,
            PartDrawSize,
            Color.white);

    protected override void Tick()
    {
        base.Tick();
        if (Find.TickManager.TicksGame > lastBeatTick + TicksPerBeat)
        {
            Beat();
        }
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);

        Matrix4x4 matrix = Matrix4x4.TRS(
            drawLoc + new Vector3(0f, 0.35f, -0.35f),
            Quaternion.identity,
            new Vector3(GetBeatScale(1.15f), 1f, GetBeatScale(1.2f)));
        GenDraw.DrawMeshNowOrLater(CenterPartGraphic.MeshAt(Rot4.South), matrix, CenterPartGraphic.MatSouth, false);

        matrix = Matrix4x4.TRS(
            drawLoc + new Vector3(-0.875f, 0.3f, 0f),
            Quaternion.AngleAxis(30f, Vector3.up),
            new Vector3(GetBeatScale(1.1f, 3), 1f, GetBeatScale(1.1f, 3)));
        GenDraw.DrawMeshNowOrLater(LeftPartGraphic.MeshAt(Rot4.South), matrix, LeftPartGraphic.MatSouth, false);

        matrix = Matrix4x4.TRS(
            drawLoc + new Vector3(0.875f, 0.3f, 0f),
            Quaternion.AngleAxis(10f, Vector3.up),
            new Vector3(GetBeatScale(1.1f, 7), 1f, GetBeatScale(1.1f, 7)));
        GenDraw.DrawMeshNowOrLater(RightPartGraphic.MeshAt(Rot4.South), matrix, RightPartGraphic.MatSouth, false);
    }

    private void Beat()
    {
        lastBeatTick = Find.TickManager.TicksGame;
        SoundDefOf.FleshmassHeart_Throb.PlayOneShot(this);
    }

    private float GetBeatScale(float maxScale, int delay = 0)
    {
        float progress = (float)(Find.TickManager.TicksGame - (lastBeatTick + delay)) / BeatDurationTicks;
        return progress < 1f ? Mathf.Lerp(1f, maxScale, Mathf.Sin(Mathf.PI * progress)) : 1f;
    }

    private const int BeatDurationTicks = 15;

    private const int TicksPerBeat = 120;

    private static readonly Vector3 PartDrawSize = new Vector3(3.5f, 3.5f, 3.5f);

    private int lastBeatTick = -99999;

    private Graphic cachedCenterPartGraphic;

    private Graphic cachedLeftPartGraphic;

    private Graphic cachedRightPartGraphic;
}
