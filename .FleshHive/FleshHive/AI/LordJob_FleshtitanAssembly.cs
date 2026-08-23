using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class LordJob_FleshtitanAssembly : LordJob
{
    public LordJob_FleshtitanAssembly()
    {
    }

    public LordJob_FleshtitanAssembly(IntVec3 assemblyPoint)
    {
        this.assemblyPoint = assemblyPoint;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();
        graph.AddToil(assemblyPoint.IsValid
            ? new LordToil_Travel(assemblyPoint)
            : new LordToil_Wait(allowRandomInteractions: false));
        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref assemblyPoint, "assemblyPoint");
    }

    private IntVec3 assemblyPoint = IntVec3.Invalid;
}
