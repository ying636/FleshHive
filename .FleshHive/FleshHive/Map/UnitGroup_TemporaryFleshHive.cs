using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class UnitGroup_TemporaryFleshHive : UnitGroup_FleshHive
{
    public override bool CanDrawTarget => false;

    public override void DrawGroupAction(Rect rect)
    {
        TooltipHandler.TipRegion(rect, "FH_FleshManagement_RecallTemporary".Translate());
        if (Widgets.ButtonImage(rect, TexButton.Reload))
        {
            RecallUnits();
        }
    }

    public void RecallUnits()
    {
        List<Pawn> members = units.Where(unit => unit != null)
            .OrderByDescending(unit => HCFGameUtility.GetUnitComp(unit)?.Props.groupCost ?? 0)
            .ThenBy(unit => unit.thingIDNumber).ToList();
        if (members.Count == 0)
        {
            Messages.Message("FH_FleshManagement_RecallTemporaryEmpty".Translate(), MessageTypeDefOf.NeutralEvent, false);
            return;
        }
        List<UnitGroup> targetGroups = GameComponent_UnitGroup.Instance.groups
            .Where(group => group != null && group != this && group.Map == Map && group.Show
                && group.tags?.Contains(FleshHiveTags.Flesh) == true).ToList();
        if (targetGroups.Count == 0)
        {
            Messages.Message("FH_FleshManagement_RecallTemporaryNoHive".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        int recalledCount = 0;
        string? rejectReason = null;
        foreach (Pawn unit in members)
        {
            foreach (UnitGroup targetGroup in targetGroups)
            {
                AcceptReason reason = targetGroup.CanAccept(unit);
                if (reason.Accepted)
                {
                    unit.jobs?.StopAll();
                    if (unit.mindState != null)
                    {
                        unit.mindState.enemyTarget = null;
                        unit.mindState.duty = null;
                    }
                    if (unit.Spawned && unit.Map == targetGroup.Map)
                    {
                        targetGroup.AcceptUnit(unit);
                    }
                    else
                    {
                        var targetLord = targetGroup.lord;
                        targetGroup.lord = null;
                        try
                        {
                            targetGroup.AcceptUnit(unit);
                        }
                        finally
                        {
                            targetGroup.lord = targetLord;
                        }
                    }
                    recalledCount++;
                    break;
                }
                rejectReason ??= reason.Reason;
            }
        }
        int remainingCount = units.Count(unit => unit != null);
        TaggedString message = remainingCount > 0
            ? "FH_FleshManagement_RecallTemporaryPartial".Translate(recalledCount, remainingCount,
                rejectReason ?? "TargetGroupDontAccept".Translate())
            : "FH_FleshManagement_RecallTemporaryComplete".Translate(recalledCount);
        Messages.Message(message, remainingCount > 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.PositiveEvent, false);
    }
    public override void Make()
    {
        this.SetMode(HCFDefOf.HCF_GroupWorkMode_Attack);
    }

    public override void AcceptUnit(Pawn unit)
    {
        base.AcceptUnit(unit);
        if (unit != null)
        {
            unit.forceNoDeathNotification = true;
        }
    }

    public override void RemoveUnit(Pawn unit)
    {
        base.RemoveUnit(unit);
        if (unit != null)
        {
            unit.forceNoDeathNotification = false;
        }
    }

    public override IEnumerable<GroupWorkModeDef> GetModeDefs()
    {
        yield return HCFDefOf.HCF_GroupWorkMode_Attack;
    }
}
