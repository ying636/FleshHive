using RimWorld;

namespace FleshHive;

public class ITab_FleshReplicaGear : ITab_Pawn_Gear
{
    public override bool IsVisible => SelPawn is FleshReplicaUnit { equipment: not null };
}
