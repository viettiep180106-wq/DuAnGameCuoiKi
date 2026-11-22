using System.Collections.Generic;
using UnityEngine;
using static EnemyAtlasVfxLayers;
using static EnemyWoodFx;

internal sealed class EnemyWoodAttackVfx
{
    internal const float DurationMultiplier = 2f;
    private static readonly Vector3[] PreviewTimes =
    {
        new(.29f,.51f,1.12f), new(.28f,.68f,1.86f), new(.26f,.48f,.98f), new(.25f,1.04f,2.32f),
        new(.24f,.58f,1.25f), new(.31f,.91f,1.97f), new(.24f,.66f,1.26f), new(.25f,.73f,1.98f),
        new(.25f,.55f,1.66f), new(.29f,.87f,2.26f), new(.23f,.57f,1.71f), new(.28f,.87f,2.24f),
        new(.30f,.61f,1.40f), new(.34f,.96f,2.61f)
    };

    private readonly EnemyAtlasVfxArt art;
    private readonly EnemyAtlasVfxLayers layers;
    private readonly Dictionary<EnemyBase, float> previousHp = new(8);
    private readonly List<TeamResult> team = new(5);
    private EnemyAttackVfxStyle style;
    private EnemyBase caster;
    private Player target;
    private Vector3 timing;
    private Vector3 source;
    private Vector2 muzzleOffset;
    private float scale;
    private float estimatedContact;
    private float missedAt;
    private float casterHp;
    private bool siphoned;
    private bool revivedAny;
    private float phase;
    private float hitAge;

    private readonly struct TeamResult
    {
        internal readonly EnemyBase Ally;
        internal readonly bool Revived;
        internal readonly bool Healed;
        internal TeamResult(EnemyBase ally, bool revived, bool healed) => (Ally, Revived, Healed) = (ally, revived, healed);
    }

    internal EnemyWoodAttackVfx(Transform root, EnemyAtlasVfxArt art)
    {
        this.art = art;
        layers = new EnemyAtlasVfxLayers(root, art);
    }

    internal static bool Supports(EnemyAttackVfxStyle style) =>
        style is >= EnemyAttackVfxStyle.WoodenClaw and <= EnemyAttackVfxStyle.AncientRevival;
    internal bool UsesArmor => style == EnemyAttackVfxStyle.ThornShell;
    internal bool UsesTeam => style is EnemyAttackVfxStyle.NatureRoar or EnemyAttackVfxStyle.BlessingRain
        or EnemyAttackVfxStyle.RootRevival or EnemyAttackVfxStyle.TalismanRevival or EnemyAttackVfxStyle.AncientRevival;
    internal float Duration => UsesArmor ? .55f : timing.z * DurationMultiplier;

    internal void Begin(EnemyAttackVfxStyle kind, EnemyBase enemy, Player player, Vector2 offset, float size, int layer, int order)
    {
        style = kind;
        caster = enemy;
        target = player;
        muzzleOffset = offset;
        scale = size;
        int index = (int)kind - (int)EnemyAttackVfxStyle.WoodenClaw;
        timing = PreviewTimes[index];
        var action = index % 2 == 0 ? enemy.GetNormalAttackActionType() : enemy.GetSkillActionType();
        estimatedContact = action == EnemySkillActionType.MeleeDash ? .30f : timing.y * DurationMultiplier;
        missedAt = float.PositiveInfinity;
        siphoned = false;
        revivedAny = false;
        previousHp.Clear();
        team.Clear();
        FollowCaster(player.transform.position.x <= caster.transform.position.x ? -1f : 1f);
        int behind = player.spriteRenderer ? player.spriteRenderer.sortingOrder - 1 : order - 2;
        layers.SetSorting(layer, order, behind);
    }

    internal float BindProjectile(float age, float duration, float expectedContact)
    {
        estimatedContact = age + Mathf.Max(.025f, expectedContact);
        return Mathf.Max(Duration, age + duration + .25f);
    }

    internal float Impact(float age) => UsesArmor ? age + .12f : UsesTeam ? Duration :
        Mathf.Max(Duration, age + (timing.z - timing.y) * DurationMultiplier);

    internal float Miss(float age)
    {
        missedAt = age;
        return Mathf.Max(Duration, age + .30f);
    }

    internal void BeforeGameplay()
    {
        casterHp = caster.CurrentHP;
        var controller = EnemyController.instance;
        if (!UsesTeam || !controller) return;
        foreach (var ally in controller.LivingEnemies)
            if (Alive(ally)) previousHp[ally] = ally.CurrentHP;
        foreach (var ally in controller.DeadEnemiesInWave)
            if (ally && ally.CurrentHP <= 0f) previousHp[ally] = 0f;
    }

    internal void AfterGameplay()
    {
        if (!caster) return;
        siphoned = style == EnemyAttackVfxStyle.RootWhip && caster.CurrentHP > casterHp;
        if (UsesArmor && caster.currentShield > 0f)
            EnemyWoodStatusVfx.ShowEnemy(art, caster, EnemyWoodStatusVfx.Style.Bark);
        var controller = EnemyController.instance;
        if (!UsesTeam || !controller) return;
        foreach (var ally in controller.LivingEnemies)
        {
            if (!Alive(ally) || !previousHp.TryGetValue(ally, out float before)) continue;
            bool revived = before <= 0f;
            revivedAny |= revived;
            bool heals = style is EnemyAttackVfxStyle.BlessingRain or EnemyAttackVfxStyle.RootRevival or EnemyAttackVfxStyle.AncientRevival;
            team.Add(new TeamResult(ally, revived, heals && ally.CurrentHP > before));
            if (style == EnemyAttackVfxStyle.NatureRoar && ally.atkBuffRounds > 0 && ally.atkBuffPercent > 0)
                EnemyWoodStatusVfx.ShowEnemy(art, ally, EnemyWoodStatusVfx.Style.Attack);
            if (style == EnemyAttackVfxStyle.TalismanRevival && ally.defBuffRounds > 0 && ally.defBuffPercent > 0)
                EnemyWoodStatusVfx.ShowEnemy(art, ally, EnemyWoodStatusVfx.Style.Defense);
            if (style == EnemyAttackVfxStyle.AncientRevival && ally.shieldRounds > 0 && ally.currentShield > 0)
                EnemyWoodStatusVfx.ShowEnemy(art, ally, EnemyWoodStatusVfx.Style.AncientShield);
        }
        layers.EnsureCapacity(team.Count * 4 + 4, team.Count * 18);
    }

    internal void RecordPoison()
    {
        if (target && style is EnemyAttackVfxStyle.TalismanBolt or EnemyAttackVfxStyle.DeadwoodArrow)
            EnemyWoodStatusVfx.ShowPoison(art, target, style == EnemyAttackVfxStyle.TalismanBolt);
    }

    private void FollowCaster(float direction)
    {
        var bounds = EnemyAttackVfx.CharacterBounds(caster);
        source = bounds.center + new Vector3(direction * muzzleOffset.x, muzzleOffset.y);
    }

    internal void Render(float age, float contact, float end, Vector3 hit, Vector3 feet, Vector3 facing)
    {
        if (UsesArmor) return;
        bool contacted = !float.IsPositiveInfinity(contact);
        hitAge = contacted ? Mathf.Max(0f, age - contact) * (timing.z - timing.y) / Mathf.Max(.04f, end - contact) : -1f;
        phase = UsesTeam ? age / DurationMultiplier : contacted ? timing.y + hitAge : timing.y * Mathf.Clamp01(age / estimatedContact);
        bool moving = !contacted && age < missedAt;
        if (moving && caster && caster.gameObject.activeInHierarchy) FollowCaster(facing.x);
        layers.BeginFrame();
        switch (style)
        {
            case EnemyAttackVfxStyle.WoodenClaw:
                Swing(0, hit, facing, 6.2f, 42f);
                ImpactBurst(1, hit, hitAge, 5.6f, Grain);
                break;
            case EnemyAttackVfxStyle.BarkBite:
                float biteAge = phase - timing.y;
                float open = 1f - Smooth(-.14f,.02f,biteAge);
                layers.Draw(5, hit + Vector3.up * S(open * .35f), S(Mathf.Lerp(4.15f, 6.2f,open)),
                    Pulse(biteAge,-.18f,-.10f,.04f,.19f), phase, angle: Mathf.Sin(biteAge*9f)*12f, warp: .021f);
                ImpactBurst(6, feet + Vector3.up * S(.95f), hitAge, 5.5f, Grain, .28f);
                break;
            case EnemyAttackVfxStyle.DeadwoodArrow:
                if (moving) Projectile(10, hit, 5.9f, 14f, .12f);
                ImpactBurst(11, hit, hitAge, 4.0f, Violet, .28f);
                break;
            case EnemyAttackVfxStyle.PoisonArrowRain:
                if (moving) RainArrows(feet);
                float poison = Pulse(hitAge,.02f,.21f,.47f,.96f);
                layers.Draw(14, hit + Vector3.down * S(.93f - .9f*Smooth(0f,.8f,hitAge)),
                    S(Mathf.Lerp(2.7f,8.4f,Smooth(0f,.54f,hitAge))), poison, hitAge, warp: .064f);
                if (hitAge >= 0f)
                {
                    layers.Ring(feet, hitAge, .61f, S(8.5f), .25f, Leaf);
                    layers.Burst(feet, hitAge, 38, S(12f), S(.48f), Violet, life: .65f);
                }
                break;
            case EnemyAttackVfxStyle.FlowerPollen:
                if (moving) Projectile(15, hit, 5.2f, 0f, 1f);
                ImpactBurst(16, hit, hitAge, 5.8f, Sun, .49f);
                break;
            case EnemyAttackVfxStyle.RootWhip:
                Swing(20, hit, facing, 7.6f, 19f);
                ImpactBurst(21, feet, hitAge, 6.1f, Grain);
                if (siphoned) Siphon(hit);
                break;
            case EnemyAttackVfxStyle.TalismanBolt:
                if (moving) Paper(hit);
                ImpactBurst(26, hit, hitAge, 5.9f, Sun, .46f);
                break;
            case EnemyAttackVfxStyle.AncientRootStrike:
                AncientStrike(feet);
                break;
            default:
                DrawTeam(facing);
                break;
        }
        layers.EndFrame(false);
    }

    private float S(float n) => n * scale;

    private void Swing(int cell, Vector3 hit, Vector3 facing, float width, float twist)
    {
        float move = Smooth(timing.x,timing.y,phase);
        float alpha = Pulse(phase,timing.x,timing.x+.044f,timing.y+.035f,timing.y+.18f);
        if (alpha <= .004f) return;
        Vector3 start = source - facing * S(1.5f) + Vector3.up * S(1.27f);
        Vector3 ctrl = source + facing * S(1.65f) + Vector3.up * S(2.55f);
        Vector3 Arc(float t) => Vector3.Lerp(Vector3.Lerp(start,ctrl,t),Vector3.Lerp(ctrl,hit,t),t);
        Vector3 point = Arc(move);
        Vector3 tangent = point - Arc(Mathf.Max(0f,move-.05f));
        float angle = Angle(tangent);
        layers.Draw(cell, point - tangent.normalized * S(.98f), S(width), alpha, phase, angle: angle + twist,
            warp: .021f, reveal: Mathf.Clamp01(.17f+move), radial: true);
        for (int i=0;i<18;i++)
            layers.Particle(Arc(move*i/18f)+Vector3.up*S(Seed(i)*.4f),S(.16f+.19f*Seed(i+9)),Leaf,alpha*.66f,angle);
    }

    private void Projectile(int cell, Vector3 hit, float size, float authored, float lift)
    {
        float t = Progress(timing.x,timing.y,phase);
        if (t <= 0f) return;
        Vector3 Path(float u) => Vector3.Lerp(source,hit,u)+Vector3.up*S(Mathf.Sin(u*Mathf.PI)*lift);
        float alpha = Smooth(0f,.11f,t);
        layers.Draw(cell, Path(t), S(size), alpha, phase,
            angle: cell == 15 ? phase*27f : Angle(hit-source)-authored, warp: cell == 15 ? .041f : .012f);
        for (int i=0;i<12;i++)
        {
            float part = t-i*.027f;
            if (part <= 0f) continue;
            layers.Particle(Path(part)+Vector3.up*S(Mathf.Sin(i+part*12f)*.11f*i),S(.33f-.015f*i),Leaf,
                alpha*(1f-i/13f)*.79f,i*34f);
        }
    }

    private void Paper(Vector3 hit)
    {
        float t = Progress(timing.x,timing.y,phase);
        if (t <= 0f) return;
        Vector3 direction = (hit-source).normalized;
        Vector3 center = Vector3.Lerp(source,hit,t);
        float motion = phase - timing.x;
        float alpha = Smooth(0f,.10f,t);
        layers.Talisman(center,direction,scale,alpha,motion,Smooth(0f,.48f,t));
        Vector3 normal = new(-direction.y,direction.x);
        for (int i=0;i<13;i++)
        {
            float u=Mathf.Repeat(motion*(2.7f+Seed(i))+.139f*i,1f);
            Vector3 trail=center+direction*S(.8f-u*6f)+normal*(S(1.42f)*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(motion*26f+i*1.3f));
            layers.Particle(trail,S(.24f+.13f*Seed(i+4)),i%3==0?Sun:Mint,alpha*Mathf.Sin(u*Mathf.PI)*.82f);
        }
    }

    private void RainArrows(Vector3 feet)
    {
        float cast = phase - timing.x;
        float shot = Smooth(0f,.30f,cast);
        Vector3 release = source + new Vector3(-S(5.7f)*shot, S(8.5f)*shot);
        layers.Draw(12,release,S(5.15f),Pulse(cast,0f,.044f,.19f,.32f),phase,angle:127f,warp:.027f);
        for (int i=0;i<7;i++)
        {
            float fall = phase - timing.y + .43f - i*.040f;
            if (fall < 0f || fall > .28f) continue;
            Vector3 ground = feet + Vector3.right*S((Seed(7+i)-.5f)*6.2f);
            Vector3 top = ground + new Vector3(S(4.5f+i*.34f), S(14f+Seed(i+33)*4.3f));
            float progress = Progress(0f,.28f,fall);
            layers.Draw(12,Vector3.Lerp(top,ground,progress*progress),S(3.1f+Seed(i)*.37f),
                Smooth(0f,.05f,fall),phase,angle:Angle(ground-top)-19f);
        }
        layers.Draw(13,feet + Vector3.up * S(5.2f),S(6.8f),Pulse(phase-timing.y,-.26f,-.16f,-.057f,.035f)*.75f,
            phase,warp:.030f);
    }

    private void Siphon(Vector3 hit)
    {
        float age = hitAge - .09f;
        if (age < 0f || age > .66f) return;
        float t = Smooth(0f,.46f,age);
        Vector3 end = caster ? EnemyAttackVfx.CharacterBounds(caster).center : source;
        Vector3 center = Vector3.Lerp(hit,end,t) + Vector3.up*S(Mathf.Sin(Mathf.PI*t)*2.9f);
        float alpha = Pulse(age,0f,.06f,.40f,.65f);
        layers.Draw(24,center,S(3.7f),alpha,age,angle:age*515f,warp:.03f);
        layers.Orbit(center,age,S(1.4f),S(.7f),Mint,alpha*.86f);
    }

    private void AncientStrike(Vector3 feet)
    {
        if (hitAge < 0f) return;

        ImpactBurst(31,feet,hitAge,10.1f,Grain,.57f);
        layers.Ring(feet,hitAge,.67f,S(12.4f),.22f,Sun);

        const float dustLead = .06f;
        float rootAge = hitAge - dustLead;
        float crackSpread = Smooth(0f,.12f,rootAge);
        float crackHeight = S(Mathf.Lerp(1.05f,3.1f,crackSpread));
        layers.Draw(EnemyWoodVfxArt.AncientRootCrack,feet + Vector3.up*crackHeight*.5f,
            S(Mathf.Lerp(4.9f,10.5f,crackSpread)),Pulse(rootAge,0f,.028f,.105f,.25f)*.88f,
            rootAge,height:crackHeight,reveal:.91f,rise:true);

        float rise = Smooth(0f,.15f,rootAge);
        float hold = 1f - Smooth(.26f,.49f,rootAge);
        float height = S(9.4f)*rise*hold;
        layers.Draw(30,feet + Vector3.up*height*.5f,S(Mathf.Lerp(5.2f,9.25f,rise)),rise*hold,
            phase,height:height,warp:.021f,reveal:rise,rise:true);
    }

    private void DrawTeam(Vector3 facing)
    {
        float cast = phase - timing.x;
        if (style == EnemyAttackVfxStyle.NatureRoar)
        {
            float spread = Smooth(0f,.35f,cast);
            Vector3 tip = source + facing*S(spread*4.7f);
            layers.Draw(2,tip,S(Mathf.Lerp(1.1f,8.8f,spread)),Pulse(cast,0f,.055f,.20f,.37f),phase,warp:.032f);
            layers.Ring(tip,cast,.36f,S(9.3f),.86f,Leaf,ground:false);
        }

        Bounds formation = new();
        bool hasFormation = false;
        for (int i=0;i<team.Count;i++)
        {
            TeamResult entry = team[i];
            if (!Alive(entry.Ally)) continue;
            var bounds = EnemyAttackVfx.CharacterBounds(entry.Ally);
            if (hasFormation) formation.Encapsulate(bounds);
            else { formation = bounds; hasFormation = true; }
            var feet = Feet(bounds);
            float unit = Mathf.Clamp(bounds.size.y / 5.6f,.53f,2.1f);
            float local = phase - timing.y - i*.086f;
            if (style == EnemyAttackVfxStyle.RootRevival && !revivedAny)
                layers.Draw(22,feet + Vector3.up*unit*.75f,unit*4.4f,Pulse(local,-.26f,-.07f,.18f,.52f)*.65f,local,
                    height:unit*1.7f,ground:true,rise:true,reveal:Smooth(-.22f,.12f,local));
            if (entry.Revived)
            {
                int cradle = style == EnemyAttackVfxStyle.AncientRevival ? 34 : style == EnemyAttackVfxStyle.TalismanRevival ? 27 : 22;
                Revival(layers,cradle,feet,local,unit);
            }
            if (style == EnemyAttackVfxStyle.BlessingRain)
                Blessing(layers,feet,local,unit);
            if (entry.Healed)
                HealRise(layers,feet,local-.29f,unit);
        }

        if (!hasFormation || style != EnemyAttackVfxStyle.AncientRevival) return;
        var basePoint = Feet(formation);
        float width = Mathf.Max(S(7f),formation.size.x+S(4f));
        float canopy = Pulse(cast,.025f,.26f,.79f,1.37f);
        layers.Draw(32,basePoint+Vector3.up*S(7.3f),Mathf.Max(S(11f),width),canopy,phase,
            height:S(12.1f),warp:.039f,ground:true);
        float shield = Smooth(.10f,.49f,phase-timing.y);
        layers.Draw(33,basePoint+Vector3.up*S(3.7f),width*shield,
            Pulse(phase-timing.y,.12f,.46f,.98f,1.54f)*.56f,phase,height:S(7.5f)*shield,warp:.023f,rise:true,reveal:shield);
    }

    private void ImpactBurst(int id, Vector3 hit, float a, float width, Color tint, float fade = .44f)
    {
        if (a < 0f) return;
        float growth=Mathf.Lerp(.26f,1f,Smooth(0f,.18f,a));
        layers.Draw(id,hit+Vector3.up*S(.28f*growth),S(width*growth),Pulse(a,0f,.033f,.12f,fade),a,warp:.026f);
        layers.Burst(hit,a,25,S(10.2f),S(.46f),tint,life:fade+.16f,gravity:S(36f));
    }

    internal void Clear()
    {
        caster = null;
        target = null;
        previousHp.Clear();
        team.Clear();
        layers.BeginFrame();
        layers.EndFrame(false);
    }

    internal void Dispose() => layers.Dispose();
}

internal static class EnemyWoodFx
{
    internal static readonly Color Leaf = new(.49f, .89f, .29f);
    internal static readonly Color Mint = new(.42f, .97f, .65f);
    internal static readonly Color Sun = new(.86f, .94f, .48f);
    internal static readonly Color Grain = new(.76f, .59f, .35f);
    internal static readonly Color Violet = new(.75f, .36f, .93f);
    internal static float Progress(float from, float to, float value) => Mathf.InverseLerp(from,to,value);
    internal static float Smooth(float from, float to, float value) => Mathf.SmoothStep(0f,1f,Progress(from,to,value));
    internal static float Pulse(float value, float start, float peak, float hold, float end) =>
        Smooth(start,peak,value)*(1f-Smooth(hold,end,value));
    internal static bool Alive(EnemyBase ally) => ally && ally.CurrentHP > 0f && ally.gameObject.activeInHierarchy;
    internal static Vector3 Feet(Bounds bounds) =>
        new(bounds.center.x,bounds.min.y+bounds.size.y*.055f,bounds.center.z);

    internal static void HealRise(EnemyAtlasVfxLayers layers, Vector3 feet, float age, float unit)
    {
        float alpha = Pulse(age,0f,.085f,.27f,.78f);
        if (alpha <= .004f) return;
        Vector3 at = feet + Vector3.up*unit*Mathf.Lerp(.8f,5.9f,Smooth(0f,.72f,age));
        layers.Draw(19,at,unit*3.3f,alpha,age,warp:.025f);
        layers.Orbit(at,age,unit*1.33f,unit*1.2f,Mint,alpha*.55f,7);
    }

    internal static void Blessing(EnemyAtlasVfxLayers layers, Vector3 feet, float age, float unit)
    {
        float fade = Pulse(age,-.43f,-.35f,.025f,.28f);
        layers.Draw(18,feet,unit*5.25f,Pulse(age,-.55f,-.35f,.38f,.68f),age,height:unit*1.66f,
            reveal:Smooth(-.55f,-.18f,age),radial:true,ground:true);
        layers.Draw(17,feet+Vector3.up*unit*(4.5f-(age+.43f)*3f),unit*3.85f,fade,age,warp:.027f);
        if (fade <= .004f) return;
        for(int i=0;i<10;i++)
        {
            float rain=Mathf.Repeat((age+.43f)*2.55f+i*.14f,1f);
            var point=feet+new Vector3((Seed(i+83)-.5f)*unit*3.9f,unit*Mathf.Lerp(7.5f,.44f,rain));
            layers.Particle(point,unit*(.16f+Seed(i)*.12f),Mint,Mathf.Sin(rain*Mathf.PI)*fade);
        }
    }

    internal static void Revival(EnemyAtlasVfxLayers layers, int cell, Vector3 feet, float age, float unit)
    {
        float growth = Smooth(-.34f,.08f,age);
        float fade = Pulse(age,-.35f,-.12f,.31f,.71f);
        float height = unit*Mathf.Lerp(1.65f,5.65f,growth);
        layers.Draw(cell,feet+Vector3.up*height*.5f,unit*Mathf.Lerp(2.5f,6.2f,growth),fade,age,height:height,
            angle:cell==27?age*211f:0f,warp:.031f,rise:cell!=27,reveal:growth,radial:cell==27);
        if (cell != 27)
            layers.Draw(23,feet+Vector3.up*unit*Mathf.Lerp(1.05f,3.65f,Smooth(-.12f,.33f,age)),unit*3.45f,
                Pulse(age,-.14f,.055f,.19f,.46f),age,angle:age*98f);
        layers.Ring(feet,age+.27f,.54f,unit*6.6f,.24f,Sun);
    }
}
