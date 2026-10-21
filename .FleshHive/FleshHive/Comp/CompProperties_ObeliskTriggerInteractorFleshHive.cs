using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_ObeliskTriggerInteractorFleshHive : CompProperties_ObeliskTriggerInteractor
{
    public override void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        Predicate<TargetInfo>? validator = targetingParameters.validator;
        targetingParameters.validator = target =>
            (validator == null || validator(target)) && target.Thing is Pawn pawn
            && ((pawn.IsColonistPlayerControlled && !pawn.NonHumanlikeOrWildMan())
                || (pawn.Faction == Faction.OfPlayer && pawn.TryGetComp<CompFleshBeastCache>() != null));
        targetingParameters.canTargetAnimals = true;
        targetingParameters.onlyTargetControlledPawns = false;
    }
}
