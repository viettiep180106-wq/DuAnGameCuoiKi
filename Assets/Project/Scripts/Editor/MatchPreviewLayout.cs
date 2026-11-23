using System.Collections.Generic;
using UnityEngine;

internal enum MatchPreviewElement { Kim, Moc, Thuy, Hoa, Tho }
internal enum MatchPreviewShape { Horizontal, Vertical, Corner, Tee, Four, Five, Parallel }

internal sealed class MatchPreviewLayout
{
    internal const int BoardSize = 6;
    internal const float End = MatchStoneVfxProfile.Duration;

    private static readonly Vector2Int[] Axes = { Vector2Int.right, Vector2Int.up };

    internal readonly MatchPreviewElement[,] Board = new MatchPreviewElement[BoardSize, BoardSize];
    internal IReadOnlyList<MatchStoneVfxGraph.Node> Nodes => graph.Nodes;
    internal IReadOnlyList<MatchStoneVfxGraph.Link> Links => graph.Links;
    internal readonly HashSet<Vector2Int> Matched = new();

    private readonly MatchStoneVfxGraph graph = new();

    internal MatchPreviewLayout(MatchPreviewElement element, MatchPreviewShape shape)
    {
        for (int x = 0; x < BoardSize; x++)
        for (int y = 0; y < BoardSize; y++)
            Board[x, y] = (MatchPreviewElement)(((int)element + 1 + (x + 2*y) % 4) % 5);

        var (root, horizontal, vertical) = shape switch
        {
            MatchPreviewShape.Vertical => (new Vector2Int(2,1), 0, 3),
            MatchPreviewShape.Corner => (new Vector2Int(1,1), 3, 3),
            MatchPreviewShape.Tee => (new Vector2Int(2,2), 0, 3),
            MatchPreviewShape.Four => (new Vector2Int(1,2), 4, 0),
            MatchPreviewShape.Five => (new Vector2Int(0,2), 5, 0),
            _ => (new Vector2Int(1,2), 3, 0)
        };
        PaintRun(root, Vector2Int.right, horizontal, element);
        PaintRun(root, Vector2Int.up, vertical, element);
        if (shape == MatchPreviewShape.Tee)
            PaintRun(new Vector2Int(1,4), Vector2Int.right, 3, element);
        if (shape == MatchPreviewShape.Parallel)
            PaintRun(root+Vector2Int.up, Vector2Int.right, 3, element);
        CollectRuns();
        graph.Resolve(Matched, shape == MatchPreviewShape.Tee ? new Vector2Int(2,4) : root);
    }

    private void PaintRun(Vector2Int start, Vector2Int axis, int count, MatchPreviewElement element)
    {
        for (int i = 0; i < count; i++)
        {
            var cell = start + axis * i;
            Board[cell.x, cell.y] = element;
        }
    }

    private static bool OnBoard(Vector2Int p) => p.x >= 0 && p.x < BoardSize && p.y >= 0 && p.y < BoardSize;
    private MatchPreviewElement At(Vector2Int p) => Board[p.x, p.y];

    private void CollectRuns()
    {
        for (int x = 0; x < BoardSize; x++)
        for (int y = 0; y < BoardSize; y++)
        {
            var root = new Vector2Int(x,y);
            var element = At(root);
            foreach (var axis in Axes)
            {
                var previous = root - axis;
                if (OnBoard(previous) && At(previous) == element) continue;
                int count = 1;
                for (var next = root + axis; OnBoard(next) && At(next) == element; next += axis) count++;
                if (count < 3) continue;
                for (int i = 0; i < count; i++)
                    Matched.Add(root + i * axis);
                graph.AddRun(root, axis, count);
            }
        }
    }

}
