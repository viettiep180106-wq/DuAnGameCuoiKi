using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class MatchStoneVfx : MonoBehaviour
{
    private readonly struct Cell
    {
        internal readonly Vector2 Center;
        internal readonly float Size;
        internal readonly ElementType Element;

        internal Cell(Vector2 center, float size, ElementType element) =>
            (Center, Size, Element) = (center, size, element);
    }

    private readonly struct Part
    {
        internal readonly MatchStoneVfxTiming View;
        internal readonly int Pool;
        internal readonly float Expires;

        internal Part(MatchStoneVfxTiming view, int pool, float expires) =>
            (View, Pool, Expires) = (view, pool, expires);
    }

    private static readonly int Clock = Shader.PropertyToID("_MatchStoneClock");
    private const int ElementCount = 5;
    private readonly RawImage[] prefabs = new RawImage[ElementCount * 2];
    private readonly Stack<MatchStoneVfxTiming>[] available = new Stack<MatchStoneVfxTiming>[ElementCount * 2];
    private readonly Dictionary<Vector2Int, Cell> cells = new(36);
    private readonly List<Part> playing = new(96);
    private RectTransform linkLayer;
    private RectTransform nodeLayer;

    internal static MatchStoneVfx Create(RectTransform board)
    {
        if (!Application.isPlaying || !board || !board.GetComponentInParent<Canvas>()) return null;

        var root = new GameObject("Match Stone VFX", typeof(RectTransform), typeof(Canvas), typeof(MatchStoneVfx));
        root.layer = board.gameObject.layer;
        var rect = (RectTransform)root.transform;
        rect.SetParent(board, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1;

        var effect = root.GetComponent<MatchStoneVfx>();
        effect.linkLayer = CreateLayer(rect, "Links");
        effect.nodeLayer = CreateLayer(rect, "Nodes");
        return effect;
    }

    private static RectTransform CreateLayer(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var layer = (RectTransform)go.transform;
        layer.SetParent(parent, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;
        return layer;
    }

    internal bool TryPlay(MatchStoneVfxGraph graph, List<List<SlotStoneBase>> grid)
    {
        if (!isActiveAndEnabled || graph.Nodes.Count == 0) return false;

        // Snapshot all coordinates while the slots still own their original stones.
        cells.Clear();
        foreach (var node in graph.Nodes)
        {
            var cell = node.Cell;
            var slot = grid[cell.x][cell.y];
            var stone = slot.GetStone();
            var rect = slot.SlotItem;
            if (!stone || !rect) return false;
            var element = stone.GetElementType();
            if (!TryLoad(element, false) || !TryLoad(element, true)) return false;

            var center = (Vector2)transform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            var width = transform.InverseTransformVector(rect.TransformVector(Vector3.right * rect.rect.width)).magnitude;
            var height = transform.InverseTransformVector(rect.TransformVector(Vector3.up * rect.rect.height)).magnitude;
            var size = Mathf.Min(width, height);
            if (size <= 0f) return false;
            cells.Add(cell, new Cell(center, size, element));
        }

        transform.SetAsLastSibling();
        float now = Time.time;
        Shader.SetGlobalFloat(Clock, now);
        foreach (var link in graph.Links)
        {
            var from = cells[link.From];
            var to = cells[link.To];
            var delta = to.Center - from.Center;
            var size = new Vector2(delta.magnitude * MatchStoneVfxProfile.LinkLength, from.Size * MatchStoneVfxProfile.LinkWidth);
            float start = now + link.Delay + MatchStoneVfxProfile.LinkLag;
            Spawn(from.Element, true, (from.Center + to.Center) * .5f, size,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start, MatchStoneVfxProfile.Seed(link.From));
        }
        foreach (var node in graph.Nodes)
        {
            var cell = cells[node.Cell];
            Spawn(cell.Element, false, cell.Center, Vector2.one * (cell.Size * MatchStoneVfxProfile.NodeScale),
                0f, now + node.Delay, MatchStoneVfxProfile.Seed(node.Cell));
        }
        return true;
    }

    private bool TryLoad(ElementType element, bool link)
    {
        int pool = Index(element, link);
        if (prefabs[pool]) return true;
        var path = MatchStoneVfxProfile.ResourceFolder + "/" + MatchStoneVfxProfile.PrefabName(element, link);
        var prefab = Resources.Load<GameObject>(path);
        if (!prefab || !prefab.TryGetComponent(out RawImage image) ||
            !image.material.shader.isSupported || !image.material.HasProperty("_Kind")) return false;
        prefabs[pool] = image;
        return true;
    }

    private void Spawn(ElementType element, bool link, Vector2 center, Vector2 size, float angle, float start, float seed)
    {
        int pool = Index(element, link);
        var free = available[pool] ??= new Stack<MatchStoneVfxTiming>();
        var view = free.Count > 0 ? free.Pop() : null;
        if (!view)
        {
            var image = Instantiate(prefabs[pool], link ? linkLayer : nodeLayer, false);
            image.gameObject.layer = gameObject.layer;
            view = image.gameObject.AddComponent<MatchStoneVfxTiming>();
        }

        var rect = (RectTransform)view.transform;
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        view.SetTiming(start, seed, link ? MatchStoneVfxProfile.LinkStrength : MatchStoneVfxProfile.NodeStrength);
        view.gameObject.SetActive(true);
        playing.Add(new Part(view, pool, start + (link ? MatchStoneVfxProfile.LinkDuration : MatchStoneVfxProfile.Duration)));
    }

    private static int Index(ElementType element, bool link) => (int)element * 2 + (link ? 1 : 0);

    private void LateUpdate()
    {
        if (playing.Count == 0) return;
        float now = Time.time;
        Shader.SetGlobalFloat(Clock, now);
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            if (now < playing[i].Expires) continue;
            Release(playing[i]);
            playing.RemoveAt(i);
        }
    }

    internal void Clear()
    {
        foreach (var part in playing) Release(part);
        playing.Clear();
        cells.Clear();
    }

    private void Release(Part part)
    {
        if (!part.View) return;
        part.View.gameObject.SetActive(false);
        available[part.Pool].Push(part.View);
    }

    private void OnDisable() => Clear();
}
