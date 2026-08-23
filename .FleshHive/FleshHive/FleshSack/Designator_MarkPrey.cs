using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Designator_MarkPrey : Designator_Cells
{
    public Designator_MarkPrey()
    {
        defaultLabel = "FH_MarkPrey".Translate();
        defaultDesc = "FH_MarkPreyDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Designators/FH_MarkPrey");
        useMouseIcon = true;
        soundSucceeded = SoundDefOf.Designate_Mine;
    }

    protected override DesignationDef Designation => FleshHiveDefOf.FH_MarkPrey;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public override bool DragDrawMeasurements => true;

    public override AcceptanceReport CanDesignateThing(Thing t)
    {
        if (t is not Pawn pawn)
        {
            return false;
        }
        if (!pawn.Downed || !pawn.Spawned)
        {
            return "FH_MarkPrey_NotDowned".Translate();
        }
        if (!pawn.RaceProps.IsFlesh || pawn.RaceProps.IsMechanoid)
        {
            return "FH_MarkPrey_NotFlesh".Translate();
        }
        if (pawn.MapHeld.designationManager.DesignationOn(pawn, FleshHiveDefOf.FH_MarkPrey) != null)
        {
            return "FH_MarkPrey_AlreadyMarked".Translate();
        }
        return true;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!c.InBounds(Map) || c.Fogged(Map))
        {
            return false;
        }
        foreach (Thing thing in c.GetThingList(Map))
        {
            if (CanDesignateThing(thing).Accepted)
            {
                return true;
            }
        }
        return false;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        foreach (Thing thing in c.GetThingList(Map))
        {
            if (CanDesignateThing(thing).Accepted)
            {
                DesignateThing(thing);
            }
        }
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        foreach (IntVec3 cell in cells)
        {
            DesignateSingleCell(cell);
        }
    }

    public override void DesignateThing(Thing t)
    {
        var map = t.MapHeld;
        var des = map.designationManager.DesignationOn(t, FleshHiveDefOf.FH_MarkPrey);
        if (des != null)
        {
            map.designationManager.RemoveDesignation(des);
        }
        else
        {
            map.designationManager.AddDesignation(new Designation(t, FleshHiveDefOf.FH_MarkPrey));
        }
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
    }
}
