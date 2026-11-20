using System;
using UnityEditor;
using UnityEngine;

internal sealed class WoodArmorPreview : EditorWindow
{
    private const float MainSceneCameraSize = 49.11628f;
    private static readonly string[] Modes = { "Activate", "Heal on hit", "Emergency heal" };
    private static readonly string[] Phases = { "Sprout", "Rise", "Pulse", "Ring", "Fade" };
    private static readonly string[] CameraModes = { "MainScene camera scale", "Close-up" };
    private static readonly float[] FullSamples = { 0.16f, 0.36f, 0.63f, 0.90f, 1.20f };
    private static readonly float[] HealSamples = { 0f, 0f, 0.12f, 0.27f, 0.52f };

    [SerializeField] private int mode;
    [SerializeField] private float time = 0.63f;
    [SerializeField] private float scale = 1f;
    [SerializeField] private bool closeUp;
    private bool playing;
    private double startedAt;
    private PreviewView view;
    private WoodArmorVisual visualPrefab;

    private float Duration => WoodArmorVisual.Duration((WoodArmorVisual.Kind)mode);

    [MenuItem("Tools/FX/Wood Armor Preview")]
    private static void Open() => GetWindow<WoodArmorPreview>("Wood Armor Preview").Show();

    private void OnEnable()
    {
        minSize = new Vector2(590f, 350f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/VFX/WoodArmorVisual.prefab");
        visualPrefab = prefab ? prefab.GetComponent<WoodArmorVisual>() : null;
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
            EditorGUILayout.HelpBox("The Wood Armor visual prefab is missing.", MessageType.Error);
            return;
        }

        int chosenMode = GUILayout.Toolbar(mode, Modes);
        if (chosenMode != mode)
        {
            mode = chosenMode;
            SelectTime(mode == 1 ? HealSamples[2] : FullSamples[2]);
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
            for (int phase = 0; phase < Phases.Length; phase++)
            {
                using (new EditorGUI.DisabledScope(mode == 1 && phase < 2))
                {
                    if (GUILayout.Button(Phases[phase]))
                        SelectTime((mode == 1 ? HealSamples : FullSamples)[phase]);
                }
            }
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Time (seconds)", time, 0f, Duration);
        if (EditorGUI.EndChangeCheck())
            SelectTime(scrubbed);
        scale = EditorGUILayout.Slider("VFX scale", scale, 0.6f, 2f);
        closeUp = GUILayout.Toolbar(closeUp ? 1 : 0, CameraModes) == 1;

        Rect area = GUILayoutUtility.GetRect(0f, 10000f, 165f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 2f || area.height < 2f)
            return;

        view ??= new PreviewView(visualPrefab);
        Texture frame = view.Render(area, mode, time, scale, closeUp);
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
        time = Mathf.Min(elapsed, Duration);
        playing = elapsed < Duration;
        Repaint();
    }

    private sealed class PreviewView : IDisposable
    {
        private readonly PreviewRenderUtility preview = new();
        private readonly WoodArmorVisual visual;

        public PreviewView(WoodArmorVisual prefab)
        {
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(33, 41, 55, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 85f;
            camera.allowHDR = false;
            camera.transform.position = new Vector3(0f, 0.7f, -35f);

            var player = Sprite("Player reference", AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Project/Sprite/Player.png"), 20);
            if (player.sprite)
                player.transform.localScale = Vector3.one * 5f;

            visual = UnityEngine.Object.Instantiate(prefab);
            visual.gameObject.hideFlags = HideFlags.HideAndDontSave;
            preview.AddSingleGO(visual.gameObject);
        }

        private GameObject CreateObject(string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
            return root;
        }

        private SpriteRenderer Sprite(string name, Sprite art, int order)
        {
            var renderer = CreateObject(name).AddComponent<SpriteRenderer>();
            renderer.sprite = art;
            renderer.sortingOrder = order;
            return renderer;
        }

        public Texture Render(Rect area, int mode, float time, float scale, bool closeUp)
        {
            visual.RenderPreview((WoodArmorVisual.Kind)mode, time, visual.VisualScale * scale);
            preview.camera.orthographicSize = closeUp ? 9.0f * Mathf.Max(1f, area.height / area.width) : MainSceneCameraSize;
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

        public void Dispose() => preview.Cleanup();
    }
}
