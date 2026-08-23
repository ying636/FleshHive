using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class LordJob_GiantFleshbeastAssault : LordJob
{
    public LordJob_GiantFleshbeastAssault()
    {
    }

    public LordJob_GiantFleshbeastAssault(Pawn leader)
    {
        this.leader = leader;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new();
        graph.AddToil(new LordToil_GiantFleshbeastAssault(leader));
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref leader, "leader");
    }

    private Pawn leader;
}
