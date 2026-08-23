using HiveCreatureFramework;
using HiveCreatureFramework.Evolution;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FleshHiveEvolution : CompPropertiesHiveEvolution
{
    public CompProperties_FleshHiveEvolution()
    {
        compClass = typeof(CompFleshHiveEvolution);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }

        if (research == null)
        {
            yield return $"{parentDef.defName} requires a research project for flesh hive evolution.";
        }

        if (evolution == null)
        {
            yield return $"{parentDef.defName} requires an evolution option for flesh hive evolution.";
        }
    }

    public ResearchProjectDef? research;
    public HiveEvolutionOptionDef? evolution;
}

public class CompFleshHiveEvolution : CompHiveEvolution
{
    public new CompProperties_FleshHiveEvolution Props => (CompProperties_FleshHiveEvolution)props;

    public bool CanShowEvolutionButton => parent.Faction == Faction.OfPlayer
        && Props.research?.IsFinished == true
        && parent.Map != null
        && !parent.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest)
            .Any(thing => thing.Faction == parent.Faction);

    public Command_Action CreateEvolutionCommand()
    {
        Command_Action command = new()
        {
            defaultLabel = "FH_FleshHive_EvolutionButton".Translate(),
            defaultDesc = "FH_FleshHive_EvolutionButtonDesc".Translate(),
            icon = FleshHiveDefOf.FH_FleshPrimaryNest.uiIcon,
            action = StartEvolution
        };

        if (Props.evolution == null || Progress == null)
        {
            DisableCommand(command, "FH_FleshHive_EvolutionUnavailable".Translate());
            return command;
        }

        if (Progress?.progresses.OfType<HiveEvolutionProgress>()
                .Any(progress => progress.def == Props.evolution) == true)
        {
            DisableCommand(command, "FH_FleshHive_EvolutionInProgress".Translate());
            return command;
        }

        AcceptReason report = CanStartEvolution(Props.evolution);
        if (!report.Accepted)
        {
            DisableCommand(command, report.Reason);
        }

        return command;
    }

    private void StartEvolution()
    {
        HiveEvolutionOptionDef? evolution = Props.evolution;
        if (evolution == null || Progress == null)
        {
            NotifyEvolutionError("FH_FleshHive_EvolutionUnavailable".Translate());
            return;
        }

        if (Progress.progresses.OfType<HiveEvolutionProgress>()
            .Any(progress => progress.def == evolution))
        {
            Messages.Message("FH_FleshHive_EvolutionInProgress".Translate(), parent,
                MessageTypeDefOf.RejectInput);
            return;
        }

        AcceptReason report = CanStartEvolution(evolution);
        if (!report.Accepted)
        {
            Messages.Message(report.Reason, parent, MessageTypeDefOf.RejectInput);
            return;
        }

        if (!evolution.requiredThings.NullOrEmpty()
            && Resource?.ConsumeRequiredItems(evolution.requiredThings) != true)
        {
            NotifyEvolutionError("FH_FleshHive_EvolutionResourceChanged".Translate());
            return;
        }

        if (!evolution.resourceCosts.NullOrEmpty())
        {
            Resource?.ConsumeResources(evolution.resourceCosts);
        }

        Progress.progresses.Add(new HiveEvolutionProgress
        {
            time = evolution.progressTick,
            totalTime = evolution.progressTick,
            def = evolution
        });
        Messages.Message("AddEvolutionProgress".Translate(), parent, MessageTypeDefOf.PositiveEvent);
    }

    private AcceptReason CanStartEvolution(HiveEvolutionOptionDef evolution)
    {
        if (evolution.resultThing == null)
        {
            return AcceptReason.False("HCF_Evolution_InvalidResult".Translate());
        }

        if (Resource == null
            && (!evolution.resourceCosts.NullOrEmpty() || !evolution.requiredThings.NullOrEmpty()))
        {
            return AcceptReason.False("NoResources".Translate());
        }

        if (Resource != null)
        {
            if (!Resource.HasRequiredResource(evolution.resourceCosts, out string resourceReason))
            {
                return AcceptReason.False(resourceReason);
            }

            if (!Resource.HasRequiredItems(evolution.requiredThings, out string itemReason))
            {
                return AcceptReason.False(itemReason);
            }
        }

        foreach (ChangeCondition condition in evolution.changeConditions)
        {
            AcceptReason report = condition.CanChange(parent, this);
            if (!report.Accepted)
            {
                return report;
            }
        }

        return AcceptReason.True;
    }

    private static void DisableCommand(Command_Action command, string reason)
    {
        string displayReason = reason.NullOrEmpty()
            ? "FH_FleshHive_EvolutionUnknownReason".Translate()
            : reason;
        command.defaultDesc += "\n\n" + "FH_FleshHive_EvolutionBlockedReason".Translate(displayReason);
        command.Disable(displayReason);
    }

    private void NotifyEvolutionError(TaggedString message)
    {
        Messages.Message(message, parent, MessageTypeDefOf.RejectInput);
        Log.Error($"[FleshHive] {message}");
    }
}
