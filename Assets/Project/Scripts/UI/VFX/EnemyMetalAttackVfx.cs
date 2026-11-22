using UnityEngine;
using static EnemyAtlasVfxLayers;
using static EnemyMetalFx;

internal sealed class EnemyMetalAttackVfx
{
    private static readonly Vector3[] PreviewTimes =
    {
        new(.23f,.38f,1.04f), new(.19f,.59f,1.72f), new(.24f,.40f,1.03f), new(.23f,.55f,1.35f),
        new(.26f,.43f,1.07f), new(.22f,.66f,1.82f), new(.23f,.57f,1.15f), new(.27f,.69f,1.40f),
        new(.25f,.45f,1.21f), new(.20f,.60f,2.03f), new(.23f,.55f,1.20f), new(.23f,.73f,1.50f),
        new(.29f,.96f,1.76f), new(.23f,.66f,2.00f)
    };

    private readonly EnemyAtlasVfxArt art;
    private readonly EnemyAtlasVfxLayers layers;
    private EnemyAttackVfxStyle style;
    private EnemyBase caster;
    private Player target;
    private Vector2 muzzleOffset;
    private Vector3 home;
    private Vector3 source;
    private Vector3 casterFeet;
    private Vector3 timing;
    private float scale;
    private float expectedContact;
    private float endedAt;
    private float direction;
    private float phase;
    private float hitAge;
    private bool followsMelee;
    private bool sourceLocked;

    internal EnemyMetalAttackVfx(Transform root, EnemyAtlasVfxArt art)
    {
        this.art = art;
        layers = new EnemyAtlasVfxLayers(root, art);
        layers.EnsureCapacity(30, 120);
    }

    internal static bool Supports(EnemyAttackVfxStyle style) =>
        style is >= EnemyAttackVfxStyle.SteelPunch and <= EnemyAttackVfxStyle.TitanLaser;
    internal bool UsesArmor => style is EnemyAttackVfxStyle.SteelFortress or EnemyAttackVfxStyle.IronBarrier or EnemyAttackVfxStyle.CounterCage;
    internal float Duration => UsesArmor ? .35f : timing.z;
    // Extremely short projectile flights still need time to show the launch and drop.
    private float MinimumContact => style is EnemyAttackVfxStyle.TitanPiston ? .64f : style is EnemyAttackVfxStyle.TitanLaser ? .34f : 0f;
    private float S(float value) => value * scale;
    private bool Left => direction < 0f;

    internal void Begin(EnemyAttackVfxStyle kind, EnemyBase enemy, Player player, Vector2 offset,
        float size, Vector3 facing, int layer, int order)
    {
        style = kind;
        caster = enemy;
        target = player;
        muzzleOffset = offset;
        scale = size;
        direction = facing.x;
        int index = (int)kind - (int)EnemyAttackVfxStyle.SteelPunch;
        timing = PreviewTimes[index];
        var action = index % 2 == 0 ? enemy.GetNormalAttackActionType() : enemy.GetSkillActionType();
        followsMelee = action == EnemySkillActionType.MeleeDash;
        expectedContact = followsMelee ? .30f : timing.y;
        sourceLocked = false;
        endedAt = float.PositiveInfinity;
        SetSource();
        home = source;
        int ground = player.spriteRenderer ? player.spriteRenderer.sortingOrder - 1 : order - 2;
        layers.SetSorting(layer, order, ground);
    }

    private void SetSource()
    {
        var bounds = EnemyAttackVfx.CharacterBounds(caster);
        casterFeet = Feet(bounds);
        source = bounds.center + new Vector3(muzzleOffset.x * direction, muzzleOffset.y);
    }

    internal float BindProjectile(float age, float duration, float contact)
    {
        expectedContact = Mathf.Max(MinimumContact, age + Mathf.Max(.025f, contact));
        return Mathf.Max(Duration, expectedContact + timing.z - timing.y, age + duration + .25f);
    }

    internal float Impact(float age)
    {
        if (UsesArmor) return age + .12f;
        if (caster && !sourceLocked) SetSource();
        sourceLocked = true;
        return Mathf.Max(MinimumContact, age) + timing.z - timing.y;
    }

    internal float Miss(float age)
    {
        endedAt = age;
        return age + .23f;
    }

    internal void AfterGameplay()
    {
        if (UsesArmor && caster)
            EnemyMetalStatusVfx.ShowArmor(art, caster, style);
        if (style == EnemyAttackVfxStyle.BloodStorm && target)
            EnemyMetalStatusVfx.ShowBleed(art, target);
    }

    internal void Render(float age, float contact, Vector3 hit, Vector3 feet)
    {
        if (UsesArmor) return;
        bool resolved = !float.IsPositiveInfinity(contact);
        float visualContact = resolved ? Mathf.Max(MinimumContact, contact) : expectedContact;
        bool landed = resolved && age >= visualContact;
        hitAge = landed ? age - visualContact : -1f;
        phase = landed ? timing.y + hitAge : timing.y * Mathf.Clamp01(age / Mathf.Max(.025f, visualContact));
        float missFade = float.IsPositiveInfinity(endedAt) ? 1f : 1f - Smooth(endedAt, endedAt + .23f, age);
        if (!sourceLocked && caster && caster.gameObject.activeInHierarchy)
        {
            SetSource();
            sourceLocked = !followsMelee && phase >= timing.x;
        }
        layers.BeginFrame();
        switch (style)
        {
            case EnemyAttackVfxStyle.SteelPunch:
                Punch(hit, feet);
                break;
            case EnemyAttackVfxStyle.TwinSwords:
                Blades(5, hit, 5.85f, 7f, Silver);
                Flash(layers, 6, hit, hitAge - .018f, S(3.05f), Silver, .23f);
                break;
            case EnemyAttackVfxStyle.ShadowAmbush:
                Ambush(hit);
                break;
            case EnemyAttackVfxStyle.IronThrust:
                Thrust(hit, feet);
                break;
            case EnemyAttackVfxStyle.SteelArrow:
                Arrow(hit, false, missFade);
                break;
            case EnemyAttackVfxStyle.PiercingArrow:
                Arrow(hit, true, missFade);
                break;
            case EnemyAttackVfxStyle.SawSweep:
                Saw(hit);
                break;
            case EnemyAttackVfxStyle.VenomDagger:
                Dagger(hit, missFade);
                break;
            case EnemyAttackVfxStyle.BloodStorm:
                BloodStorm(hit, missFade);
                break;
            case EnemyAttackVfxStyle.TitanPiston:
                Piston(feet, landed, missFade);
                break;
            case EnemyAttackVfxStyle.TitanLaser:
                Laser(hit, feet, landed, missFade);
                break;
        }
        layers.EndFrame(false);
    }

    private void Punch(Vector3 hit, Vector3 feet)
    {
        float strike = phase - timing.y;
        float move = Ease(Progress(-.065f, .095f, strike));
        Vector3 force = Vector3.Lerp(source, hit, move * .82f);
        // The preview's clockwise convention is reversed in world space. Mirror the attack toward the Player.
        layers.Draw(0, force, S(Mathf.Lerp(1.95f,5.35f,move)), Pulse(strike,-.065f,.025f,.058f,.24f), strike,
            height:S(Mathf.Lerp(1.2f,4.45f,move)), angle:90f * direction, flip:Left);
        layers.Ring(force, hitAge, .29f, S(5.9f), .32f, Silver, ground:false);
        Dust(layers, casterFeet, hitAge + .022f, S(5.55f), -direction);
        Dust(layers, feet, hitAge + .012f, S(5.77f), direction);
        layers.Burst(force, hitAge, 15, S(8.3f), S(.29f), Silver, .33f,
            heading:18f + (Left ? 144f : 0f), spread:74f);
    }

    private void Blades(int cell, Vector3 hit, float width, float tilt, Color tint)
    {
        float a = phase - timing.y;
        float progress = Smooth(-.075f,.068f,a);
        float alpha = Pulse(a,-.077f,-.027f,.085f,.235f);
        Vector3 center = hit + Vector3.right * S(Mathf.Lerp(-1.95f,.28f,progress) * direction);
        layers.Draw(cell, center, S(width), alpha, phase, angle:tilt * direction,
            reveal:progress, warp:.016f, flip:Left);
        for (int i = 0; i < 2; i++)
        {
            float slice = a - i * .041f;
            float u = Smooth(-.065f,.06f,slice);
            float slope = i == 0 ? 1f : -1f;
            var start = hit + new Vector3(-direction * S(width*.55f), -slope * S(width*.34f));
            var end = hit + new Vector3(direction * S(width*.55f), slope * S(width*.34f));
            Streak(layers, Vector3.Lerp(start,end,Mathf.Max(0f,u-.48f)), Vector3.Lerp(start,end,u),
                S(.30f), Pulse(slice,-.067f,-.017f,.06f,.139f) * .78f, slice, tint);
        }
    }

    private void Ambush(Vector3 hit)
    {
        float dash = phase - timing.x;
        Vector3 trail = source - home;
        if (trail.sqrMagnitude > .20f)
            layers.Draw(7, (source+home)*.5f, trail.magnitude,
                Pulse(dash,-.10f,.055f,.25f,.43f) * .86f, phase, height:S(3.25f),
                angle:Tilt(trail), warp:.08f, flip:Left);
        Blades(8, hit, 7.1f, 7f, Violet);
        Flash(layers, 6, hit, hitAge - .012f, S(3.55f), Violet, .23f);
        BurstArt(layers,9,hit,hitAge-.025f,S(7.2f),Gold,.43f);
    }

    private void Thrust(Vector3 hit, Vector3 feet)
    {
        float a = phase - timing.y;
        float u = Smooth(-.14f,.02f,a);
        Vector3 approach = Vector3.right * direction;
        Vector3 contact = new(hit.x, source.y, hit.z);
        float distance = Mathf.Max(0f, (contact.x - source.x) * direction);
        float width = S(Mathf.Lerp(1.75f,6.75f,u));
        Vector3 tip = contact - approach * distance * (1f-u);
        layers.Draw(10, tip - approach * width * .47f, width, Pulse(a,-.14f,-.05f,.056f,.27f), phase,
            height:S(Mathf.Lerp(.72f,1.68f,u)), flip:Left);
        Flash(layers,11,contact,hitAge,S(3.14f),Gold,.27f);
        Dust(layers,feet,hitAge-.04f,S(3.65f),direction);
    }

    private void Arrow(Vector3 hit, bool pierce, float fade)
    {
        float cast = phase - timing.x;
        Vector3 axis = (hit - source).normalized;
        float width = S(pierce ? 6.22f : 4.55f);
        if (pierce)
        {
            float push = Smooth(0f,.17f,cast);
            layers.Draw(18,source+axis*S(Mathf.Lerp(.85f,3.61f,push)),S(Mathf.Lerp(1.88f,6.25f,push)),
                Pulse(cast,0f,.022f,.077f,.28f)*fade,phase,height:S(Mathf.Lerp(1.38f,4.15f,push)),
                angle:Tilt(axis),warp:.036f,flip:Left);
        }
        float u = Progress(timing.x,timing.y,phase);
        Vector3 behind = hit + axis * S(3.9f);
        Vector3 tip = hitAge < 0f ? Vector3.Lerp(source,hit,u) : Vector3.Lerp(hit,behind,Progress(0f,.12f,hitAge));
        float alpha = Smooth(0f,.035f,cast) * fade;
        if (hitAge >= 0f) alpha *= pierce ? 1f-Smooth(.092f,.165f,hitAge) : 1f-Smooth(0f,.044f,hitAge);
        Streak(layers,tip-axis*S((pierce ? 4.8f : 3.5f)*u),tip,S(pierce?.58f:.21f),alpha*.62f,phase,pierce?Cyan:Silver);
        layers.Draw(pierce?17:15,tip-axis*width*.42f,width,alpha,phase,
            angle:Tilt(axis),warp:pierce?.048f:.027f,flip:Left);
        if (pierce)
        {
            Flash(layers,19,behind,hitAge-.12f,S(6.05f),Cyan,.32f);
            layers.Burst(behind,hitAge-.12f,24,S(16.2f),S(.28f),Cyan,.42f,heading:Left?167f:13f);
        }
        else Flash(layers,16,hit,hitAge,S(2.43f),Gold,.21f);
    }

    private void Saw(Vector3 hit)
    {
        float a = phase-timing.y;
        float u = Smooth(-.17f,.075f,a);
        Vector3 blade = Vector3.Lerp(source, hit-Vector3.right * S(.96f*direction),u);
        blade.y += S(1.84f*Mathf.Sin(Mathf.PI*u));
        float alpha = Pulse(a,-.19f,-.112f,.10f,.315f);
        float angle = phase * 1640f * direction;
        layers.Draw(20,blade,S(6.2f),alpha*.24f,angle:angle-29f*direction);
        layers.Draw(20,blade,S(5.2f),alpha,angle:angle);
        layers.Draw(21,hit,S(Mathf.Lerp(2.33f,7.15f,Ease(Progress(0f,.24f,hitAge)))),
            Pulse(hitAge,0f,.031f,.077f,.27f),angle:-32f*direction,flip:Left);
        layers.Burst(hit,hitAge,36,S(21.8f),S(.28f),Gold,.54f,
            heading:Left?128f:52f,spread:94f,gravity:S(40f));
    }

    private void Dagger(Vector3 hit, float fade)
    {
        float cast = phase-timing.x;
        float u = Progress(timing.x,timing.y,phase);
        Vector3 tip = Vector3.Lerp(source,hit,u);
        Vector3 axis = (hit-source).normalized;
        Vector3 normal = new(-axis.y,axis.x);
        float alpha = Smooth(0f,.027f,cast)*fade*(hitAge < 0f ? 1f : 1f-Smooth(0f,.039f,hitAge));
        float width = S(8.02f), height = width * art.Aspect(25);
        float rotation = Tilt(axis)-46.2f*direction;
        Vector3 pivot = Quaternion.Euler(0f,0f,rotation) * new Vector3(width*.2585f*direction,height*.265f);
        layers.DrawEffect(25,tip-pivot,width,height,new Vector4(13f,1f,cast,0f),Tint(Color.white,alpha),rotation,flip:Left);
        for (int i = 0; i < 12; i++)
        {
            float drop = Mathf.Repeat(cast*7.8f+Seed(i+31),1f);
            float reach = S(u*4.74f*drop);
            Vector3 bead = tip-axis*(S(2.0f+1.4f*Seed(i+13))+reach)+normal*(Seed(i)-.5f)*S(.38f+drop*2.1f);
            layers.Particle(bead,S(Mathf.Lerp(.26f,.09f,drop)),Venom,alpha*Mathf.Sin(drop*Mathf.PI)*.9f);
        }
        BurstArt(layers,26,hit,hitAge,S(4.8f),Venom,.40f);
    }

    private void BloodStorm(Vector3 hit, float fade)
    {
        float a = phase-timing.y;
        for (int i = 0; i < 4; i++)
        {
            float slash = a+.41f-i*.094f;
            Vector3 center = hit + new Vector3(S((i-1.5f)*.73f*direction),S(.42f-i*.31f));
            float angle = (i % 2 == 0 ? 50f-i*12f : -25f-i*7f) * direction;
            layers.Draw(27,center,S(7.15f+i*.57f),Pulse(slash,0f,.037f,.10f,.22f)*fade,phase,
                angle:angle,reveal:Smooth(0f,.105f,slash),warp:.02f,flip:Left);
        }
        float finish = Smooth(-.035f,.13f,a);
        layers.Draw(28,hit+Vector3.right*S(Mathf.Lerp(-2.15f,1.69f,finish)*direction),S(Mathf.Lerp(8.3f,12.75f,finish)),
            Pulse(a,-.042f,.04f,.13f,.39f)*fade,phase,height:S(Mathf.Lerp(5.54f,7.89f,finish)),
            angle:Mathf.Lerp(25f,-12f,finish)*direction,reveal:finish,flip:Left);
        Flash(layers,6,hit,hitAge,S(3.35f),Crimson,.21f);
        layers.Burst(hit,hitAge,28,S(18.4f),S(.33f),Crimson,.46f,heading:Left?134f:46f);
    }

    private void Piston(Vector3 feet, bool landed, float fade)
    {
        float cast = phase-timing.x;
        float strike = phase-timing.y;
        Vector3 impact = feet;
        Vector3 muzzle = source + new Vector3(-S(2.6f)*direction,S(3.05f));
        float launch = Progress(0f,.32f,cast);
        Vector3 Launch(float t) => muzzle + new Vector3(direction*S(2.53f*t+2.9f*t*t),
            S(12.4f*(.55f*t+.45f*t*t)));
        float charge = Pulse(cast,-.23f,-.10f,-.012f,.052f)*fade;
        layers.Draw(33,muzzle,S(Mathf.Lerp(4.35f,2.62f,Smooth(-.24f,0f,cast))),charge,phase,angle:cast*344f);
        layers.Ring(muzzle,cast,.19f,S(6.0f),.43f,Cyan,ground:false);
        float ascent = Pulse(cast,0f,.040f,.22f,.40f)*fade;
        Vector3 head = Launch(launch);
        Streak(layers,Launch(Mathf.Max(0f,launch-.69f)),head,S(3.22f),ascent,cast,Cyan);
        layers.Draw(33,head,S(4f),ascent,phase,height:S(3f),angle:cast*170f,warp:.028f);

        float fall = Progress(-.28f,0f,strike);
        Vector3 Fall(float t) => impact+Vector3.up*S(19.1f*(1f-(.28f*t+.72f*t*t)));
        Vector3 piston = Fall(fall);
        float falling = Pulse(strike,-.28f,-.18f,-.011f,.065f)*fade;
        if (fall >= 1f && !landed) falling = 0f;
        Streak(layers,Fall(Mathf.Max(0f,fall-.59f)),piston,S(3.62f),falling*.82f,strike,Cyan);
        float uniform = S(Mathf.Lerp(.90f,1.08f,fall))*(1f-Pulse(hitAge,0f,.038f,.075f,.21f)*.055f);
        float width = uniform * 12.7f, height = uniform * 11.15f;
        Vector3 offset = new(-width/330f,height*(235f/290f-.5f));
        float burst = Ease(Progress(0f,.21f,hitAge));
        layers.Draw(31,impact+Vector3.down*S(.42f),S(Mathf.Lerp(4f,11.8f,burst)),
            Pulse(hitAge,0f,.045f,.14f,.48f),height:S(Mathf.Lerp(1.5f,3.92f,burst)),ground:true);
        layers.DrawEffect(30,impact+offset,width,height,new Vector4(12f,1f,hitAge,0f),
            Tint(Color.white,Pulse(hitAge,0f,.027f,.095f,.33f)));
        layers.DrawEffect(30,piston+offset,width,height,new Vector4(11f,1f,strike,0f),
            Tint(Color.white,Pulse(strike,-.28f,-.20f,.080f,.30f)*fade*(fall>=1f&&!landed?0f:1f)));
        layers.Ring(impact,hitAge,.45f,S(14.75f),.23f,Gold);
        layers.Ring(impact,hitAge-.077f,.44f,S(18.6f),.19f,Cyan);
        Dust(layers,impact,hitAge-.034f,S(7.0f),direction);
        layers.Burst(impact,hitAge,31,S(17.6f),S(.34f),Gold,.58f,spread:150f);
    }

    private void Laser(Vector3 hit, Vector3 feet, bool landed, float fade)
    {
        float strike = phase-timing.y;
        float wind = Smooth(-.35f,-.012f,strike);
        float charge = Pulse(strike,-.445f,-.31f,-.015f,.096f)*fade;
        layers.Draw(33,source,S(Mathf.Lerp(5.46f,3f,wind)),charge,phase,angle:-wind*83f);
        layers.Orbit(source,-phase*2f,S(Mathf.Lerp(4.75f,.48f,wind)),S(Mathf.Lerp(2.8f,.48f,wind)),Cyan,charge*.75f,21);

        float extend = Ease(Progress(0f,.18f,strike));
        float beam = Pulse(strike,0f,.035f,.48f,.70f)*fade;
        float width = S(Mathf.Lerp(1.73f,4.78f,extend)) * (1f-Smooth(.46f,.69f,strike));
        Vector3 end = Vector3.Lerp(source,hit+Vector3.right*S(8.6f*direction),extend);
        Vector3 delta = end-source;
        layers.DrawEffect(32,(source+end)*.5f,delta.magnitude,width,new Vector4(15f,1f,strike,0f),
            Tint(Cyan,beam),Angle(delta));

        if (!landed) return;
        Vector3 start = new(source.x+S(4.35f*direction),feet.y,feet.z);
        Vector3 floorEnd = feet+Vector3.right*S(8.15f*direction);
        for (int i = 0; i < 5; i++)
        {
            Vector3 ground = Vector3.Lerp(start,floorEnd,i*.25f);
            float t = hitAge-.064f-i*.052f;
            float spread = Ease(Progress(0f,.25f,t));
            float height = S(Mathf.Lerp(1.38f,4.69f,spread));
            layers.Draw(34,ground+Vector3.up*height*.5f,S(Mathf.Lerp(2.5f,8.1f,spread)),
                Pulse(t,0f,.038f,.11f,.49f),t,height:height,warp:.046f);
            layers.Ring(ground,t,.47f,S(9.84f),.22f,Cyan);
            layers.Burst(ground,t,17,S(15f),S(.29f),i%2==0?Cyan:Gold,.51f);
        }
    }

    internal void Clear()
    {
        caster = null;
        target = null;
        layers.BeginFrame();
        layers.EndFrame(false);
    }

    internal void Dispose() => layers.Dispose();
}

internal static class EnemyMetalFx
{
    internal static readonly Color Silver = new(.86f,.94f,1f);
    internal static readonly Color Cyan = new(.49f,.91f,1f);
    internal static readonly Color Gold = new(1f,.79f,.43f);
    internal static readonly Color Violet = new(.72f,.61f,.94f);
    internal static readonly Color Crimson = new(1f,.39f,.42f);
    internal static readonly Color Venom = new(.67f,.98f,.32f);
    internal static float Progress(float from, float to, float value) => Mathf.InverseLerp(from,to,value);
    internal static float Smooth(float from, float to, float value) => Mathf.SmoothStep(0f,1f,Progress(from,to,value));
    internal static float Pulse(float value, float start, float peak, float hold, float end) =>
        Smooth(start,peak,value)*(1f-Smooth(hold,end,value));
    internal static float Ease(float value) => 1f-(1f-value)*(1f-value)*(1f-value);
    internal static Color Tint(Color color, float alpha) => new(color.r,color.g,color.b,alpha);
    internal static float Tilt(Vector3 direction) =>
        Mathf.Atan2(direction.y,Mathf.Abs(direction.x))*Mathf.Rad2Deg*(direction.x<0f?-1f:1f);
    internal static Vector3 Feet(Bounds bounds) =>
        new(bounds.center.x,bounds.min.y+bounds.size.y*.069f,bounds.center.z);

    internal static void Streak(EnemyAtlasVfxLayers layers, Vector3 from, Vector3 to,
        float thickness, float opacity, float time, Color tint)
    {
        Vector3 delta = to-from;
        layers.DrawEffect(0,(from+to)*.5f,delta.magnitude,thickness,new Vector4(14f,1f,time,0f),
            Tint(tint,opacity),Angle(delta));
    }

    internal static void Dust(EnemyAtlasVfxLayers layers, Vector3 ground, float age, float width, float facing)
    {
        if (age <= 0f || age > .53f) return;
        for (int i = 0; i < 6; i++)
        {
            float time = age-i*.015f;
            float u = Progress(0f,.42f,time);
            float size = width*Mathf.Lerp(.21f,.53f,u);
            Vector3 offset = new(facing*width*(i*.038f+Seed(i+20)*.37f*u), size*.33f+width*(.044f+i*.014f)*u);
            layers.Draw(1,ground+offset,size,Pulse(time,0f,.032f,.10f,.40f)*(.33f+.17f*Seed(i)),
                angle:facing*(Seed(i)-.5f)*11.5f,ground:true,flip:facing<0f);
        }
    }

    internal static void Flash(EnemyAtlasVfxLayers layers, int cell, Vector3 point, float age, float size, Color tint, float end)
    {
        if (age < 0f || age >= end) return;
        float growth = Ease(Progress(0f,.075f,age));
        layers.Draw(cell,point,size*Mathf.Lerp(.31f,1f,growth)*(1f-.3f*Smooth(.095f,end,age)),
            Pulse(age,0f,.024f,.077f,end),angle:age*39f);
        layers.Burst(point,age,10,size*2.7f,size*.094f,tint,end+.07f,gravity:24f);
    }

    internal static void BurstArt(EnemyAtlasVfxLayers layers, int cell, Vector3 point, float age, float size, Color tint, float end)
    {
        if (age < 0f || age >= end) return;
        layers.Draw(cell,point,size*Mathf.Lerp(.31f,1f,Ease(Progress(0f,end*.73f,age))),
            Pulse(age,0f,.045f,.15f,end),age,angle:age*37f,warp:.032f);
        layers.Burst(point,age,22,size*2.2f,size*.053f,tint,end+.08f,gravity:27f);
    }
}
