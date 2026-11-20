using System;
using UnityEditor;
using UnityEngine;

internal sealed class IceRainPreview : EditorWindow
{
    private const float CloudLead = IceRainStrikeVisual.CloudLead;
    private const float FallDuration = IceRainStrikeVisual.FallDuration;
    private const float ImpactDuration = IceRainStrikeVisual.ImpactDuration;
    private const float MainSceneCameraSize = 49.11628f;
    private static readonly int[] Counts = { 1, 3, 5, 8 };
    private static readonly string[] CountLabels = { "1 bolt", "3 bolts", "5 bolts", "8 bolts" };
    private static readonly string[] CameraLabels = { "MainScene camera scale", "Close-up" };
    private static readonly int[] TargetOrder = { 1, 0, 2, 1, 2, 0, 1, 1 };
    private static readonly Vector3[] TargetPositions = { new(-13f, -7f), new(0f, -5f), new(13f, -8f) };
    private static readonly Vector3 CastPosition = new(-26f, -5f);

    [SerializeField] private int selectedCount;
    [SerializeField] private float time = CloudLead + FallDuration + 0.11f;
    [SerializeField] private float boltLength = 17f;
    [SerializeField] private float impactWidth = 11f;
    [SerializeField] private float shardSpread = 1f;
    [SerializeField] private bool closeUp;
    private PreviewView view;
    private IceRainStrikeVisual strikePrefab;
    private float boltDelay;
    private bool playing;
    private double startedAt;

    private float TimelineEnd => (Counts[selectedCount] - 1) * boltDelay + CloudLead + FallDuration + ImpactDuration;

    [MenuItem("Tools/FX/Ice Rain Preview")]
    private static void Open()
    {
        GetWindow<IceRainPreview>("Ice Rain Preview").Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(640f, 390f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/Ice.prefab");
        var skill = prefab ? prefab.GetComponent<SkillIce>() : null;
        boltDelay = skill ? skill.boltDelay : 0.08f;
        if (skill)
            strikePrefab = (IceRainStrikeVisual)new SerializedObject(skill).FindProperty("strikePrefab").objectReferenceValue;
    }

    private void OnDisable()
    {
        view?.Dispose();
        view = null;
    }

    private void OnGUI()
    {
        if (!strikePrefab)
        {
            EditorGUILayout.HelpBox("The Ice skill prefab is missing its strike VFX.", MessageType.Error);
            return;
        }

        int count = GUILayout.Toolbar(selectedCount, CountLabels);
        if (count != selectedCount)
        {
            selectedCount = count;
            SelectTime(Mathf.Min(time, TimelineEnd));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playing ? "Pause" : "Play"))
            {
                if (playing)
                    playing = false;
                else
                {
                    time = 0f;
                    startedAt = EditorApplication.timeSinceStartup;
                    playing = true;
                }
            }
            if (GUILayout.Button("Cloud")) SelectTime(CloudLead * 0.60f);
            if (GUILayout.Button("Fall")) SelectTime(CloudLead + 0.15f);
            if (GUILayout.Button("Impact")) SelectTime(CloudLead + FallDuration + 0.038f);
            if (GUILayout.Button("Shatter")) SelectTime(CloudLead + FallDuration + 0.16f);
            if (GUILayout.Button("Mist")) SelectTime(CloudLead + FallDuration + 0.44f);
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Time (seconds)", time, 0f, TimelineEnd);
        if (EditorGUI.EndChangeCheck())
            SelectTime(scrubbed);
        boltLength = EditorGUILayout.Slider("Bolt length (units)", boltLength, 6f, 26f);
        impactWidth = EditorGUILayout.Slider("Impact width (units)", impactWidth, 5f, 21f);
        shardSpread = EditorGUILayout.Slider("Shard spread", shardSpread, 0.35f, 2f);
        closeUp = GUILayout.Toolbar(closeUp ? 1 : 0, CameraLabels) == 1;

        Rect area = GUILayoutUtility.GetRect(0f, 10000f, 175f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 2f || area.height < 2f)
            return;

        view ??= new PreviewView(strikePrefab);
        Texture frame = view.Render(area, time, Counts[selectedCount], boltDelay,
            boltLength, impactWidth, shardSpread, closeUp);
        if (frame)
            GUI.DrawTexture(area, frame, ScaleMode.StretchToFill, false);
    }

    private void SelectTime(float selectedTime)
    {
        time = selectedTime;
        playing = false;
    }

    private void Update()
    {
        if (!playing)
            return;

        float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
        time = Mathf.Min(elapsed, TimelineEnd);
        playing = elapsed < TimelineEnd;
        Repaint();
    }

    private sealed class PreviewView : IDisposable
    {
        private readonly PreviewRenderUtility preview = new();
        private readonly IceRainStrikeVisual[] bolts = new IceRainStrikeVisual[TargetOrder.Length];

        public PreviewView(IceRainStrikeVisual prefab)
        {
            var camera = preview.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(33, 41, 55, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;

            var player = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Player.png");
            PlaceReference(player, CastPosition, 5f, false);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Project/Resources/Prefab/Base/Enemy/Fire/Enemy_TinhLinhLua.prefab");
            var enemyRenderer = enemyPrefab ? enemyPrefab.GetComponentInChildren<SpriteRenderer>() : null;
            Sprite enemySprite = enemyRenderer && enemyRenderer.sprite ? enemyRenderer.sprite : player;
            foreach (Vector3 target in TargetPositions)
                PlaceReference(enemySprite, target, 5.7f, true);

            for (int i = 0; i < bolts.Length; i++)
            {
                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance.gameObject);
                instance.transform.position = TargetPositions[TargetOrder[i]];
                bolts[i] = instance;
            }
        }

        private GameObject CreateObject(string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
            return root;
        }

        public SpriteRenderer Sprite(string name, Sprite art, int order)
        {
            var renderer = CreateObject(name).AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void PlaceReference(Sprite sprite, Vector3 center, float height, bool flip)
        {
            if (!sprite)
                return;
            var renderer = Sprite("Combat scale reference", sprite, 0);
            float width = height * sprite.bounds.size.x / sprite.bounds.size.y;
            Place(renderer, center, width, height, 1f);
            renderer.flipX = flip;
        }

        public Texture Render(Rect area, float time, int count, float boltDelay, float boltLength,
            float impactWidth, float shardSpread, bool closeUp)
        {
            for (int i = 0; i < bolts.Length; i++)
            {
                float age = i < count ? time - i * boltDelay : -1f;
                bolts[i].RenderPreview(age, boltLength, impactWidth, shardSpread);
            }

            preview.camera.transform.position = new Vector3(-6f, 4f, -40f);
            preview.camera.orthographicSize = closeUp
                ? 19f * Mathf.Max(1f, area.height / area.width)
                : MainSceneCameraSize;
            preview.BeginPreview(area, GUIStyle.none);
            Texture frame;
            try
            {
                preview.Render(allowScriptableRenderPipeline: true, updatefov: false);
            }
            finally
            {
                frame = preview.EndPreview();
            }
            return frame;
        }

        private static void Place(SpriteRenderer renderer, Vector3 center, float width, float height, float alpha)
        {
            renderer.enabled = alpha > 0.001f;
            if (!renderer.enabled)
                return;

            Vector3 size = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
            renderer.transform.position = center;
            renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        public void Dispose() => preview.Cleanup();
    }
}
