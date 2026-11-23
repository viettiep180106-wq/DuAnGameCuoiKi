using System.Collections.Generic;
using UnityEngine;

// The board supplies confirmed runs. Overlapping triples share nodes and links.
public sealed class MatchStoneVfxGraph
{
    public readonly struct Node
    {
        public readonly Vector2Int Cell;
        public readonly float Delay;

        internal Node(Vector2Int cell, float delay) => (Cell, Delay) = (cell, delay);
    }

    public readonly struct Link
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;
        public readonly float Delay;

        internal Link(Vector2Int from, Vector2Int to, float delay) => (From, To, Delay) = (from, to, delay);
    }

    private readonly HashSet<(Vector2Int from, Vector2Int to)> edges = new();
    private readonly Dictionary<Vector2Int, int> depths = new();
    private readonly Queue<Vector2Int> frontier = new();
    private readonly List<Vector2Int> order = new(36);
    private readonly List<Node> nodes = new(36);
    private readonly List<Link> links = new(60);

    public IReadOnlyList<Node> Nodes => nodes;
    public IReadOnlyList<Link> Links => links;

    public void Clear()
    {
        edges.Clear();
        nodes.Clear();
        links.Clear();
    }

    public void AddRun(Vector2Int start, Vector2Int step, int count)
    {
        for (int i = 1; i < count; i++)
        {
            var next = start + step;
            edges.Add((start, next));
            start = next;
        }
    }

    public void Resolve(HashSet<Vector2Int> matched, Vector2Int? origin)
    {
        depths.Clear();
        nodes.Clear();
        links.Clear();
        order.Clear();
        order.AddRange(matched);
        order.Sort(CompareCells);

        if (origin.HasValue && matched.Contains(origin.Value))
            Walk(origin.Value);
        foreach (var cell in order)
        {
            Walk(cell);
            nodes.Add(new Node(cell, Delay(depths[cell])));
        }

        foreach (var (a,b) in edges)
        {
            bool reverse = depths[a] > depths[b];
            links.Add(new Link(reverse ? b : a, reverse ? a : b, Delay(Mathf.Min(depths[a], depths[b]))));
        }
    }

    private void Walk(Vector2Int start)
    {
        if (!depths.TryAdd(start, 0)) return;
        frontier.Enqueue(start);
        while (frontier.TryDequeue(out var cell))
        {
            int depth = depths[cell] + 1;
            foreach (var (a,b) in edges)
            {
                if (a != cell && b != cell) continue;
                var neighbor = cell == a ? b : a;
                if (depths.TryAdd(neighbor, depth))
                    frontier.Enqueue(neighbor);
            }
        }
    }

    private static int CompareCells(Vector2Int a, Vector2Int b)
    {
        int row = a.y.CompareTo(b.y);
        return row == 0 ? a.x.CompareTo(b.x) : row;
    }

    private static float Delay(int depth) => Mathf.Min(depth, 4) * .014f;
}

public static class MatchStoneVfxProfile
{
    public const string ResourceFolder = "VFX/MatchStone";
    public const float Duration = .55f;
    public const float LinkDuration = .39f;
    public const float LinkLag = .056f;
    public const float NodeScale = 2.08f;
    public const float LinkLength = 1.10f;
    public const float LinkWidth = .67f;
    public const float NodeStrength = .96f;
    public const float LinkStrength = .95f;

    private static readonly string[] Names = { "Kim", "Moc", "Thuy", "Hoa", "Tho" };

    public static string PrefabName(ElementType element, bool link) =>
        "VFX_" + (link ? "Link_" : "Node_") + Names[(int)element];

    public static float Seed(Vector2Int cell) => Mathf.Repeat(cell.x * .534f + cell.y * .367f + .19f, 1f);
}
