using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompHostileHiveIncome : ThingComp
{
    public override void CompTick()
    {
        if (parent.Faction?.IsPlayer != false || !parent.Faction.HostileTo(Faction.OfPlayer))
            return;
        incomeTicks++;
        if (incomeTicks < 250)
            return;
        HiveResource? resource = parent.TryGetComp<CompHiveResource>()?.resources
            .FirstOrDefault(resource => resource.def == HostileFleshHiveDefOf.FH_Resource_HostileNutrition);
        resource?.IncreaseResource(10f * incomeTicks / GenDate.TicksPerDay);
        incomeTicks = 0;
    }

    public override string CompInspectStringExtra()
    {
        return "FH_HostileHive_DailyIncome".Translate() + ": 10";
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref incomeTicks, "FH_incomeTicks");
    }

    private int incomeTicks;
}
