using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class LordJob_FuriousmeldAssault : LordJob
{
    public LordJob_FuriousmeldAssault()
    {
    }

    public LordJob_FuriousmeldAssault(Pawn furiousmeld)
    {
        this.furiousmeld = furiousmeld;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new();
        graph.AddToil(new LordToil_FuriousmeldAssault(furiousmeld));
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref furiousmeld, "furiousmeld");
    }

    private Pawn furiousmeld;
}
