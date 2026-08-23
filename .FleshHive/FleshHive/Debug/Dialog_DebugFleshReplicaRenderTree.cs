using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Dialog_DebugFleshReplicaRenderTree : Window
{
    public override Vector2 InitialSize => new Vector2(760f, 720f);

    public Dialog_DebugFleshReplicaRenderTree(FleshReplicaUnit replica)
    {
        this.replica = replica;
        this.forcePause = true;
        this.doCloseX = true;
        this.closeOnClickedOutside = true;
        this.absorbInputAroundWindow = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "Flesh replica render tree");
        Text.Font = GameFont.Small;

        PawnRenderTree? tree = replica.DebugHostRenderTree;
        Rect buttonRect = new Rect(inRect.x, inRect.y + 38f, 120f, 28f);
        if (Widgets.ButtonText(buttonRect, "Refresh"))
        {
            tree?.SetDirty();
            replica.DebugEnsureHostRendererInitialized();
            tree = replica.DebugHostRenderTree;
        }

        Rect infoRect = new Rect(inRect.x, buttonRect.yMax + 8f, inRect.width, 110f);
        Widgets.Label(infoRect, GetHeaderText(tree));

        Rect outRect = new Rect(inRect.x, infoRect.yMax + 8f, inRect.width, inRect.height - infoRect.yMax - 8f);
        List<string> lines = BuildLines(tree);
        float viewHeight = Mathf.Max(outRect.height, lines.Count * 22f + 8f);
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        float y = 4f;
        foreach (string line in lines)
        {
            Widgets.Label(new Rect(4f, y, viewRect.width - 8f, 22f), line);
            y += 22f;
        }
        Widgets.EndScrollView();
    }

    private string GetHeaderText(PawnRenderTree? tree)
    {
        if (tree == null)
        {
            return "No special render tree.\n" + GetApparelText();
        }

        return $"Replica: {replica.LabelShortCap}\nHost: {replica.Host?.LabelShortCap ?? "null"} | Resolved: {tree.Resolved}\n{GetApparelText()}";
    }

    private string GetApparelText()
    {
        Pawn? host = replica.Host;
        if (host?.apparel == null)
        {
            return "Host apparel: null";
        }

        List<Apparel> apparels = host.apparel.WornApparel;
        if (apparels.NullOrEmpty())
        {
            return "Host apparel: empty";
        }

        List<string> parts = new List<string>();
        foreach (Apparel apparel in apparels)
        {
            bool graphicOk = ApparelGraphicRecordGetter.TryGetGraphicApparel(
                apparel,
                host.story?.bodyType,
                host.Drawer?.renderer?.StatueColor.HasValue == true,
                out _);
            parts.Add($"{apparel.def.defName}: graphic={graphicOk}");
        }

        return "Host apparel: " + string.Join(", ", parts);
    }

    private static List<string> BuildLines(PawnRenderTree? tree)
    {
        List<string> lines = new List<string>();
        if (tree?.rootNode == null)
        {
            lines.Add("rootNode: null");
            return lines;
        }

        AddNode(lines, tree.rootNode, 0);
        return lines;
    }

    private static void AddNode(List<string> lines, PawnRenderNode node, int depth)
    {
        if (node == null)
        {
            lines.Add(new string(' ', depth * 2) + "<null>");
            return;
        }

        lines.Add(new string(' ', depth * 2) + GetNodeText(node));
        if (node.children == null)
        {
            return;
        }

        foreach (PawnRenderNode child in node.children)
        {
            AddNode(lines, child, depth + 1);
        }
    }

    private static string GetNodeText(PawnRenderNode node)
    {
        PawnRenderNodeProperties? props = node.Props;
        string tag = props?.tagDef?.defName ?? "-";
        string label = props?.debugLabel ?? node.GetType().Name;
        string? apparel = node.apparel?.def?.defName;
        return apparel.NullOrEmpty()
            ? $"{node.GetType().Name} | {label} | tag={tag}"
            : $"{node.GetType().Name} | {label} | tag={tag} | apparel={apparel}";
    }

    private readonly FleshReplicaUnit replica;
    private Vector2 scrollPosition;
}
