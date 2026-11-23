using UnityEngine;
using UnityEngine.UI;

// Each quad carries its own clock and seed while sharing its prefab material.
internal sealed class MatchStoneVfxTiming : BaseMeshEffect
{
    private Vector4 timing;

    internal void SetTiming(float start, float seed, float strength)
    {
        timing = new Vector4(start, seed, strength, 2f);
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive()) return;
        var vertex = new UIVertex();
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            vertex.uv1 = timing;
            vertices.SetUIVertex(vertex, i);
        }
    }
}
