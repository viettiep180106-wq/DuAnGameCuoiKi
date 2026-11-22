using System;
using UnityEditor;
using UnityEngine;

internal sealed class EnemyFireAttackPreview : EditorWindow
{
    private const string ClipPath = "Assets/Project/Animation/Enemies/Fire/";
    private const float MatchCameraSize = 49.11628f;
    private const int ExistingEnemyCount = 4;
    private static readonly string[] Enemies =
    {
        "Tinh Linh Lửa", "Chó Săn Địa Ngục", "Hỏa Quỷ Nhỏ", "Phù Thủy Lửa",
        "Chiến Binh Dung Nham", "Hỏa Long Nhỏ", "Hỏa Thần Diệt Vong"
    };
    private static readonly string[] EnemyIds =
    {
        "TinhLinhLua", "ChoSanDiaNguc", "HoaQuyNho", "PhuThuyLua",
        "ChienBinhDungNham", "HoaLongNho", "HoaThanDietVong"
    };
    private static readonly string[] Modes = { "Đánh thường", "Active" };
    private static readonly string[] ActiveNames =
    {
        "Tia Lửa Đỏ", "Cắn Xé Rực Lửa", "Cầu Lửa Hắc Ám", "Cột Lửa Thiêu Rụi",
        "Bổ Búa Dung Nham", "Hơi Thở Rồng Lửa", "Mưa Sao Băng Tối Thượng"
    };
    private static readonly string[] CameraModes = { "MainScene camera scale", "Close-up" };
    private static readonly Timing[,] Timeline =
    {
        { new(0.10f, 0.33f, 0.62f), new(0.30f, 0.70f, 1.06f) },
        { new(0.28f, 0.28f, 0.88f), new(0.28f, 0.48f, 1.08f) },
        { new(0.10f, 0.30f, 0.60f), new(0.18f, 0.63f, 1.20f) },
        { new(0.12f, 0.42f, 0.87f), new(0.30f, 0.30f, 1.47f) },
        { new(0.13f, 0.33f, 1.01f), new(0.14f, 0.34f, 2.28f) },
        { new(0.14f, 0.43f, 1.35f), new(0.22f, 0.44f, 2.24f) },
        { new(0.19f, 0.34f, 0.85f), new(0.20f, 0.64f, 3.52f) }
    };

    [SerializeField] private int enemyIndex;
    [SerializeField] private int mode;
    [SerializeField] private float time = 0.45f;
    [SerializeField] private float vfxScale = 1f;
    [SerializeField] private bool closeUp = true;
    [SerializeField] private bool slow;
    private bool playing;
    private double startedAt;
    private Artwork artwork;
    private PreviewView view;

    private Timing Current => Timeline[enemyIndex, mode];
    private float HitPreviewTime => Current.Hit + 0.045f
        + (mode == 0 && (enemyIndex == 0 || enemyIndex == 2) ? EnemyAttackVfxVisuals.NormalImpactDelay : 0f);
    private float AttackSelectionTime => enemyIndex == 2 && mode == 1
        ? Mathf.Lerp(Current.Release, Current.Hit, 0.62f)
        : HitPreviewTime;

    [MenuItem("Tools/FX/Enemy Fire Attack Preview")]
    private static void Open() => GetWindow<EnemyFireAttackPreview>("Enemy Fire Attack Preview").Show();

    private void OnEnable()
    {
        minSize = new Vector2(640f, 380f);
        artwork = new Artwork();
    }

    private void OnDisable()
    {
        view?.Dispose();
        view = null;
    }

    private void OnGUI()
    {
        int enemy = EditorGUILayout.Popup("Enemy", enemyIndex, Enemies);
        int selectedMode = GUILayout.Toolbar(mode, Modes);
        if (enemy != enemyIndex || selectedMode != mode)
        {
            enemyIndex = enemy;
            mode = selectedMode;
            SelectTime(AttackSelectionTime);
        }
        if (!artwork.IsReady(enemyIndex))
        {
            EditorGUILayout.HelpBox("The fire enemy preview is missing sprite artwork or its shader.", MessageType.Error);
            return;
        }
        if (mode == 1)
            EditorGUILayout.LabelField(ActiveNames[enemyIndex], EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playing ? "Pause" : "Play"))
            {
                playing = !playing;
                if (playing)
                {
                    time = 0f;
                    startedAt = EditorApplication.timeSinceStartup;
                }
            }
            if (GUILayout.Button("Wind-up"))
                SelectTime(Current.Release * 0.57f);
            if (GUILayout.Button("Release"))
                SelectTime(Current.Release + Mathf.Min(0.055f, (Current.End - Current.Release) * 0.15f));
            if (GUILayout.Button("Hit"))
                SelectTime(HitPreviewTime);
            if (GUILayout.Button("Aftermath"))
                SelectTime(Mathf.Lerp(Current.Hit, Current.End, 0.66f));
            bool halfSpeed = GUILayout.Toggle(slow, "0.5×", EditorStyles.miniButton, GUILayout.Width(42f));
            if (playing && halfSpeed != slow)
                startedAt = EditorApplication.timeSinceStartup - time / (halfSpeed ? 0.5f : 1f);
            slow = halfSpeed;
        }

        EditorGUI.BeginChangeCheck();
        float scrubbed = EditorGUILayout.Slider("Time (seconds)", time, 0f, Current.End);
        if (EditorGUI.EndChangeCheck())
            SelectTime(scrubbed);
        vfxScale = EditorGUILayout.Slider("VFX scale", vfxScale, 0.6f, 2f);
        closeUp = GUILayout.Toolbar(closeUp ? 1 : 0, CameraModes) == 1;

        int rounds = EnemyFireAdvancedVisuals.PreviewRoundCount(enemyIndex, mode);
        if (rounds > 0)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Nhịp thiêu đốt mô phỏng");
                for (int i = 0; i < rounds; i++)
                    if (GUILayout.Button($"Lượt {i + 1}"))
                        SelectTime(EnemyFireAdvancedVisuals.PreviewRoundTime(enemyIndex, Current.Hit, i) + 0.051f);
            }
        }

        Rect area = GUILayoutUtility.GetRect(0f, 10000f, 150f, 10000f,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (Event.current.type != EventType.Repaint || area.width < 2f || area.height < 2f)
            return;

        view ??= new PreviewView(artwork);
        var frame = view.Render(area, enemyIndex, mode, time, vfxScale, closeUp);
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
        float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt) * (slow ? 0.5f : 1f);
        time = Mathf.Min(elapsed, Current.End);
        playing = elapsed < Current.End;
        Repaint();
    }

    private readonly struct Timing
    {
        public readonly float Release;
        public readonly float Hit;
        public readonly float End;

        public Timing(float release, float hit, float end)
        {
            Release = release;
            Hit = hit;
            End = end;
        }
    }

    private sealed class Artwork
    {
        public readonly EnemyAttackVfxArt Effects = EnemyAttackVfxArt.Load();
        public readonly EnemyArt[] Characters = new EnemyArt[EnemyIds.Length];
        public readonly Sprite Player = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprite/Player.png");

        public Artwork()
        {
            for (int i = 0; i < Characters.Length; i++)
                Characters[i] = new EnemyArt(EnemyIds[i]);
        }

        public bool IsReady(int index) => Effects && Effects.IsReady && Player && Characters[index].Idle
            && (index < ExistingEnemyCount || Effects.HasAdvancedArt);
    }

    private sealed class EnemyArt
    {
        private readonly ObjectReferenceKeyframe[][] attacks;
        public readonly Sprite Idle;

        public EnemyArt(string id)
        {
            var idle = LoadFrames(id, "Fly");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Project/Resources/Prefab/Base/Enemy/Fire/Enemy_{id}.prefab");
            var renderer = prefab ? prefab.GetComponentInChildren<SpriteRenderer>() : null;
            Idle = idle.Length > 0 ? idle[0].value as Sprite : renderer ? renderer.sprite : null;
            attacks = new[] { LoadFrames(id, "Atack_1"), LoadFrames(id, "Atack_2") };
        }

        private static ObjectReferenceKeyframe[] LoadFrames(string id, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipPath}{id}/{name}.anim");
            if (clip)
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    if (binding.propertyName == "m_Sprite")
                        return AnimationUtility.GetObjectReferenceCurve(clip, binding);
            return Array.Empty<ObjectReferenceKeyframe>();
        }

        public Sprite Sample(int mode, float time)
        {
            var keys = attacks[mode];
            if (time < 0f || keys.Length == 0 || time > keys[keys.Length - 1].time + 0.10f)
                return Idle;
            for (int i = keys.Length - 1; i >= 0; i--)
                if (keys[i].time <= time)
                    return keys[i].value as Sprite ?? Idle;
            return Idle;
        }
    }

    private sealed class PreviewView : IDisposable
    {
        private static readonly Vector3 PlayerPosition = new(-12f, 0f);
        private static readonly Vector3 EnemyPosition = new(10f, 0f);
        private static readonly Vector3 HitPoint = PlayerPosition + new Vector3(0.35f, 0.55f);
        private static readonly Vector3 Feet = PlayerPosition + Vector3.down * 2.45f;
        private static readonly Vector3[] Muzzles =
        {
            new(-1.5f, 0.35f), new(-1.5f, -0.05f), new(-2.1f, 1.0f), new(-1.85f, 1.55f),
            new(-2f, 1f), new(-2.15f, 0.85f), new(-2.85f, 1.5f)
        };

        private readonly PreviewRenderUtility preview = new();
        private readonly Artwork artwork;
        private readonly SpriteRenderer actor;
        private readonly EnemyAttackVfxVisuals visuals;
        private readonly Transform effectRoot;
        private EnemyFireAdvancedVisuals advanced;

        public PreviewView(Artwork art)
        {
            artwork = art;
            Camera camera = preview.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(35, 38, 49, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.allowHDR = false;
            camera.transform.position = new Vector3(-1f, 1.6f, -35f);
            var player = CreateRenderer("Player", 15);
            PlaceCharacter(player, art.Player, PlayerPosition, 5.6f);
            actor = CreateRenderer("Enemy", 15);
            actor.flipX = true;
            var effectObject = new GameObject("Attack VFX") { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(effectObject);
            effectRoot = effectObject.transform;
            visuals = new EnemyAttackVfxVisuals(effectRoot, art.Effects);
        }

        private SpriteRenderer CreateRenderer(string name, int order)
        {
            var root = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void PlaceCharacter(SpriteRenderer renderer, Sprite sprite, Vector3 position, float height)
        {
            renderer.sprite = sprite;
            renderer.transform.position = position;
            renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        }

        public Texture Render(Rect area, int enemy, int mode, float time, float scale, bool closeUp)
        {
            Timing timing = Timeline[enemy, mode];
            Vector3 caster = EnemyPosition;
            Vector3 meleeStance = Feet + new Vector3(enemy == 6 ? 5.4f : 4.9f, 0.24f);
            bool weaponSwing = enemy == 4 || (enemy == 6 && mode == 0);
            if (weaponSwing)
            {
                float approach = 1f - Mathf.Pow(1f - Unit(time / timing.Release), 2f);
                float retreat = Smooth(timing.Hit + 0.14f, timing.Hit + 0.41f, time);
                caster = Vector3.Lerp(EnemyPosition, meleeStance, approach * (1f - retreat));
            }
            else if (enemy == 1)
            {
                Vector3 near = PlayerPosition + Vector3.right * 3.7f;
                float approach = 1f - Mathf.Pow(1f - Unit(time / timing.Release), 2f);
                float retreat = Smooth(timing.Hit + 0.10f, timing.Hit + 0.36f, time);
                caster = Vector3.Lerp(EnemyPosition, near, approach * (1f - retreat));
            }

            float animationTime = time - (enemy == 1 ? 0.24f : 0.04f);
            if (weaponSwing)
                animationTime = SampleWeaponClip(enemy == 4 && mode == 0, timing, time);
            if (enemy == 5 && mode == 1)
                animationTime = SampleBreathClip(timing.Release, time);

            float height = enemy == 6 ? 8.3f : enemy >= ExistingEnemyCount ? 6.4f : 5.7f;
            PlaceCharacter(actor, artwork.Characters[enemy].Sample(mode, animationTime), caster, height);
            Vector3 source = caster + Muzzles[enemy];
            if (enemy == 5 && mode == 1)
                source = caster + new Vector3(-2.42f, 3.05f);
            var style = (EnemyAttackVfxStyle)(1 + enemy * 2 + mode);
            float windup = enemy == 1 ? 0.20f : timing.Hit;
            var frameTiming = new EnemyAttackVfxFrame(timing.Release, timing.Hit, timing.Hit, windup);
            visuals.Render(enemy < ExistingEnemyCount ? style : EnemyAttackVfxStyle.None, time, scale, frameTiming,
                source, HitPoint, Feet, Vector3.left, 0, 20);
            if (enemy >= ExistingEnemyCount)
            {
                advanced ??= new EnemyFireAdvancedVisuals(effectRoot, artwork.Effects);
                advanced.Render(style, time, scale, frameTiming, meleeStance, source, HitPoint, Feet,
                    simulateRounds: true);
            }
            else
            {
                advanced?.Clear();
            }

            float closeCameraSize = 11.8f * Mathf.Max(1f, area.height / area.width);
            preview.camera.orthographicSize = closeUp ? closeCameraSize : MatchCameraSize;
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

        private static float SampleWeaponClip(bool normalHammer, Timing timing, float time)
        {
            float windupPose = normalHammer ? 0.15f : 0f;
            float contactPose = normalHammer ? 0.30f : 0.15f;
            if (time < timing.Release)
                return Mathf.Lerp(0f, windupPose, Unit(time / timing.Release));
            if (time < timing.Hit)
                return Mathf.Lerp(windupPose, contactPose, Mathf.InverseLerp(timing.Release, timing.Hit, time));
            return contactPose + (time - timing.Hit) + 0.001f;
        }

        private static float SampleBreathClip(float release, float time)
        {
            if (time < release)
                return 0f;
            if (time < release + 0.60f)
                return 0.151f;
            return 0.301f + (time - release - 0.60f);
        }

        private static float Unit(float value) => Mathf.Clamp01(value);
        private static float Smooth(float start, float end, float time) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, time));

        public void Dispose()
        {
            advanced?.Dispose();
            visuals.Dispose();
            preview.Cleanup();
        }
    }
}
