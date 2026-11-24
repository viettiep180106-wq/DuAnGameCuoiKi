using UnityEngine;

public static class EnemyStatusAnimation
{
    public const string AtlasResource = "VFX/EnemyStatus/EnemyStatusAtlas";
    public const string BurnShaderResource = "VFX/EnemyStatus/EnemyStatusBurn";
    public const float Intro = .24f;
    public const float Outro = .72f;

    public enum Kind { Burn, Freeze, Slow, Stun, DefenseDown }

    // Coordinates use positive Y down, as they do in the Editor preview.
    public interface ICanvas
    {
        void Draw(Rect art, Vector2 anchor, float width, float alpha, float angle = 0f, bool bottom = false, float heightScale = 1f);
        void DrawFlames(Vector2 feet, float unit, Vector4 motion);
        void DrawOrbit(Vector2 center, float unit, float tilt, float opacity);
    }

    // Pixel rectangles in the preview atlas, ordered by status and then by phase.
    private static readonly Rect[,] Art =
    {
        { new(66, 66, 215, 245), new(52, 378, 242, 240), new(92, 685, 163, 219) },
        { new(336, 101, 269, 211), new(323, 421, 290, 185), new(340, 744, 262, 166) },
        { new(640, 121, 258, 188), new(634, 395, 285, 214), new(698, 732, 158, 180) },
        { new(941, 85, 227, 214), new(926, 402, 279, 179), new(981, 738, 151, 149) },
        { new(1228, 86, 268, 228), new(1260, 392, 205, 230), new(1278, 694, 156, 226) }
    };
    public static Rect BurnFallback => Art[0, 1];
    private static readonly Rect Ember = new(70, 490, 29, 33);
    private static readonly Rect BurnSmoke = new(77, 387, 102, 94);
    private static readonly Rect IceChip = new(496, 758, 37, 47);
    private static readonly Rect Droplet = new(691, 144, 38, 41);
    private static readonly Rect Star = new(985, 751, 76, 84);
    private static readonly Rect StoneChip = new(1348, 822, 47, 46);

    public static float Hold(float elapsed, float endAge) =>
        Mathf.SmoothStep(0f, 1f, elapsed / Intro) * (1f - Mathf.SmoothStep(0f, 1f, endAge / .19f));

    public static void Draw(ICanvas canvas, Kind kind, Vector2 feet, Vector2 head, float unit,
        float elapsed, float endAge, float pulse = 0f)
    {
        float hold = Hold(elapsed, endAge);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, endAge / Outro);
        switch (kind)
        {
            case Kind.Burn: DrawBurn(canvas, feet, unit, elapsed, hold, endAge, fade, pulse); break;
            case Kind.Freeze: DrawFreeze(canvas, feet, unit, hold, endAge, fade); break;
            case Kind.Slow: DrawSlow(canvas, feet, unit, elapsed, hold, endAge, fade); break;
            case Kind.Stun: DrawStun(canvas, head, unit, elapsed, hold, endAge, fade); break;
            case Kind.DefenseDown: DrawDefense(canvas, head, unit, elapsed, hold, endAge, fade); break;
        }

        if (elapsed < Intro && kind != Kind.Burn)
        {
            float phase = Mathf.Clamp01(elapsed / Intro);
            bool grounded = kind == Kind.Freeze || kind == Kind.Slow;
            Vector2 origin = grounded ? feet + Vector2.up * unit * .035f : head;
            canvas.Draw(Art[(int)kind, 0], origin, unit * Mathf.Lerp(.28f, .83f, phase),
                Mathf.Sin(phase * Mathf.PI) * .9f, bottom: grounded);
        }
    }

    private static void DrawStun(ICanvas canvas, Vector2 head, float unit, float elapsed, float hold, float age, float fade)
    {
        float tilt = Mathf.Sin(elapsed * 3f) * .24f;
        if (hold > .001f) canvas.DrawOrbit(head, unit, tilt, hold * .59f);
        for (int i = 0; i < 3; i++)
        {
            float angle = elapsed * 6.8f + i * 2f * Mathf.PI / 3f;
            float depth = .77f + .23f * Mathf.Sin(angle);
            canvas.Draw(Star, OrbitPoint(head, unit, angle, tilt), unit * .22f * depth, hold * depth, Mathf.Sin(angle) * 16f);
        }
        if (age >= 0f) canvas.Draw(Art[3, 2], head - Vector2.up * unit * age * .27f, unit * .47f, fade);
    }

    public static Vector2 OrbitPoint(Vector2 center, float unit, float angle, float tilt)
    {
        float x = Mathf.Cos(angle) * .43f;
        return center + new Vector2(x, Mathf.Sin(angle) * .105f + x * tilt) * unit;
    }

    private static void DrawBurn(ICanvas canvas, Vector2 feet, float unit, float elapsed, float hold, float age, float fade, float pulse)
    {
        float extinguish = age >= 0f ? fade : 1f;
        for (int i = 0; i < 4; i++)
        {
            float phase = Mathf.Repeat(elapsed * .86f + i * .25f, 1f);
            float side = Seed(i + 31) - .5f;
            Vector2 offset = new(side * (.18f + phase * .42f), -.17f - phase * .53f);
            float opacity = Mathf.Sin(phase * Mathf.PI) * extinguish * Mathf.Clamp01(elapsed / .35f) * .38f;
            canvas.Draw(BurnSmoke, feet + offset * unit, unit * (.11f + phase * .25f), opacity, side * phase * 36f);
        }

        if (hold > .001f)
        {
            float growth = Mathf.SmoothStep(0f, 1f, elapsed / Intro) * Mathf.Lerp(.45f, 1f, hold);
            canvas.DrawFlames(feet, unit, new Vector4(elapsed, growth, hold, pulse));
        }
        float emberOpacity = Mathf.Max(hold, age >= 0f ? fade * .55f : 0f);
        for (int i = 0; i < 12; i++)
        {
            float seed = Seed(i);
            float phase = Mathf.Repeat(elapsed * (.95f + seed * .52f) + Seed(i + 23), 1f);
            Vector2 offset = new((Seed(i + 9) - .5f) * (.28f + phase * .45f), -phase * (.47f + seed * .22f));
            offset.x += Mathf.Sin(phase * 4f + i) * phase * .095f;
            canvas.Draw(Ember, feet + offset * unit, unit * (.017f + seed * .024f) * (1f - phase * .46f),
                emberOpacity * Mathf.Sin(phase * Mathf.PI), i * 38f + phase * 120f);
        }
    }

    private static void DrawFreeze(ICanvas canvas, Vector2 feet, float unit, float hold, float age, float fade)
    {
        canvas.Draw(Art[1, 1], feet + Vector2.up * unit * .04f, unit * 1.16f, hold, bottom: true,
            heightScale: Mathf.Lerp(.26f, 1f, hold));
        if (age < 0f) return;
        canvas.Draw(Art[1, 2], feet - Vector2.up * unit * age * .13f, unit * (.57f + age * .74f), fade * .46f, bottom: true);
        Scatter(canvas, IceChip, feet - Vector2.up * unit * .10f, unit, age, fade, 9);
    }

    private static void DrawSlow(ICanvas canvas, Vector2 feet, float unit, float elapsed, float hold, float age, float fade)
    {
        Vector2 center = feet - Vector2.up * (unit * (.12f + Mathf.Sin(elapsed * 5f) * .024f));
        canvas.Draw(Art[2, 1], center, unit * (.78f + Mathf.Sin(elapsed * 7f) * .065f), hold * .91f,
            5f * Mathf.Sin(elapsed * 4f), heightScale: .64f);
        for (int i = 0; i < 8; i++)
        {
            float angle = elapsed * 4f - i * .52f;
            Vector2 point = center + new Vector2(Mathf.Cos(angle) * .40f, Mathf.Sin(angle) * .12f) * unit;
            canvas.Draw(Droplet, point, unit * (.047f - i * .003f), hold * (1f - i * .095f), angle * Mathf.Rad2Deg + 90f);
        }
        if (age >= 0f) canvas.Draw(Art[2, 2], center - Vector2.up * unit * age * .22f, unit * (.38f + age * .22f), fade);
    }

    private static void DrawDefense(ICanvas canvas, Vector2 head, float unit, float elapsed, float hold, float age, float fade)
    {
        Vector2 mark = head + Vector2.up * unit * (-.02f + Mathf.Sin(elapsed * 4f) * .035f);
        canvas.Draw(Art[4, 1], mark, unit * .31f, hold, 2f * Mathf.Sin(elapsed * 4f));
        if (age >= 0f)
        {
            canvas.Draw(Art[4, 2], mark + Vector2.up * unit * age * .58f, unit * .31f, fade * .86f, age * 22f);
            Scatter(canvas, StoneChip, mark, unit * .48f, age, fade, 5);
        }
    }

    private static void Scatter(ICanvas canvas, Rect art, Vector2 center, float unit, float age, float fade, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float side = Seed(i + 7) - .5f;
            float speed = .6f + Seed(i) * .88f;
            Vector2 offset = new(side * (age * speed * 1.55f + .22f), -age * speed + age * age * 2.05f);
            canvas.Draw(art, center + offset * unit, unit * (.055f + Seed(i + 2) * .10f), fade, i * 42f + age * 250f * side);
        }
    }

    private static float Seed(int index) => Mathf.Repeat(Mathf.Sin((index + 1) * 67.12f) * 129.81f, 1f);

    public static Rect AtlasUv(Rect source) =>
        new(source.x / 1536f, 1f - source.yMax / 1024f, source.width / 1536f, source.height / 1024f);
}
