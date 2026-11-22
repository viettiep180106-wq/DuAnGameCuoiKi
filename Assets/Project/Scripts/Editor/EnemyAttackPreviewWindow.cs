using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

internal abstract class EnemyAttackPreviewWindow : EditorWindow
{
    private const float FrameInterval = 1f / 30f;
    private static readonly string[] Modes = { "Đánh thường", "Active" };
    private static readonly string[] Speeds = { "0,25×", "0,5×", "1×" };
    private static readonly float[] SpeedValues = { 0.25f, 0.5f, 1f };

    [SerializeField] private int enemy;
    [SerializeField] private int mode = 1;
    [SerializeField] private int speed = 1;
    private int scenario;
    private int round;
    private float time;
    private float roundAge = 1f;
    private bool playing;
    private double lastUpdate;
    private double nextFrame;
    private double roundStarted;
    private bool needsFrame;
    private int revision;
    private int requestedRevision;
    private string caption;
    private string error;
    private Texture2D preview;
    private EnemyPreviewRenderer renderer;
    private Task<PreviewMetadata> pendingStartup;
    private Task<PreviewFrame> pendingFrame;
    private PreviewMetadata metadata;
    private Attack Current => metadata.attacks[enemy * 2 + mode];

    protected abstract string PreviewFolder { get; }
    protected abstract string BridgeName { get; }
    protected abstract string ElementLabel { get; }

    protected void OnEnable()
    {
        minSize = new Vector2(600f, 360f);
        StartRenderer();
    }

    protected void OnDisable()
    {
        renderer?.Dispose();
        renderer = null;
        pendingFrame = null;
        pendingStartup = null;
        metadata = null;
        playing = false;
        if (preview)
            DestroyImmediate(preview);
    }

    private void StartRenderer()
    {
        renderer?.Dispose();
        error = null;
        pendingFrame = null;
        renderer = new EnemyPreviewRenderer(Directory.GetParent(Application.dataPath).FullName, PreviewFolder, BridgeName);
        pendingStartup = renderer.StartAsync<PreviewMetadata>();
    }

    protected void OnGUI()
    {
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
            if (GUILayout.Button("Thử lại"))
                StartRenderer();
            return;
        }
        if (metadata == null)
        {
            EditorGUILayout.HelpBox($"Đang tải sprite và bộ dựng hình hệ {ElementLabel}…", MessageType.Info);
            return;
        }

        int selectedEnemy = EditorGUILayout.Popup("Enemy", enemy, metadata.enemies);
        int selectedMode = GUILayout.Toolbar(mode, Modes);
        if (selectedEnemy != enemy || selectedMode != mode)
        {
            enemy = selectedEnemy;
            mode = selectedMode;
            scenario = 0;
            SelectRound(0);
        }
        EditorGUILayout.LabelField(Current.name, EditorStyles.boldLabel);
        DrawControls();
        EditorGUILayout.HelpBox(Current.description, MessageType.None);
        if (!string.IsNullOrEmpty(caption))
            EditorGUILayout.LabelField(caption);

        Rect area = GUILayoutUtility.GetRect(0f, 10000f, 100f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type == EventType.Repaint && preview)
        {
            EditorGUI.DrawRect(area, new Color(0.04f, 0.08f, 0.12f));
            GUI.DrawTexture(area, preview, ScaleMode.ScaleToFit, false);
        }
    }

    private void DrawControls()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playing ? "Dừng" : "Phát", GUILayout.Width(74)))
            {
                if (playing)
                    playing = false;
                else
                    Play(round > 0 || time >= Current.end);
            }
            if (GUILayout.Button("↺", GUILayout.Width(31))) Play(true);
            if (GUILayout.Button("‹", GUILayout.Width(30))) Seek(time - FrameInterval);
            if (GUILayout.Button("›", GUILayout.Width(30))) Seek(time + FrameInterval);
            foreach (var marker in Current.markers)
                if (GUILayout.Button(marker.label)) Seek(marker.time);
            speed = EditorGUILayout.Popup(speed, Speeds, GUILayout.Width(64));
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Thời gian (giây)", time, 0f, Current.end);
        if (EditorGUI.EndChangeCheck()) Seek(scrubbed);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (Current.scenarioLabels.Length > 1)
            {
                int choice = EditorGUILayout.Popup("Tình huống", scenario, Current.scenarioLabels);
                if (choice != scenario)
                {
                    scenario = choice;
                    InvalidateFrame();
                }
            }
            if (Current.roundCount > 0)
            {
                int choice = EditorGUILayout.Popup("Mốc theo lượt", round, Current.roundLabels);
                if (choice != round) SelectRound(choice);
            }
        }
    }

    private void SelectRound(int selection)
    {
        round = selection;
        Seek(round == 0 ? Current.hit + 0.075f : Current.end);
        roundAge = round == 0 ? 1f : 0f;
        if (round > 0) roundStarted = EditorApplication.timeSinceStartup;
    }

    private void Seek(float value)
    {
        time = Mathf.Clamp(value, 0f, Current.end);
        playing = false;
        roundAge = 1f;
        InvalidateFrame();
    }

    private void Play(bool restart)
    {
        if (restart)
        {
            round = 0;
            Seek(0f);
        }
        playing = true;
        lastUpdate = EditorApplication.timeSinceStartup;
    }

    private void InvalidateFrame()
    {
        revision++;
        needsFrame = true;
        nextFrame = 0;
    }

    protected void Update()
    {
        try
        {
            if (pendingStartup?.IsCompleted == true)
            {
                var startup = pendingStartup;
                pendingStartup = null;
                metadata = startup.GetAwaiter().GetResult();
                foreach (var attack in metadata.attacks) attack.PrepareOptions();
                SelectRound(0);
                Repaint();
            }
            if (metadata == null || error != null) return;

            if (pendingFrame?.IsCompleted == true)
            {
                var request = pendingFrame;
                pendingFrame = null;
                var frame = request.GetAwaiter().GetResult();
                if (requestedRevision == revision)
                {
                    if (!preview)
                        preview = new Texture2D(2, 2, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
                    if (!preview.LoadImage(Convert.FromBase64String(frame.image)))
                        throw new InvalidDataException("Không đọc được ảnh preview hệ Thủy.");
                    caption = frame.caption;
                    Repaint();
                }
            }

            double now = EditorApplication.timeSinceStartup;
            if (pendingFrame != null || now < nextFrame) return;
            if (playing)
            {
                time = Mathf.Min(Current.end, time + (float)(now - lastUpdate) * SpeedValues[speed]);
                lastUpdate = now;
                playing = time < Current.end;
                needsFrame = true;
            }
            if (roundAge < 1f)
            {
                roundAge = Mathf.Clamp01((float)(now - roundStarted));
                needsFrame = true;
            }
            if (needsFrame)
            {
                needsFrame = false;
                nextFrame = now + FrameInterval;
                requestedRevision = revision;
                string scenarioId = Current.scenarios[scenario].id;
                string call = FormattableString.Invariant($"window.{BridgeName}.frame({enemy},{mode},{time:F4},'{scenarioId}',{round},{roundAge:F4})");
                pendingFrame = renderer.EvaluateAsync<PreviewFrame>(call);
            }
        }
        catch (Exception exception)
        {
            renderer?.Dispose();
            renderer = null;
            metadata = null;
            error = exception.Message;
            playing = false;
            Repaint();
        }
    }

    [Serializable]
    private sealed class PreviewMetadata
    {
        public string[] enemies = Array.Empty<string>();
        public Attack[] attacks = Array.Empty<Attack>();
    }

    [Serializable]
    private sealed class Attack
    {
        public string name = "";
        public float hit = 0f;
        public float end = 0f;
        public string description = "";
        public int roundCount = 0;
        public Option[] scenarios = Array.Empty<Option>();
        public Marker[] markers = Array.Empty<Marker>();
        public string[] scenarioLabels;
        public string[] roundLabels;

        public void PrepareOptions()
        {
            scenarioLabels = Array.ConvertAll(scenarios, option => option.label);
            roundLabels = new string[roundCount + 1];
            roundLabels[0] = "Lúc áp dụng";
            for (int i = 1; i <= roundCount; i++) roundLabels[i] = $"Sau round {i}";
        }
    }

    [Serializable]
    private sealed class Option
    {
        public string id = "";
        public string label = "";
    }

    [Serializable]
    private sealed class Marker
    {
        public string label = "";
        public float time = 0f;
    }

    [Serializable]
    private sealed class PreviewFrame
    {
        public string image = "";
        public string caption = "";
    }
}
