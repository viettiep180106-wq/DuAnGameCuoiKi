using System;
using UnityEditor;
using UnityEngine;

internal sealed class EarthSlamPreview : EditorWindow
{
    private const float FallDuration = EarthSlamVisual.FallDuration;
    private const float PreviewDuration = FallDuration + EarthSlamVisual.ImpactDuration;
    private const float MatchCameraSize = 49.11628f;
    private static readonly string[] Rows = { "Front row", "Back row only" };
    private static readonly string[] Stages = { "Fall", "Impact", "Burst", "Fracture", "Residue" };
    private static readonly string[] Cameras = { "MainScene camera scale", "Close-up" };
    private static readonly float[] Samples = { 0.33f, 0.455f, 0.61f, 0.85f, 1.18f };
    private static readonly Vector3[] Front = { new(2.5f, 0f), new(2.5f, 5f) };
    private static readonly Vector3[] Back = { new(7.5f, 7.5f), new(7.5f, 2.5f), new(7.5f, -2.5f) };

    [SerializeField] private int row;
    [SerializeField] private float time = 0.62f;
    [SerializeField] private float boulderSize = 12f;
    [SerializeField] private float shockwaveWidth = 17f;
    [SerializeField] private float particleSpread = 1f;
    [SerializeField] private bool closeUp;
    private bool playing;
    private double startedAt;
    private float fallHeight;
    private EarthSlamVisual visualPrefab;
    private PreviewView view;

    [MenuItem("Tools/FX/Earth Slam Preview")]
    private static void Open() => GetWindow<EarthSlamPreview>("Earth Slam Preview").Show();

    private void OnEnable()
    {
        minSize = new Vector2(640f, 390f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/earth.prefab");
        var skill = prefab ? prefab.GetComponent<SkillEarth>() : null;
        fallHeight = skill ? skill.slamHeight : 22f;
        if (skill)
            visualPrefab = (EarthSlamVisual)new SerializedObject(skill).FindProperty("visualPrefab").objectReferenceValue;
        if (visualPrefab)
        {
            var settings = new SerializedObject(visualPrefab);
            boulderSize = settings.FindProperty("boulderSize").floatValue;
            shockwaveWidth = settings.FindProperty("impactWidth").floatValue;
            particleSpread = settings.FindProperty("particleSpread").floatValue;
        }
    }

    private void OnDisable()
    {
        view?.Dispose();
        view = null;
    }

    private void OnGUI()
    {
        if (!visualPrefab)
        {
            EditorGUILayout.HelpBox("The Earth Slam visual prefab is missing.", MessageType.Error);
            return;
        }

        row = GUILayout.Toolbar(row, Rows);
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
            for (int i = 0; i < Stages.Length; i++)
                if (GUILayout.Button(Stages[i]))
                    SelectTime(Samples[i]);
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Time (seconds)", time, 0f, PreviewDuration);
        if (EditorGUI.EndChangeCheck())
            SelectTime(scrubbed);
        boulderSize = EditorGUILayout.Slider("Meteor size (units)", boulderSize, 5f, 20f);
        shockwaveWidth = EditorGUILayout.Slider("Impact width (units)", shockwaveWidth, 8f, 28f);
        particleSpread = EditorGUILayout.Slider("Dust and rock spread", particleSpread, 0.35f, 2f);
        closeUp = GUILayout.Toolbar(closeUp ? 1 : 0, Cameras) == 1;

        Rect area = GUILayoutUtility.GetRect(0f, 10000f, 175f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 2f || area.height < 2f)
            return;

        view ??= new PreviewView(visualPrefab);
        var frame = view.Render(area, time, row == 1, fallHeight, boulderSize, shockwaveWidth, particleSpread, closeUp);
        if (frame)
            GUI.DrawTexture(area, frame, ScaleMode.StretchToFill, false);
    }

    private void SelectTime(float value)
    {
        time = value;
        playing = false;
    }

    private void Update()
    {
        if (!playing)
            return;

        float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
        time = Mathf.Min(elapsed, PreviewDuration);
        playing = elapsed < PreviewDuration;
        Repaint();
    }

    private sealed class PreviewView : IDisposable
    {
        private readonly PreviewRenderUtility preview = new();
        private readonly EarthSlamVisual visual;
        private readonly SpriteRenderer[] frontEnemies;

        public PreviewView(EarthSlamVisual prefab)
        {
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(37, 39, 49, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 90f;
            camera.allowHDR = false;
            camera.transform.position = new Vector3(4f, 6f, -40f);

            var fallback = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Player.png");
            frontEnemies = BuildRow(Front, LoadEnemy("Enemy_BoCapDa", fallback), 22);
            BuildRow(Back, LoadEnemy("Enemy_LinhDat", fallback), 20);
            visual = UnityEngine.Object.Instantiate(prefab);
            visual.gameObject.hideFlags = HideFlags.HideAndDontSave;
            preview.AddSingleGO(visual.gameObject);
        }

        private static Sprite LoadEnemy(string name, Sprite fallback)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Project/Resources/Prefab/Base/Enemy/Earth/{name}.prefab");
            var renderer = prefab ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
            return renderer && renderer.sprite ? renderer.sprite : fallback;
        }

        private SpriteRenderer[] BuildRow(Vector3[] positions, Sprite sprite, int order)
        {
            var result = new SpriteRenderer[positions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = CreateSprite("Enemy reference", sprite, order);
                Place(result[i], positions[i], 5.6f * sprite.rect.width / sprite.rect.height, 5.6f, 1f);
                result[i].flipX = true;
            }
            return result;
        }

        private GameObject CreateObject(string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
            return root;
        }

        private SpriteRenderer CreateSprite(string name, Sprite sprite, int order)
        {
            var renderer = CreateObject(name).AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        public Texture Render(Rect area, float time, bool backOnly, float height, float rockSize,
            float width, float spread, bool closeUp)
        {
            foreach (var enemy in frontEnemies)
                enemy.enabled = !backOnly;
            Vector3[] targets = backOnly ? Back : Front;
            Vector3 center = Vector3.zero;
            foreach (var target in targets)
                center += target;
            center /= targets.Length;

            visual.transform.position = center;
            visual.RenderPreview(time, targets, height, rockSize, width, spread);

            preview.camera.orthographicSize = closeUp ? 20f * Mathf.Max(1f, area.height / area.width) : MatchCameraSize;
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

        private static void Place(SpriteRenderer sprite, Vector3 position, float width, float height, float alpha)
        {
            sprite.enabled = alpha > 0.001f;
            if (!sprite.enabled)
                return;
            Vector3 size = sprite.sprite.bounds.size;
            sprite.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
            sprite.transform.position = position;
            sprite.color = new Color(1f, 1f, 1f, alpha);
        }

        public void Dispose() => preview.Cleanup();
    }
}
