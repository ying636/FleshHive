using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public class GenStep_Dreadmeld_FleshHive : GenStep_Dreadmeld
{
    public override void Generate(Map map, GenStepParams parms)
    {
        PocketMapExit pitGateExit = (PocketMapExit)map.listerThings.ThingsOfDef(ThingDefOf.CaveExit).First();
        CellFinder.TryFindRandomCell(
            map,
            c => c.Standable(map) && !c.InHorDistOf(pitGateExit.Position, 20f) && c.DistanceToEdge(map) > 10,
            out IntVec3 result);
        List<IntVec3> fleshmassCells = GridShapeMaker.IrregularLump(result, map, 100);
        List<IntVec3> interiorCells = GridShapeMaker.IrregularLump(result, map, 20);
        foreach (IntVec3 cell in fleshmassCells)
        {
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Fleshmass), cell, map, Rot4.Random).SetFaction(Faction.OfEntities);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Flesh);
        }

        foreach (IntVec3 cell in interiorCells)
        {
            foreach (Thing thing in cell.GetThingList(map).ToList().Where(t => t.def.destroyable))
            {
                thing.Destroy();
            }
        }

        int numFleshBulbs = new IntRange(2, 4).RandomInRange;
        for (int i = 0; i < numFleshBulbs; i++)
        {
            if (CellFinder.TryFindRandomCellNear(result, map, 5, c => c.GetEdifice(map) == null, out IntVec3 bulbCell))
            {
                GenSpawn.Spawn(ThingDefOf.Fleshbulb, bulbCell, map).SetFaction(Faction.OfEntities);
            }
        }

        Pawn mother = PawnGenerator.GeneratePawn(new PawnGenerationRequest(GetMotherKind(), Faction.OfEntities));
        FleshParasiteUtility.TryApplyDefaultParasites(mother);
        GenSpawn.Spawn(mother, result, map, Rot4.Random);

        string signalTag = "dreadmeldApproached-" + Find.UniqueIDsManager.GetNextSignalTagID();
        CellRect rect = CellRect.FromCellList(fleshmassCells).ExpandedBy(2).ClipInsideMap(map);
        RectTrigger rectTrigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
        rectTrigger.signalTag = signalTag;
        rectTrigger.Rect = rect;
        rectTrigger.destroyIfUnfogged = true;
        GenSpawn.Spawn(rectTrigger, rect.CenterCell, map);
        SignalAction_Letter signalActionLetter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter);
        signalActionLetter.signalTag = signalTag;
        signalActionLetter.letterDef = LetterDefOf.ThreatBig;
        signalActionLetter.letterLabelKey = "LetterLabelDreadmeldWarning";
        signalActionLetter.letterMessageKey = "LetterDreadmeldWarning";
        GenSpawn.Spawn(signalActionLetter, rect.CenterCell, map);
    }

    private static PawnKindDef GetMotherKind()
    {
        motherKinds ??= new List<PawnKindDef>
        {
            DefDatabase<PawnKindDef>.GetNamed("FH_Nexusmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Furiousmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Bastionmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Fissionmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Dreadmeld")
        };
        return motherKinds.RandomElement();
    }

    private static List<PawnKindDef> motherKinds;
}
