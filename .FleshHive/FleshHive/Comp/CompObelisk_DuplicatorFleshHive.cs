using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace FleshHive;

public class CompObelisk_DuplicatorFleshHive : CompObelisk_Duplicator
{
    public override void TriggerInteractionEffect(Pawn interactor, bool triggeredByPlayer = false)
    {
        if (interactor.TryGetComp<CompFleshBeastCache>() == null)
        {
            base.TriggerInteractionEffect(interactor, triggeredByPlayer);
            return;
        }
        Map map = parent.Map;
        if (!CellFinder.TryFindRandomCellNear(parent.Position, map, 16,
                cell => cell.Standable(map) && cell.Walkable(map) && !cell.Fogged(map),
                out IntVec3 targetCell, 100))
        {
            return;
        }

        Pawn duplicate = DuplicateFleshbeast(interactor);
        int duplicateOf = interactor.duplicate.duplicateOf == int.MinValue
            ? interactor.thingIDNumber : interactor.duplicate.duplicateOf;
        interactor.duplicate.duplicateOf = duplicateOf;
        duplicate.duplicate.duplicateOf = duplicateOf;
        Find.PawnDuplicator.AddDuplicate(duplicateOf, interactor);
        Find.PawnDuplicator.AddDuplicate(duplicateOf, duplicate);
        map.effecterMaintainer.AddEffecterToMaintain(
            EffecterDefOf.Skip_EntryNoDelay.Spawn(targetCell, map), targetCell, 60);
        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(targetCell, map));
        GenSpawn.Spawn(duplicate, targetCell, map);
        UnitGroup? group = HCFGameUtility.GetUnitComp(duplicate)?.group;
        group?.RemoveUnit(duplicate);
        duplicate.GetLord()?.RemovePawn(duplicate);
        Lord assaultLord = Lord;
        if (group?.hive == duplicate)
        {
            if (group.lord != null)
            {
                group.lord.lordManager.RemoveLord(group.lord);
            }
            group.lord = assaultLord;
            group.AcceptUnit(duplicate);
        }
        else
        {
            assaultLord.AddPawn(duplicate);
        }
        lastInteractionEffectTick = Find.TickManager.TicksGame;
        Find.LetterStack.ReceiveLetter("ObeliskHostileDuplicateLetterLabel".Translate(),
            "ObeliskHostileDuplicateLetter".Translate(interactor.Named("PAWN")),
            LetterDefOf.ThreatBig, duplicate);
    }

    private static Pawn DuplicateFleshbeast(Pawn original)
    {
        float biologicalAge = original.ageTracker.AgeBiologicalYearsFloat;
        Pawn duplicate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            original.kindDef, original.Faction, forceGenerateNewPawn: true,
            canGeneratePawnRelations: false, fixedGender: original.gender,
            fixedBiologicalAge: biologicalAge,
            fixedChronologicalAge: Math.Min(original.ageTracker.AgeChronologicalYearsFloat, biologicalAge),
            forceNoGear: true));
        duplicate.SetFaction(Faction.OfEntities);
        if (original.Name != null)
        {
            duplicate.Name = new NameSingle(original.Name.ToString());
        }
        duplicate.health.hediffSet.hediffs.Clear();
        foreach (Hediff source in original.health.hediffSet.hediffs)
        {
            if (!source.def.duplicationAllowed || source is ParasitismHediff or ParasitismSystem
                || (source.Part != null && !duplicate.health.hediffSet.HasBodyPart(source.Part))
                || (source is Hediff_AddedPart or Hediff_Implant && !source.def.organicAddedBodypart))
            {
                continue;
            }
            Hediff hediff = HediffMaker.MakeHediff(source.def, duplicate, source.Part);
            hediff.CopyFrom(source);
            duplicate.health.hediffSet.AddDirect(hediff);
        }
        foreach (Hediff source in original.health.hediffSet.hediffs)
        {
            if (source is Hediff_AddedPart && !source.def.organicAddedBodypart)
            {
                duplicate.health.RestorePart(source.Part, null, checkStateChange: false);
            }
        }
        if (original.skills != null && duplicate.skills != null)
        {
            duplicate.skills.skills.Clear();
            foreach (SkillRecord skill in original.skills.skills)
            {
                duplicate.skills.skills.Add(new SkillRecord(duplicate, skill.def)
                {
                    levelInt = skill.levelInt,
                    passion = skill.passion,
                    xpSinceLastLevel = skill.xpSinceLastLevel,
                    xpSinceMidnight = skill.xpSinceMidnight
                });
            }
        }
        if (original.abilities != null)
        {
            duplicate.abilities ??= new Pawn_AbilityTracker(duplicate);
            foreach (Ability ability in original.abilities.abilities)
            {
                if (duplicate.abilities.GetAbility(ability.def) == null)
                {
                    duplicate.abilities.GainAbility(ability.def);
                }
            }
            foreach (Ability ability in duplicate.abilities.abilities.ToArray())
            {
                if (original.abilities.GetAbility(ability.def) == null)
                {
                    duplicate.abilities.RemoveAbility(ability.def);
                }
            }
        }
        foreach (Need need in duplicate.needs.AllNeeds)
        {
            Need? source = original.needs.AllNeeds.FirstOrDefault(candidate => candidate.def == need.def);
            if (source != null)
            {
                need.CurLevel = source.CurLevel;
            }
        }
        if (original.TryGetComp<CompTwistedFlesh>() is { } originalStorage
            && duplicate.TryGetComp<CompTwistedFlesh>() is { } duplicateStorage)
        {
            duplicateStorage.SetAmount(FleshHiveDefOf.FH_Resource_TwistedFlesh, originalStorage.CurrentTwistedFlesh);
        }
        duplicate.Notify_DuplicatedFrom(original);
        duplicate.Drawer.renderer.SetAllGraphicsDirty();
        duplicate.Notify_DisabledWorkTypesChanged();
        return duplicate;
    }
}
