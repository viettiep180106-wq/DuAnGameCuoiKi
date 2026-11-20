using System;
using UnityEditor;
using UnityEngine;

internal sealed class FireBombSizePreview : EditorWindow
{
    // A representative flight within SkillFire's 0.08–0.22 second range.
    private const float FlightDuration = 0.18f;
    private const float OrbStart = FlightDuration * 0.28f;
    private const float BurstStart = 0.065f;
    private const float SmokeStart = 0.22f;
    private const float End = FlightDuration + 0.72f;

    [SerializeField] private float relativeScale = 1f;
    [SerializeField] private float zoom = 1f;
    [SerializeField] private float time = FlightDuration + 0.12f;
    private double playbackStart;
    private bool playing;
    private PreviewView currentView;
    private PreviewView proposedView;
    private SkillFire projectile;
    private FireBombBurst burst;
    private Sprite ember;
    private Sprite orb;
    private Sprite character;

    [MenuItem("Tools/FX/Fire Bomb Size Preview")]
    private static void Open() => GetWindow<FireBombSizePreview>("Fire Bomb Size Preview").Show();

    private void OnEnable()
    {
        minSize = new Vector2(640f, 320f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Resources/Prefab/Base/Skill/Passive/fire.prefab");
        if (prefab && prefab.TryGetComponent(out projectile))
        {
            var data = new SerializedObject(projectile);
            ember = (Sprite)data.FindProperty("emberSprite").objectReferenceValue;
            orb = (Sprite)data.FindProperty("fireballSprite").objectReferenceValue;
            burst = (FireBombBurst)data.FindProperty("burstPrefab").objectReferenceValue;
        }
        character = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Player.png");
    }

    private void OnDisable()
    {
        currentView?.Dispose();
        proposedView?.Dispose();
        currentView = null;
        proposedView = null;
    }

    private void OnGUI()
    {
        if (!projectile || !ember || !orb || !burst)
        {
            EditorGUILayout.HelpBox("The Fire Bomb prefab is missing its VFX references.", MessageType.Error);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playing ? "Pause" : "Play"))
            {
                playing = !playing;
                if (playing)
                    playbackStart = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("Ember")) SelectFrame(0f);
            if (GUILayout.Button("Fireball")) SelectFrame(0.12f);
            if (GUILayout.Button("Flash")) SelectFrame(FlightDuration + 0.03f);
            if (GUILayout.Button("Burst")) SelectFrame(FlightDuration + 0.12f);
            if (GUILayout.Button("Smoke")) SelectFrame(FlightDuration + 0.33f);
        }
        EditorGUI.BeginChangeCheck();
        float selectedTime = EditorGUILayout.Slider("Time (seconds)", time, 0f, End);
        if (EditorGUI.EndChangeCheck())
            SelectFrame(selectedTime);

        relativeScale = EditorGUILayout.Slider("Scale relative to current", relativeScale, 0.5f, 2f);
        zoom = EditorGUILayout.Slider("Camera zoom", zoom, 0.75f, 2.5f);
        EditorGUILayout.LabelField($"Damage radius: {projectile.splashRadius:0.##} units in both views");
        Rect previewArea = GUILayoutUtility.GetRect(0f, 10000f, 160f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint)
            return;

        currentView ??= new PreviewView(burst, projectile.GetComponentInChildren<SpriteRenderer>(), character);
        proposedView ??= new PreviewView(burst, projectile.GetComponentInChildren<SpriteRenderer>(), character);
        const float gap = 4f;
        float width = (previewArea.width - gap) * 0.5f;
        DrawView(currentView, new Rect(previewArea.x, previewArea.y, width, previewArea.height), 1f, "Current");
        DrawView(proposedView, new Rect(previewArea.x + width + gap, previewArea.y, width, previewArea.height),
            relativeScale, "Proposed");
    }

    private void DrawView(PreviewView view, Rect rect, float multiplier, string label)
    {
        GUI.Box(new Rect(rect.x, rect.y, rect.width, 22f), $"{label}  {multiplier:0.0#}×", EditorStyles.toolbar);
        rect.yMin += 22f;
        if (rect.width <= 1f || rect.height <= 1f)
            return;

        Sprite flightSprite = time < OrbStart ? ember : orb;
        Texture frame = view.Render(rect, time, flightSprite, projectile.splashRadius, multiplier, zoom);
        if (frame)
            GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, false);
    }

    private void SelectFrame(float selectedTime)
    {
        time = selectedTime;
        playing = false;
    }

    private void Update()
    {
        if (!playing)
            return;

        double elapsed = EditorApplication.timeSinceStartup - playbackStart;
        time = Mathf.Min((float)elapsed, End);
        if (elapsed > End + 0.2f)
            playing = false;
        Repaint();
    }

    private sealed class PreviewView : IDisposable
    {
        private static readonly Vector3 LaunchPosition = new(-8f, 0f, 0f);
        private readonly PreviewRenderUtility preview = new();
        private readonly SpriteRenderer flightRenderer;
        private readonly SpriteRenderer burstRenderer;
        private readonly Sprite flash;
        private readonly Sprite fire;
        private readonly Sprite smoke;
        private readonly float burstScale;
        private readonly ParticleSystem[] particles;

        public PreviewView(FireBombBurst burstPrefab, SpriteRenderer sourceRenderer, Sprite character)
        {
            Camera camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(34, 39, 51, 255);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(-2f, 0f, -35f);
            camera.allowHDR = false;

            var reference = CreateRenderer("Character reference");
            reference.sprite = character;
            reference.transform.localScale = Vector3.one * 5f;
            flightRenderer = CreateRenderer("Fireball");
            flightRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            flightRenderer.sortingOrder = sourceRenderer.sortingOrder;

            GameObject burstObject = UnityEngine.Object.Instantiate(burstPrefab.gameObject);
            burstObject.hideFlags = HideFlags.HideAndDontSave;
            preview.AddSingleGO(burstObject);
            burstObject.GetComponent<FireBombBurst>().enabled = false;
            burstRenderer = burstObject.GetComponent<SpriteRenderer>();
            var burstData = new SerializedObject(burstPrefab);
            flash = (Sprite)burstData.FindProperty("flashSprite").objectReferenceValue;
            fire = (Sprite)burstData.FindProperty("burstSprite").objectReferenceValue;
            smoke = (Sprite)burstData.FindProperty("smokeSprite").objectReferenceValue;
            burstScale = burstData.FindProperty("visualScale").floatValue;
            particles = burstObject.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles[i].useAutoRandomSeed = false;
                particles[i].randomSeed = (uint)(17 + i);
            }
        }

        private SpriteRenderer CreateRenderer(string name)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
            return root.AddComponent<SpriteRenderer>();
        }

        public Texture Render(Rect rect, float time, Sprite flightSprite, float radius, float multiplier, float zoom)
        {
            bool flying = time < FlightDuration;
            float progress = Mathf.Clamp01(time / FlightDuration);
            flightRenderer.enabled = flying;
            flightRenderer.sprite = flightSprite;
            flightRenderer.transform.localScale = Vector3.one * (5.8f * multiplier);
            flightRenderer.transform.position = Vector3.Lerp(LaunchPosition, Vector3.zero, progress * progress);

            float impactTime = time - FlightDuration;
            burstRenderer.enabled = !flying && time < End;
            burstRenderer.transform.position = Vector3.zero;
            burstRenderer.transform.localScale = Vector3.one * (Mathf.Max(0.1f, radius) / 1.7f * burstScale * multiplier);
            burstRenderer.sprite = impactTime < BurstStart ? flash : impactTime < SmokeStart ? fire : smoke;
            burstRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(SmokeStart, 0.57f, impactTime));
            foreach (var system in particles)
            {
                if (impactTime > BurstStart && time < End)
                    system.Simulate(impactTime - BurstStart, withChildren: false, restart: true, fixedTimeStep: false);
                else
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            preview.camera.orthographicSize = 10.4f * Mathf.Max(1f, rect.height / rect.width) / zoom;
            preview.BeginPreview(rect, GUIStyle.none);
            try
            {
                preview.Render(allowScriptableRenderPipeline: true, updatefov: false);
            }
            catch
            {
                preview.EndPreview();
                throw;
            }
            return preview.EndPreview();
        }

        public void Dispose() => preview.Cleanup();
    }
}
