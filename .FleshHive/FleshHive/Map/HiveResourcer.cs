using System.Linq;
using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveResourcer : IExposable
{
    public HiveResourcer()
    {
    }

    public HiveResourcer(Thing sourceHive, Blueprint_FleshBuild targetBlueprint, HiveResourceDef carriedResourceDef, float carriedAmount)
    {
        this.sourceHive = sourceHive;
        this.targetBlueprint = targetBlueprint;
        this.carriedResourceDef = carriedResourceDef;
        this.carriedAmount = carriedAmount;
    }

    public HiveResourcer(Thing sourceHive, Blueprint_FleshBuild targetBlueprint, ThingDef carriedThingDef, int carriedCount)
    {
        this.sourceHive = sourceHive;
        this.targetBlueprint = targetBlueprint;
        this.carriedThingDef = carriedThingDef;
        this.carriedAmount = carriedCount;
    }

    public bool CanDraw => sourceHive?.Spawned == true && targetBlueprint?.Spawned == true && carriedAmount > 0f;

    public bool Tick()
    {
        if (carriedAmount <= 0f)
        {
            return true;
        }

        if (targetBlueprint?.Spawned != true)
        {
            Refund();
            return true;
        }

        if (sourceHive?.Spawned != true)
        {
            Refund();
            return true;
        }

        progress = Mathf.Min(1f, progress + GetProgressPerTick());
        if (progress < 1f)
        {
            return false;
        }

        Deliver();
        return true;
    }

    public void Draw()
    {
        if (!CanDraw)
        {
            return;
        }

        Vector3 basePos = GetDrawPos(AltitudeLayer.MetaOverlays.AltitudeFor());
        Vector3 resourcePos = GetDrawPos(AltitudeLayer.MetaOverlays.AltitudeFor(0.45f));
        Vector3 overlayPos = GetDrawPos(AltitudeLayer.MetaOverlays.AltitudeFor(0.9f));

        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(basePos, Quaternion.identity, new Vector3(BaseScale, 1f, BaseScale)), FleshHiveTex.FleshBoxTop, 0);

        Graphic topGraphic = GetTopGraphic();
        if (topGraphic != null)
        {
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(resourcePos, Quaternion.identity, new Vector3(ResourceScale, 1f, ResourceScale)), topGraphic.MatAt(Rot4.North, null), 0);
        }

        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(overlayPos, Quaternion.identity, new Vector3(TopScale, 1f, TopScale)), FleshHiveTex.FleshBoxBottom, 0);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref sourceHive, "sourceHive");
        Scribe_References.Look(ref targetBlueprint, "targetBlueprint");
        Scribe_Defs.Look(ref carriedResourceDef, "carriedResourceDef");
        Scribe_Defs.Look(ref carriedThingDef, "carriedThingDef");
        Scribe_Values.Look(ref carriedAmount, "carriedAmount", 0f);
        Scribe_Values.Look(ref progress, "progress", 0f);
    }

    private void Deliver()
    {
        if (targetBlueprint == null || carriedAmount <= 0f)
        {
            carriedAmount = 0f;
            return;
        }

        if (carriedResourceDef != null)
        {
            float delivered = targetBlueprint.ReceiveResource(carriedResourceDef, carriedAmount);
            carriedAmount = Mathf.Max(0f, carriedAmount - delivered);
            return;
        }

        if (carriedThingDef != null)
        {
            int delivered = targetBlueprint.ReceiveThing(carriedThingDef, Mathf.RoundToInt(carriedAmount));
            carriedAmount = Mathf.Max(0f, carriedAmount - delivered);
            return;
        }

        carriedAmount = 0f;
    }

    private void Refund()
    {
        Map map = targetBlueprint?.MapHeld ?? sourceHive?.MapHeld;
        IntVec3 dropCell = GetDropCell(map);

        if (carriedThingDef != null &&
            carriedAmount > 0f)
        {
            int remainingCount = Mathf.RoundToInt(carriedAmount);
            while (remainingCount > 0)
            {
                Thing thing = ThingMaker.MakeThing(carriedThingDef);
                thing.stackCount = Mathf.Min(remainingCount, carriedThingDef.stackLimit);
                DropThing(map, dropCell, thing);
                remainingCount -= thing.stackCount;
            }
        }

        if (carriedResourceDef != null &&
            carriedAmount > 0f)
        {
            DropResource(map, dropCell);
        }

        carriedAmount = 0f;
    }

    private Vector3 GetDrawPos(float altitude)
    {
        Vector3 start = sourceHive.DrawPos;
        Vector3 end = targetBlueprint.DrawPos;
        Vector3 drawPos = Vector3.Lerp(start, end, progress);
        drawPos.y = altitude;
        return drawPos;
    }

    private float GetProgressPerTick()
    {
        float distance = Mathf.Max(0.1f, sourceHive.Position.DistanceTo(targetBlueprint.Position));
        return TravelSpeedPerTick / distance;
    }

    private Graphic GetTopGraphic()
    {
        if (carriedResourceDef?.graphic?.Graphic != null)
        {
            return carriedResourceDef.graphic.Graphic;
        }

        ThingDef displayThing = GetDisplayThingDef();
        if (displayThing?.graphic == null)
        {
            return null;
        }

        return displayThing.graphic;
    }

    private ThingDef GetDisplayThingDef()
    {
        if (carriedThingDef != null)
        {
            return carriedThingDef;
        }

        if (carriedResourceDef != null)
        {
            return carriedResourceDef.thing;
        }

        return null;
    }

    private IntVec3 GetDropCell(Map map)
    {
        if (map == null)
        {
            return IntVec3.Invalid;
        }

        IntVec3 sourceCell = sourceHive?.PositionHeld ?? IntVec3.Invalid;
        IntVec3 targetCell = targetBlueprint?.PositionHeld ?? IntVec3.Invalid;
        if (sourceCell.IsValid && targetCell.IsValid)
        {
            IntVec3 cell = new IntVec3(
                Mathf.RoundToInt(Mathf.Lerp(sourceCell.x, targetCell.x, progress)),
                0,
                Mathf.RoundToInt(Mathf.Lerp(sourceCell.z, targetCell.z, progress)));
            if (cell.InBounds(map))
            {
                return cell;
            }
        }

        if (sourceCell.IsValid && sourceCell.InBounds(map))
        {
            return sourceCell;
        }

        if (targetCell.IsValid && targetCell.InBounds(map))
        {
            return targetCell;
        }

        return sourceHive?.PositionHeld.IsValid == true ? sourceHive.PositionHeld : targetBlueprint?.PositionHeld ?? IntVec3.Invalid;
    }

    private void DropThing(Map map, IntVec3 dropCell, Thing thing)
    {
        if (thing == null)
        {
            return;
        }

        if (map == null || !dropCell.IsValid || !dropCell.InBounds(map))
        {
            thing.Destroy();
            return;
        }

        if (!GenPlace.TryPlaceThing(thing, dropCell, map, ThingPlaceMode.Near))
        {
            thing.Destroy();
        }
    }

    private void DropResource(Map map, IntVec3 dropCell)
    {
        if (carriedResourceDef?.thing == null ||
            carriedResourceDef.thingCountPerResource <= 0f)
        {
            return;
        }

        int remainingCount = Mathf.FloorToInt(carriedAmount * carriedResourceDef.thingCountPerResource);
        while (remainingCount > 0)
        {
            Thing thing = ThingMaker.MakeThing(carriedResourceDef.thing);
            thing.stackCount = Mathf.Min(remainingCount, carriedResourceDef.thing.stackLimit);
            DropThing(map, dropCell, thing);
            remainingCount -= thing.stackCount;
        }
    }

    public Thing sourceHive;
    public Blueprint_FleshBuild targetBlueprint;
    public HiveResourceDef carriedResourceDef;
    public ThingDef carriedThingDef;
    public float carriedAmount;
    public float progress;

    private const float BaseScale = 0.65f;
    private const float ResourceScale = 0.42f;
    private const float TopScale = 0.65f;
    private const float TravelSpeedPerTick = 0.06f;
}
