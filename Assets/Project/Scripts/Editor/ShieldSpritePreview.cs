using System;
using UnityEditor;
using UnityEngine;

internal sealed class ShieldSpritePreview : EditorWindow
{
    private const string SpriteFolder = "Assets/Project/Sprite/Shield/";
    private const string MaterialPath = "Assets/Project/Shaders/ShieldSprite.mat";
    private static readonly string[] SpriteNames =
    {
        "Shield_01_Cast", "Shield_02_Idle", "Shield_03_Charge", "Shield_04_Impact", "Shield_05_Shatter"
    };
    private static readonly string[] StateNames = { "Cast", "Idle", "Charge", "Impact", "Shatter" };
    private static readonly float[] DefaultProgress = { 0.64f, 0f, 0.22f, 0.21f, 0.32f };
    private static readonly float[] Durations = { 0.48f, 2.5f, 0.37f, 0.36f, 0.68f };
    private static readonly int StateId = Shader.PropertyToID("_State");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int ElapsedId = Shader.PropertyToID("_Elapsed");

    private static readonly Step[] CastSequence = { new(0), new(2), new(1) };
    private static readonly Step[] HitSequence = { new(3), new(1) };
    private static readonly Step[] BreakSequence = { new(3, 0.57f), new(4) };

    private readonly Sprite[] sprites = new Sprite[SpriteNames.Length];
    private PreviewView darkView;
    private PreviewView brightView;
    private Material material;
    private Sprite character;
    private bool showCharacter = true;
    private int state = 1;
    private float progress;
    private float elapsed;
    private Step[] sequence;
    private double startedAt;
    private float sequenceInitialElapsed;

    [MenuItem("Tools/FX/Shield Sprite Preview")]
    private static void Open()
    {
        GetWindow<ShieldSpritePreview>("Shield Sprite Preview").Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(485, 280);
        material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        character = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Player.png");
        for (int i = 0; i < sprites.Length; i++)
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + SpriteNames[i] + ".png");
    }

    private void OnDisable()
    {
        darkView?.Dispose();
        brightView?.Dispose();
        darkView = null;
        brightView = null;
    }

    private void OnGUI()
    {
        if (!material || Array.Exists(sprites, sprite => !sprite))
        {
            EditorGUILayout.HelpBox("The shield material or one of the five sprites is missing.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cast → Charge → Idle"))
                Play(CastSequence);
            if (GUILayout.Button("Impact → Idle"))
                Play(HitSequence);
            if (GUILayout.Button("Impact → Shatter"))
                Play(BreakSequence);
            if (GUILayout.Button("Pause", GUILayout.Width(60)))
                sequence = null;
        }

        int chosen = GUILayout.Toolbar(state, StateNames);
        if (chosen != state)
        {
            sequence = null;
            state = chosen;
            progress = DefaultProgress[state];
            elapsed = progress * Durations[state];
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Progress", progress, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            sequence = null;
            progress = scrubbed;
            elapsed = progress * Durations[state];
        }

        showCharacter = EditorGUILayout.ToggleLeft("Show character", showCharacter);

        Rect previews = GUILayoutUtility.GetRect(0, 10000, 120, 10000,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint)
            return;

        darkView ??= new PreviewView(new Color32(34, 39, 51, 255));
        brightView ??= new PreviewView(new Color32(196, 186, 167, 255));
        float width = (previews.width - 4f) * 0.5f;
        DrawView(darkView, new Rect(previews.x, previews.y, width, previews.height), "Dark background");
        DrawView(brightView, new Rect(previews.x + width + 4f, previews.y, width, previews.height), "Light background");
    }

    private void DrawView(PreviewView view, Rect rect, string label)
    {
        GUI.Box(new Rect(rect.x, rect.y, rect.width, 22f), label, EditorStyles.toolbar);
        rect.yMin += 22f;
        view.Draw(rect, material, sprites[state], showCharacter ? character : null, state, progress, elapsed);
    }

    private void Play(Step[] steps)
    {
        sequence = steps;
        sequenceInitialElapsed = elapsed;
        startedAt = EditorApplication.timeSinceStartup;
        Update();
    }

    private void Update()
    {
        if (sequence == null)
            return;

        float time = (float)(EditorApplication.timeSinceStartup - startedAt);
        elapsed = sequenceInitialElapsed + time;
        for (int i = 0; i < sequence.Length; i++)
        {
            Step step = sequence[i];
            state = step.State;
            float duration = Durations[state] * step.EndProgress;
            if (time < duration || (state == 1 && i == sequence.Length - 1))
            {
                progress = state == 1 ? Mathf.Repeat(time, duration) / duration : time / Durations[state];
                Repaint();
                return;
            }
            time -= duration;
        }

        progress = 1f;
        sequence = null;
        Repaint();
    }

    private readonly struct Step
    {
        public readonly int State;
        public readonly float EndProgress;

        public Step(int state, float endProgress = 1f)
        {
            State = state;
            EndProgress = endProgress;
        }
    }

    internal sealed class PreviewView : IDisposable
    {
        private readonly PreviewRenderUtility preview = new();
        private readonly MaterialPropertyBlock properties = new();
        private readonly SpriteRenderer shield;
        private readonly SpriteRenderer character;

        public PreviewView(Color background)
        {
            Camera camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 25f;
            camera.transform.position = new Vector3(0, 0, -10f);
            camera.allowHDR = false;
            character = CreateRenderer("Shield preview character", 0);
            shield = CreateRenderer("Shield preview effect", 1);
        }

        private SpriteRenderer CreateRenderer(string objectName, int sortingOrder)
        {
            var root = new GameObject(objectName) { hideFlags = HideFlags.HideAndDontSave };
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            preview.AddSingleGO(root);
            return renderer;
        }

        internal Texture Render(Rect rect, Material material, Sprite sprite, Sprite subject,
            int state, float progress, float elapsed)
        {
            shield.sprite = sprite;
            shield.sharedMaterial = material;
            properties.SetFloat(StateId, state);
            properties.SetFloat(ProgressId, progress);
            properties.SetFloat(ElapsedId, elapsed);
            shield.SetPropertyBlock(properties);

            character.sprite = subject;
            if (subject)
            {
                float scale = 1.84f / subject.bounds.size.y;
                character.transform.localScale = Vector3.one * scale;
                character.transform.localPosition = -subject.bounds.center * scale;
            }

            preview.camera.orthographicSize = 2.04f * Mathf.Max(1f, rect.height / rect.width);
            preview.BeginPreview(rect, GUIStyle.none);
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

        public void Draw(Rect rect, Material material, Sprite sprite, Sprite subject,
            int state, float progress, float elapsed)
        {
            if (rect.width > 1f && rect.height > 1f)
            {
                Texture frame = Render(rect, material, sprite, subject, state, progress, elapsed);
                if (frame)
                    GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, false);
            }
        }

        public void Dispose() => preview.Cleanup();
    }
}
