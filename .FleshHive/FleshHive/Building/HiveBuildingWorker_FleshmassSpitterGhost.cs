using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_FleshmassSpitterGhost : HiveBuildingWorker_FleshBlueprint
{
    public override void DrawGhost(IntVec3 center, Rot4 rot, Map map, Color ghostCol)
    {
        DrawSpitterGhost(center, ghostCol);
        DrawPlaceWorkerGhosts(center, rot, ghostCol);
    }

    private void DrawSpitterGhost(IntVec3 center, Color ghostCol)
    {
        Vector3 drawPos = GenThing.TrueCenter(center, Rot4.North, SpitterSize, AltitudeLayer.Blueprint.AltitudeFor());
        drawPos += SpitterDrawOffset;
        Material material = MaterialPool.MatFrom(SpitterTexPath, ShaderDatabase.Transparent, ghostCol);
        Graphics.DrawMesh(MeshPool.GridPlane(SpitterDrawSize), drawPos, Quaternion.identity, material, 0);
    }

    private void DrawPlaceWorkerGhosts(IntVec3 center, Rot4 rot, Color ghostCol)
    {
        if (def.placeWorkers.NullOrEmpty())
        {
            return;
        }

        foreach (PlaceWorker placeWorker in def.placeWorkers)
        {
            placeWorker.DrawGhost(def.building, center, rot, ghostCol);
        }
    }

    private const string SpitterTexPath = "Things/Building/Fleshmass/FleshmassSpitter/FleshmassSpitter_A";
    private static readonly IntVec2 SpitterSize = new(2, 2);
    private static readonly Vector2 SpitterDrawSize = new(2.5f, 2.5f);
    private static readonly Vector3 SpitterDrawOffset = new(0f, 0.02f, 0f);
}
