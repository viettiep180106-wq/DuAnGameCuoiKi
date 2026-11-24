using UnityEditor;
using UnityEngine;

internal sealed class EnemyStatusPreview : EditorWindow, EnemyStatusAnimation.ICanvas
{
    private const float Intro = EnemyStatusAnimation.Intro;
    private const float Outro = EnemyStatusAnimation.Outro;
    private static readonly string[] StatusNames = { "Thiêu đốt", "Đóng băng", "Làm chậm", "Choáng", "Giảm giáp" };
    private static readonly float[] Durations = { 3f, 2f, 2.5f, 1.5f, 2.4f };
    private static readonly string[] EnemyNames = { "Kỵ Sĩ Sắt", "Slime Nước", "Thủy Thần Sa Ngã" };
    private static readonly string[] EnemyPaths = { "Metal/Enemy_KySiSat", "Water/Enemy_SlimeNuoc", "Water/Enemy_ThuyThanSaNga" };
    private static readonly float[] EnemyScales = { 1f, .68f, 1.13f };

    private readonly Vector3[] orbitLine = new Vector3[41];
    private readonly Sprite[] enemySprites = new Sprite[EnemyPaths.Length];
    private readonly bool[] enemyFlipX = new bool[EnemyPaths.Length];
    private Texture2D atlas;
    private Material burnMaterial;
    private RenderTexture burnFrame;
    [SerializeField] private int status;
    [SerializeField] private int enemy;
    [SerializeField] private bool loop = true;
    [SerializeField] private float time = .65f;
    private bool playing;
    private double lastUpdate;

    private float EndTime => Durations[status] + Outro;

    [MenuItem("Tools/FX/Enemy Status Preview")]
    private static void Open() => GetWindow<EnemyStatusPreview>("Enemy Status Preview").Show();

    private void OnEnable()
    {
        minSize = new Vector2(560f, 390f);
        atlas = Resources.Load<Texture2D>(EnemyStatusAnimation.AtlasResource);
        var burnShader = Resources.Load<Shader>(EnemyStatusAnimation.BurnShaderResource);
        if (burnShader && burnShader.isSupported)
        {
            burnMaterial = new Material(burnShader) { hideFlags = HideFlags.HideAndDontSave };
            burnMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            burnMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        }
        for (int i = 0; i < EnemyPaths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Project/Resources/Prefab/Base/Enemy/{EnemyPaths[i]}.prefab");
            var renderer = prefab ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            enemySprites[i] = renderer ? renderer.sprite : null;
            enemyFlipX[i] = renderer && renderer.flipX;
        }
    }

    private void OnDisable()
    {
        if (burnFrame)
        {
            burnFrame.Release();
            DestroyImmediate(burnFrame);
        }
        if (burnMaterial) DestroyImmediate(burnMaterial);
    }

    private void OnGUI()
    {
        int selection = GUILayout.Toolbar(status, StatusNames);
        if (selection != status)
        {
            status = selection;
            Seek(0f);
            Play();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            enemy = EditorGUILayout.Popup("Enemy mẫu", enemy, EnemyNames);
            if (GUILayout.Button(playing ? "Tạm dừng" : "Phát", GUILayout.Width(82f)))
            {
                if (playing) playing = false;
                else Play();
            }
            loop = GUILayout.Toggle(loop, "Lặp", GUILayout.Width(48f));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Kích hoạt")) Seek(Intro * .55f);
            if (GUILayout.Button("Duy trì")) Seek(Durations[status] * .52f);
            if (GUILayout.Button("Tan")) Seek(Durations[status] + Outro * .38f);
        }

        float selectedTime = EditorGUILayout.Slider("Thời gian (giây)", time, 0f, EndTime);
        if (!Mathf.Approximately(selectedTime, time)) Seek(selectedTime);
        if (status == 2) EditorGUILayout.LabelField("Skill Thủy hiện chưa áp được trạng thái làm chậm.", EditorStyles.miniLabel);
        if (status == 4) EditorGUILayout.LabelField("Pha tan minh họa lúc enemy rời sân.", EditorStyles.miniLabel);

        if (!atlas || !enemySprites[enemy])
        {
            EditorGUILayout.HelpBox("Thiếu atlas hoặc sprite enemy mẫu.", MessageType.Error);
            return;
        }

        Rect area = GUILayoutUtility.GetRect(1f, 10000f, 200f, 10000f, GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 2f || area.height < 2f) return;

        GUI.BeginGroup(area);
        DrawPreview(area.size);
        GUI.EndGroup();
    }

    private void Seek(float value)
    {
        time = value;
        playing = false;
    }

    private void Play()
    {
        if (time >= EndTime) time = 0f;
        lastUpdate = EditorApplication.timeSinceStartup;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        double now = EditorApplication.timeSinceStartup;
        time += (float)(now - lastUpdate);
        lastUpdate = now;
        if (time >= EndTime)
        {
            time = loop ? Mathf.Repeat(time, EndTime) : EndTime;
            playing = loop;
        }
        Repaint();
    }

    private void DrawPreview(Vector2 size)
    {
        EditorGUI.DrawRect(new Rect(Vector2.zero, size), new Color32(22, 30, 43, 255));
        float floor = size.y * .78f;
        EditorGUI.DrawRect(new Rect(0f, floor, size.x, size.y - floor), new Color32(15, 22, 32, 255));
        EditorGUI.DrawRect(new Rect(0f, floor, size.x, 1f), new Color32(39, 51, 66, 255));

        Vector2 feet = new(size.x * .5f, floor);
        float unit = Mathf.Min(size.y * .52f, 250f) * EnemyScales[enemy];
        Vector2 head = feet - Vector2.up * (unit * 1.12f);
        float age = time - Durations[status];
        float frost = status == 1 ? EnemyStatusAnimation.Hold(time, age) : 0f;
        DrawActor(enemySprites[enemy], enemyFlipX[enemy], feet, unit, frost);
        EnemyStatusAnimation.Draw(this, (EnemyStatusAnimation.Kind)status, feet, head, unit, time, age,
            Mathf.Exp(-Mathf.Repeat(time, 1f) * 15f));
    }

    private static void DrawActor(Sprite sprite, bool flipX, Vector2 feet, float height, float frost)
    {
        Rect source = sprite.rect;
        Texture2D texture = sprite.texture;
        float width = height * source.width / source.height;
        Rect target = new(feet.x - width * .5f, feet.y - height, width, height);
        Rect uv = new(source.x / texture.width, source.y / texture.height,
            source.width / texture.width, source.height / texture.height);
        if (flipX)
        {
            uv.x += uv.width;
            uv.width = -uv.width;
        }
        Color tint = Color.Lerp(Color.white, new Color(.54f, .84f, 1f), frost * .65f);
        DrawTexture(texture, uv, target, tint, 0f);
    }

    public void DrawFlames(Vector2 feet, float unit, Vector4 motion)
    {
        if (!burnMaterial)
        {
            Draw(EnemyStatusAnimation.BurnFallback, feet, unit * .44f, motion.z, bottom: true);
            return;
        }

        if (!burnFrame)
            burnFrame = new RenderTexture(288, 256, 0, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

        burnMaterial.SetVector("_Motion", motion);
        var previousTarget = RenderTexture.active;
        Graphics.Blit(null, burnFrame, burnMaterial);
        RenderTexture.active = previousTarget;

        float width = unit * .68f;
        float height = unit * .58f;
        GUI.DrawTexture(new Rect(feet.x - width * .5f, feet.y - height * .94f, width, height), burnFrame);
    }

    public void DrawOrbit(Vector2 center, float unit, float tilt, float opacity)
    {
        for (int i = 0; i < orbitLine.Length; i++)
            orbitLine[i] = EnemyStatusAnimation.OrbitPoint(center, unit, i * 2f * Mathf.PI / (orbitLine.Length - 1), tilt);
        Handles.BeginGUI();
        Color handlesColor = Handles.color;
        Handles.color = new Color(1f, .72f, .20f, opacity);
        Handles.DrawAAPolyLine(2f, orbitLine);
        Handles.color = handlesColor;
        Handles.EndGUI();
    }

    public void Draw(Rect source, Vector2 anchor, float width, float alpha, float angle = 0f, bool bottom = false, float heightScale = 1f)
    {
        if (alpha <= .001f) return;
        float height = width * (source.height / source.width) * heightScale;
        Rect target = new(anchor.x - width * .5f, anchor.y - height * (bottom ? 1f : .5f), width, height);
        Rect uv = EnemyStatusAnimation.AtlasUv(source);
        DrawTexture(atlas, uv, target, new Color(1f, 1f, 1f, alpha), angle);
    }

    private static void DrawTexture(Texture2D texture, Rect uv, Rect target, Color tint, float angle)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color color = GUI.color;
        if (angle != 0f) GUIUtility.RotateAroundPivot(angle, target.center);
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(target, texture, uv, true);
        GUI.color = color;
        GUI.matrix = matrix;
    }
}
