using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class LordJob_FleshtitanAssault : LordJob
{
    public LordJob_FleshtitanAssault()
    {
    }

    public LordJob_FleshtitanAssault(Pawn fleshtitan)
    {
        this.fleshtitan = fleshtitan;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();
        graph.AddToil(new LordToil_FleshtitanAssault(fleshtitan));
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref fleshtitan, "fleshtitan");
    }

    private Pawn fleshtitan = null!;
}
