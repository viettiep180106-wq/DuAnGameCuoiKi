using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

internal sealed class MatchStonePreviewWindow : EditorWindow
{
    private static readonly string[] Elements = { "Kim", "Mộc", "Thủy", "Hỏa", "Thổ" };
    private static readonly string[] Shapes = { "Ngang 3", "Dọc 3", "Chữ L", "Chữ T", "Ngang 4", "Ngang 5", "Hai hàng song song" };
    private static readonly string[] Speeds = { "0,25×", "0,5×", "1×" };
    private static readonly float[] SpeedValues = { .25f, .5f, 1f };
    private static readonly string[] Notes =
    {
        "Vòng vàng bắt sáng, tia điện nối các ô, rồi tách thành mảnh kim loại và bụi sáng.",
        "Vòng dây leo ôm viên đá, hai nhánh đan qua các ô và bung thành cánh hoa, lá.",
        "Viền băng lục giác đóng lại, dòng lạnh luồn qua các ô, tinh thể vỡ thành mảnh nhỏ.",
        "Lửa cuộn quanh đá, chạy dọc dải nối, rồi bung tia nóng, tàn lửa và khói.",
        "Đất rạn dưới từng viên, gờ đá lan theo dải nối, sạn nảy lên rồi rơi xuống."
    };

    [SerializeField] private int element;
    [SerializeField] private int shape = (int)MatchPreviewShape.Tee;
    [SerializeField] private int speed = 1;
    private MatchPreviewCanvas canvas;
    private MatchPreviewLayout layout;
    private float time = .192f;
    private bool playing;
    private double lastUpdate;
    private bool dirty = true;
    private string error;

    [MenuItem("Tools/FX/Match Stone VFX Preview")]
    public static void Open()
    {
        var window = GetWindow<MatchStonePreviewWindow>("Match Stone VFX");
        window.minSize = new Vector2(595f,465f);
        window.Show();
    }

    private void OnEnable() => ResetPreview();

    private void OnDisable() => canvas?.Dispose();

    private void ResetPreview()
    {
        canvas?.Dispose();
        canvas = null;
        error = null;
        try { canvas = new MatchPreviewCanvas(); }
        catch (Exception ex) { error = ex.Message; }
        RebuildLayout();
    }

    private void RebuildLayout()
    {
        layout = new MatchPreviewLayout((MatchPreviewElement)element, (MatchPreviewShape)shape);
        dirty = true;
    }

    private void OnGUI()
    {
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Info);
            if (GUILayout.Button("Tạo asset preview"))
            {
                MatchPreviewAssets.Build();
                ResetPreview();
            }
            return;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawElement();
            DrawShape();
            EditorGUILayout.LabelField($"{layout.Nodes.Count} ô · {layout.Links.Count} Link", GUILayout.Width(98));
        }
        DrawTimeControls();
        EditorGUILayout.LabelField(Notes[element], EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4);

        var rect = GUILayoutUtility.GetRect(100f, 10000f, 170f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint) return;
        EditorGUI.DrawRect(rect,new Color(.040f,.059f,.084f));
        if (dirty)
        {
            canvas.Render(layout, time);
            dirty = false;
        }
        GUI.DrawTexture(rect, canvas.Texture, ScaleMode.ScaleToFit, false);
    }

    private void DrawElement()
    {
        int updated = EditorGUILayout.Popup("Nguyên tố",element,Elements);
        if (updated == element) return;
        element = updated;
        RebuildLayout();
    }

    private void DrawShape()
    {
        int updated = EditorGUILayout.Popup("Kiểu match",shape,Shapes);
        if (updated == shape) return;
        shape = updated;
        RebuildLayout();
    }

    private void DrawTimeControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playing?"Dừng":"Phát",GUILayout.Width(59)))
            {
                if (playing) playing = false;
                else
                {
                    if (time >= MatchPreviewLayout.End) Seek(0f);
                    playing = true;
                    lastUpdate = EditorApplication.timeSinceStartup;
                }
            }
            if (GUILayout.Button("↺",GUILayout.Width(29))) { Seek(0f); playing = true; lastUpdate = EditorApplication.timeSinceStartup; }
            if (GUILayout.Button("‹",GUILayout.Width(29))) Seek(time-1f/60f);
            if (GUILayout.Button("›",GUILayout.Width(29))) Seek(time+1f/60f);
            speed=EditorGUILayout.Popup(speed,Speeds,GUILayout.Width(65));
            if (GUILayout.Button("Tụ lực")) Seek(.061f);
            if (GUILayout.Button("Nối ô")) Seek(.162f);
            if (GUILayout.Button("Bung hạt")) Seek(.224f);
            if (GUILayout.Button("Dư âm")) Seek(.365f);
        }
        EditorGUI.BeginChangeCheck();
        float position = EditorGUILayout.Slider("Thời gian (giây)",time,0f,MatchPreviewLayout.End);
        if (EditorGUI.EndChangeCheck()) Seek(position);
    }

    private void Seek(float value)
    {
        time=Mathf.Clamp(value,0f,MatchPreviewLayout.End);
        playing=false;
        dirty=true;
        Repaint();
    }

    private void Update()
    {
        if (!playing) return;
        double now=EditorApplication.timeSinceStartup;
        time=Mathf.Min(MatchPreviewLayout.End,time+Mathf.Min(.05f,(float)(now-lastUpdate))*SpeedValues[speed]);
        lastUpdate=now;
        playing=time<MatchPreviewLayout.End;
        dirty=true;
        Repaint();
    }
}

internal sealed class MatchPreviewCanvas : IDisposable
{
    internal const int Width = 680;
    internal const int Height = 624;
    private const float Cell = 89f;

    private static readonly string[] StonePaths = { "StoneLight", "StoneWood", "StoneIce", "StoneFire", "StoneEarth" };
    private static readonly int Kind = Shader.PropertyToID("_Kind");
    private static readonly int Age = Shader.PropertyToID("_Age");
    private static readonly int Seed = Shader.PropertyToID("_Seed");
    private static readonly int Strength = Shader.PropertyToID("_Strength");
    private static readonly int SpriteRect = Shader.PropertyToID("_SpriteRect");
    private readonly Material[] nodes = new Material[5];
    private readonly Material[] links = new Material[5];
    private readonly Sprite[] stones = new Sprite[5];
    private Material backdrop;
    private Mesh quad;
    internal RenderTexture Texture { get; private set; }

    internal MatchPreviewCanvas()
    {
        try
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(MatchPreviewAssets.ShaderPath);
            if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Unity chưa nạp được shader preview match đá.");
            for (int i = 0; i < nodes.Length; i++)
            {
                var element = (MatchPreviewElement)i;
                nodes[i] = Clone(element, false);
                links[i] = Clone(element, true);
                var stone = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Project/Resources/Prefab/Base/Stone/{StonePaths[i]}.prefab");
                stones[i] = stone ? stone.GetComponent<UnityEngine.UI.Image>()?.sprite : null;
                if (!stones[i]) throw new InvalidOperationException($"Thiếu sprite đá {StonePaths[i]}.");
            }
            backdrop = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            quad = new Mesh { name = "Match Preview Quad", hideFlags = HideFlags.HideAndDontSave };
            quad.vertices = new Vector3[] { new(-.5f,-.5f), new(.5f,-.5f), new(.5f,.5f), new(-.5f,.5f) };
            quad.uv = new Vector2[] { new(0f,0f), new(1f,0f), new(1f,1f), new(0f,1f) };
            quad.triangles = new[] { 0,2,1,0,3,2 };
            Texture = new RenderTexture(Width,Height,0,RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
            Texture.Create();
        }
        catch { Dispose(); throw; }
    }

    private static Material Clone(MatchPreviewElement element, bool link)
    {
        var template = MatchPreviewAssets.PrefabMaterial(element,link);
        if (!template) throw new InvalidOperationException("Cần tạo prefab Node và Link cho preview.");
        return new Material(template) { hideFlags = HideFlags.HideAndDontSave };
    }

    private static Vector2 Center(Vector2Int cell) => new(Width*.5f+(cell.x-2.5f)*Cell,Height*.5f+(cell.y-2.5f)*Cell);
    private static float Jitter(Vector2Int cell) => MatchStoneVfxProfile.Seed(cell);

    internal void Render(MatchPreviewLayout layout,float time)
    {
        var before = RenderTexture.active;
        bool srgb = GL.sRGBWrite;
        GL.PushMatrix();
        try
        {
            RenderTexture.active=Texture;
            GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
            GL.LoadPixelMatrix(0,Width,0,Height);
            GL.Clear(false,true,new Color(.018f,.029f,.046f,1f));
            DrawBoard(layout);
            DrawStones(layout,time);
            foreach (var link in layout.Links) DrawLink(layout,link,time);
            foreach (var node in layout.Nodes) DrawNode(layout,node,time);
        }
        finally
        {
            GL.PopMatrix();
            GL.sRGBWrite=srgb;
            RenderTexture.active=before;
        }
    }

    private void DrawBoard(MatchPreviewLayout layout)
    {
        backdrop.SetFloat(Kind,4f);
        Draw(backdrop,new Vector2(Width*.5f,Height*.5f),new Vector2(Cell*6+59,Cell*6+59));
        backdrop.SetFloat(Kind,2f);
        for (int x=0;x<MatchPreviewLayout.BoardSize;x++)
        for (int y=0;y<MatchPreviewLayout.BoardSize;y++)
        {
            var cell=new Vector2Int(x,y);
            backdrop.SetFloat(Seed,Jitter(cell)*.40f);
            Draw(backdrop,Center(cell),Vector2.one*(Cell-5f));
        }
    }

    private void DrawStones(MatchPreviewLayout layout, float time)
    {
        backdrop.SetFloat(Kind,3f);
        float backgroundAlpha = Mathf.SmoothStep(.84f,.51f,Mathf.InverseLerp(0f,.070f,time));
        for (int x=0;x<MatchPreviewLayout.BoardSize;x++)
        for (int y=0;y<MatchPreviewLayout.BoardSize;y++)
        {
            var cell=new Vector2Int(x,y);
            if (layout.Matched.Contains(cell)) continue;
            DrawStone(layout.Board[x,y],Center(cell),Cell*.76f,backgroundAlpha);
        }
        foreach (var node in layout.Nodes)
        {
            float age=time-node.Delay;
            float fade=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.140f,.221f,age));
            float charge=Mathf.Sin(Mathf.PI*Mathf.InverseLerp(0f,.147f,age));
            DrawStone(layout.Board[node.Cell.x,node.Cell.y],Center(node.Cell),Cell*(.76f+charge*.035f)*Mathf.Lerp(.66f,1f,fade),fade);
        }
    }

    private void DrawStone(MatchPreviewElement element,Vector2 center,float size,float alpha)
    {
        if (alpha<.006f) return;
        Sprite sprite=stones[(int)element];
        var rect=sprite.textureRect;
        var tex=sprite.texture;
        backdrop.mainTexture=tex;
        backdrop.SetVector(SpriteRect,new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));
        backdrop.SetFloat(Strength,alpha);
        Draw(backdrop,center,rect.size*(size/Mathf.Max(rect.width,rect.height)));
    }

    private void DrawLink(MatchPreviewLayout layout, MatchStoneVfxGraph.Link link,float time)
    {
        var a=Center(link.From);
        var b=Center(link.To);
        var delta=b-a;
        var material=links[(int)layout.Board[link.From.x,link.From.y]];
        material.SetFloat(Age,time-link.Delay-MatchStoneVfxProfile.LinkLag);
        material.SetFloat(Seed,Jitter(link.From));
        material.SetFloat(Strength,MatchStoneVfxProfile.LinkStrength);
        Draw(material,(a+b)*.5f,new Vector2(delta.magnitude*MatchStoneVfxProfile.LinkLength,Cell*MatchStoneVfxProfile.LinkWidth),
            Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
    }

    private void DrawNode(MatchPreviewLayout layout, MatchStoneVfxGraph.Node node,float time)
    {
        var cell=node.Cell;
        var material=nodes[(int)layout.Board[cell.x,cell.y]];
        material.SetFloat(Age,time-node.Delay);
        material.SetFloat(Seed,Jitter(cell));
        material.SetFloat(Strength,MatchStoneVfxProfile.NodeStrength);
        Draw(material,Center(cell),Vector2.one*(Cell*MatchStoneVfxProfile.NodeScale));
    }

    private void Draw(Material material,Vector2 center,Vector2 size,float angle=0f)
    {
        if (!material.SetPass(0)) return;
        Graphics.DrawMeshNow(quad,Matrix4x4.TRS(center,Quaternion.Euler(0f,0f,angle),new Vector3(size.x,size.y,1f)));
    }

    public void Dispose()
    {
        foreach (var item in nodes) if (item) Object.DestroyImmediate(item);
        foreach (var item in links) if (item) Object.DestroyImmediate(item);
        if (backdrop) Object.DestroyImmediate(backdrop);
        if (quad) Object.DestroyImmediate(quad);
        if (Texture)
        {
            Texture.Release();
            Object.DestroyImmediate(Texture);
        }
    }
}
