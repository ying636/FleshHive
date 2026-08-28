using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public class ScenPart_StartingFleshSurvivors : ScenPart
{
    public override void PostGameStart()
    {
        base.PostGameStart();
        GameComponent_FleshSurvivor? component = Current.Game?.GetComponent<GameComponent_FleshSurvivor>();
        if (component == null)
        {
            Log.Error("[FleshHive] Could not find GameComponent_FleshSurvivor while starting the flesh survivor scenario.");
            return;
        }

        component.DisableQuest();
    }

    public override IEnumerable<Thing> PlayerStartingThings()
    {
        if (startingBeasts != null)
        {
            return startingBeasts;
        }

        startingBeasts = new List<Pawn>
        {
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Puffspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Puffspike, Faction.OfPlayer)
        };

        Pawn? hela = Find.GameInitData?.startingAndOptionalPawns
            .FirstOrDefault(pawn => pawn.IsMutant && pawn.mutant.Def == FleshHiveDefOf.FH_HelaSubhuman);
        HediffComp_HelaNode? helaNode = hela?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela)
            ?.TryGetComp<HediffComp_HelaNode>();
        if (helaNode != null)
        {
            helaNode.QueueStartingUnits(startingBeasts);
        }

        return startingBeasts;
    }

    public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
    {
        base.Notify_PawnGenerated(pawn, context, redressed);
        if (context == PawnGenerationContext.PlayerStarter
            && pawn.IsMutant
            && pawn.mutant.Def == FleshHiveDefOf.FH_HelaSubhuman)
        {
            FleshSurvivorHelaGenerator.Configure(pawn);
        }
    }

    public override void PostMapGenerate(Map map)
    {
        base.PostMapGenerate(map);
        Pawn? hela = Find.GameInitData?.startingAndOptionalPawns
            .FirstOrDefault(pawn => pawn.IsMutant && pawn.mutant.Def == FleshHiveDefOf.FH_HelaSubhuman);
        if (hela == null)
        {
            Log.Error("[FleshHive] Could not find the starting Hela pawn for the flesh survivor scenario.");
            return;
        }

        HediffComp_HelaNode? helaNode = hela.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela)
            ?.TryGetComp<HediffComp_HelaNode>();
        if (helaNode == null)
        {
            Log.Error("[FleshHive] Starting Hela has no Hela control node.");
            return;
        }

        helaNode.QueueStartingUnits(startingBeasts ?? new List<Pawn>());
    }

    private List<Pawn>? startingBeasts;
}
