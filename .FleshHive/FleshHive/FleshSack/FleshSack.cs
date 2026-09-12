using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FleshSack : Building, IThingHolder
{
    public FleshSack()
    {
        contents = new ThingOwner<Thing>(this);
    }

    public bool IsDigesting => contents.Any && !digestionFinished;

    public bool CanAcceptMore => !contents.Any;

    protected override void Tick()
    {
        base.Tick();
        if (!contents.Any || digestionFinished)
        {
            return;
        }
        digestionProgress++;
        float nutritionPerTick = 20f / 60000f * MapComponent_FleshHive.GetNutritionAbsorptionFactor(Map);
        MapComponent_FleshHive.AddNutrition(Map, nutritionPerTick);
        if (digestionProgress >= totalDigestionTime)
        {
            EjectCorpse();
        }
    }

    public bool InsertPawn(Pawn pawn)
    {
        if (!contents.TryAddOrTransfer(pawn))
        {
            return false;
        }
        float bodySize = pawn.BodySize;
        totalDigestionTime = Mathf.RoundToInt(bodySize * 5f * 60000f);
        digestionProgress = 0;
        digestionFinished = false;
        pawn.Kill(new DamageInfo(DamageDefOf.Crush, 99999f));
        return true;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Faction == Faction.OfPlayer)
        {
            yield return new Designator_MarkPrey();
        }

        if (!DebugSettings.ShowDevGizmos || !IsDigesting)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "FH_DevCompleteFleshSackDigestion".Translate(),
            defaultDesc = "FH_DevCompleteFleshSackDigestionDesc".Translate(),
            action = CompleteDigestion
        };
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        string baseStr = base.GetInspectString();
        if (!baseStr.NullOrEmpty())
        {
            sb.AppendLine(baseStr);
        }
        if (IsDigesting)
        {
            float pct = (float)digestionProgress / totalDigestionTime;
            sb.AppendLine("FH_FleshSack_Digesting".Translate(pct.ToStringPercent()));
            if (contents[0] is Corpse c && c.InnerPawn != null)
            {
                sb.AppendLine("FH_FleshSack_Inner".Translate(c.InnerPawn.LabelShort));
            }
        }
        else if (!contents.Any)
        {
            sb.AppendLine("FH_FleshSack_Empty".Translate());
        }
        return sb.ToString().TrimEnd();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref contents, "contents", this);
        Scribe_Values.Look(ref digestionProgress, "digestionProgress");
        Scribe_Values.Look(ref totalDigestionTime, "totalDigestionTime");
        Scribe_Values.Look(ref digestionFinished, "digestionFinished");
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return contents;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    private void CompleteDigestion()
    {
        if (!IsDigesting)
        {
            return;
        }

        int remainingTicks = Mathf.Max(totalDigestionTime - digestionProgress, 0);
        if (remainingTicks > 0)
        {
            float nutritionPerTick = 20f / 60000f * MapComponent_FleshHive.GetNutritionAbsorptionFactor(Map);
            MapComponent_FleshHive.AddNutrition(Map, remainingTicks * nutritionPerTick);
        }

        EjectCorpse();
    }

    private void EjectCorpse()
    {
        if (!contents.Any)
        {
            return;
        }
        if (contents[0] is Corpse corpse)
        {
            corpse.GetComp<CompRottable>()?.RotImmediately(RotStage.Dessicated);
        }
        contents.TryDropAll(Position, Map, ThingPlaceMode.Near);
        digestionProgress = 0;
        digestionFinished = false;
    }

    public ThingOwner<Thing> contents;

    public int digestionProgress;

    public int totalDigestionTime;

    public bool digestionFinished;
}
