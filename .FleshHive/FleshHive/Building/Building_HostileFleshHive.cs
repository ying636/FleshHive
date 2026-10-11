using System.Text;
using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class Building_HostileFleshHive : Building_Hive
{
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map map = Map;
        base.DeSpawn(mode);
        map.GetComponent<MapComponent_HostileFleshHive>().RemoveInactiveColonies();
    }

    public override string GetInspectString()
    {
        StringBuilder text = new(base.GetInspectString());
        if (this.TryGetComp<CompHiveResource>() is { } resources)
        {
            foreach (HiveResource resource in resources.resources)
            {
                if (text.Length > 0)
                {
                    text.AppendLine();
                }
                text.Append($"{resource.def.LabelCap}: {resource.Amount:0.##}/{resource.GetLimit():0.##}");
            }
        }
        if (Map?.GetComponent<MapComponent_HiveLord>().GetLord(this) is { } lord)
        {
            if (text.Length > 0)
            {
                text.AppendLine();
            }
            text.Append("FH_HiveOwnedFleshbeasts".Translate(lord.Population));
            text.Append($" / {lord.def.maxUnits}");
            HostileFleshHiveColony colony = Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
            text.AppendLine();
            text.Append(colony.GetStatus());
            text.AppendLine();
            text.Append($"{"FH_HostileHive_Treasury".Translate()}: {colony.Nutrition:0.##}");
        }
        return text.ToString();
    }
}
