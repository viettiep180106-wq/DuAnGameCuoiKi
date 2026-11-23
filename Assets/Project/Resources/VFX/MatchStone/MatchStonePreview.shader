Shader "Game/MatchStone/Preview"
{
    Properties
    {
        _Element ("Element", Float) = 0
        _Kind ("0 Node, 1 Link, 2 Tile, 3 Stone, 4 Board", Float) = 0
        _Age ("Age", Float) = -1
        _Seed ("Seed", Float) = 0
        _Strength ("Strength", Float) = 1
        _MainTex ("Stone", 2D) = "white" {}
        _SpriteRect ("Stone UV", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SpriteRect;
            float _Element, _Kind, _Age, _Seed, _Strength;
            float _MatchStoneClock;
            #define PI 3.14159265
            #define TAU 6.28318530

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 timing : TEXCOORD1;
                float alpha : TEXCOORD2;
            };
            Varyings Vert(float4 vertex : POSITION, float2 uv : TEXCOORD0,
                float4 timing : TEXCOORD1, float4 color : COLOR)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(vertex);
                output.uv = uv;
                output.timing = timing;
                output.alpha = color.a;
                return output;
            }

            float Hash(float x) { return frac(sin(x * 127.13 + 14.17) * 43758.5453); }
            float Pulse(float t,float a,float b,float c,float d)
            {
                return smoothstep(a,b,t) * (1.0 - smoothstep(c,d,t));
            }
            float Mask(float sd,float feather)
            {
                return 1.0-smoothstep(-feather,feather,sd);
            }
            float Segment(float2 p,float2 a,float2 b)
            {
                float2 delta=b-a;
                return length(p-a-delta*saturate(dot(p-a,delta)/max(dot(delta,delta),0.00001)));
            }
            float Ring(float r,float radius,float thickness)
            {
                return Mask(abs(r-radius)-thickness,0.009);
            }
            float2 Rotate(float2 p,float angle)
            {
                float c=cos(angle), s=sin(angle);
                return float2(c*p.x-s*p.y,s*p.x+c*p.y);
            }
            float RoundedBox(float2 p,float2 size,float radius)
            {
                float2 q=abs(p)-size+radius;
                return length(max(q,0.0))+min(max(q.x,q.y),0.0)-radius;
            }
            void Over(inout float4 dst,float3 color,float opacity)
            {
                float alpha=saturate(opacity);
                dst.rgb=color*alpha+dst.rgb*(1.0-alpha);
                dst.a=alpha+dst.a*(1.0-alpha);
            }

            void Palette(out float3 color,out float3 hot,out float3 dark)
            {
                if(_Element<0.5) { color=float3(1.0,.57,.12); hot=float3(1,.95,.59); dark=float3(.27,.12,.025); }
                else if(_Element<1.5) { color=float3(.16,.72,.34); hot=float3(.59,1,.66); dark=float3(.035,.25,.15); }
                else if(_Element<2.5) { color=float3(.23,.72,1); hot=float3(.74,.97,1); dark=float3(.035,.23,.39); }
                else if(_Element<3.5) { color=float3(1,.235,.05); hot=float3(1,.87,.39); dark=float3(.29,.088,.071); }
                else { color=float3(.69,.39,.17); hot=float3(1,.73,.36); dark=float3(.24,.12,.053); }
            }

            float Leaf(float2 p, float size)
            {
                float2 q=abs(p)/float2(size,size*.44);
                float lens=q.y+q.x*q.x+q.x*.09;
                return Mask(lens-1.0,0.085);
            }

            float Hex(float2 p)
            {
                p=abs(p);
                return max(p.x, dot(p,float2(.50,.8660254)));
            }

            float Crack(float2 p,float angle,float width,float reach,float start,float seed)
            {
                float2 q=Rotate(p,-angle);
                float2 bend=float2(start+reach*.46, (Hash(seed)-.5)*reach*.29);
                float trunk=Segment(q,float2(start,0),bend);
                trunk=min(trunk,Segment(q,bend,float2(start+reach,reach*.07)));
                if(q.x>start+reach*.42)
                    trunk=min(trunk,Segment(q,bend,float2(start+reach*.82,-reach*.25)));
                return Mask(trunk-width,.006);
            }

            float4 Tile(float2 p)
            {
                float sd=RoundedBox(p,float2(.98,.98),.15);
                float edge=Mask(sd,.018);
                float inset=Mask(sd+.032,.016);
                float3 base=lerp(float3(.047,.066,.095),float3(.095,.13,.17),saturate(p.y*.5+.5));
                base+=_Seed*.018;
                return float4(lerp(float3(.19,.245,.31),base,inset)*edge,edge);
            }

            float4 Board(float2 p)
            {
                float sd=RoundedBox(p,float2(.99,.99),.08);
                float edge=Mask(sd,.012);
                float border=1.0-Mask(sd+.012,.006);
                float3 tint=lerp(float3(.022,.032,.051),float3(.056,.085,.119),saturate(.96-length(p*float2(.75,.92))));
                tint+=float3(.13,.19,.25)*border*.73;
                return float4(tint*edge,edge);
            }

            float4 Node(float2 p, float t, float seed, float strength)
            {
                if(t<0 || t>.54) return 0;
                float3 color,hot,dark;
                Palette(color,hot,dark);
                float r=length(p), theta=atan2(p.y,p.x);
                float ready=Pulse(t,0,.049,.113,.204);
                float blast=Pulse(t,.150,.183,.231,.355);
                float wave=Pulse(t,.159,.207,.232,.365);
                float travel=smoothstep(0,.19,t);
                float blastSize=lerp(.19,.74,smoothstep(.15,.29,t));
                float4 dst=0;
                Over(dst,color,exp(-r*r*7.1)*(.20*ready+.44*blast));
                Over(dst,color,exp(-abs(r-blastSize)*21.0)*wave*.44);

                if(_Element<0.5)
                {
                    float rim=.44+sin(theta*10+t*19)*.010;
                    Over(dst,dark,Ring(r,rim,.035)*ready*.91);
                    Over(dst,hot,Ring(r,rim,.013)*ready);
                    Over(dst,color,Ring(r,.512,.008)*ready*.64);
                    float a=theta+t*.84;
                    float tips=.14+.49*pow(abs(cos(a*2.0)),13.0)+.17*pow(abs(sin(a*4.0)),12.0);
                    Over(dst,color,Mask(r-tips*(.78+.34*travel),.019)*blast);
                    Over(dst,hot,Mask(r-tips*.77,.018)*blast);
                }
                else if(_Element<1.5)
                {
                    float unfold=smoothstep(.002,.092,t);
                    float vine=Ring(r+sin(theta*5+t*22)*.031,.455,.018);
                    float upper=Ring(r+sin(theta*5+t*22+PI)*.031,.455,.012);
                    float arc=1.0-smoothstep(unfold,unfold+.07,frac(theta/TAU+seed*.29-t*.45));
                    Over(dst,dark,vine*arc*ready);
                    Over(dst,hot,upper*arc*ready*.77);
                    for(int i=0;i<6;i++)
                    {
                        float a=i*TAU/6.0+t*3.4;
                        float2 leaf=Rotate(p,-a)-float2(.46+.045*sin(t*20+i),.065);
                        Over(dst,i%2==0?color:hot,Leaf(Rotate(leaf,-.64),.113)*arc*ready);
                        float2 petal=Rotate(p,-a)-float2(blastSize*.46,0.0);
                        Over(dst,color,Leaf(petal,blastSize*.50)*blast);
                        Over(dst,hot,Leaf(petal+float2(.031,-.014),blastSize*.36)*blast*.72);
                    }
                }
                else if(_Element<2.5)
                {
                    float hex=Hex(Rotate(p,-PI/6.0));
                    float frost=Ring(hex,.44,.016)+Ring(hex,.478,.008)*.52;
                    Over(dst,color,frost*ready*.92);
                    Over(dst,hot,Ring(hex,.453,.008)*ready);
                    float arm=abs(cos(theta*3.0));
                    float crystal=.15+.62*pow(arm,8.0);
                    Over(dst,color,Mask(r-crystal*blastSize*1.57,.012)*blast);
                    Over(dst,hot,Mask(r-crystal*blastSize*1.06,.009)*blast);
                    for(int i=0;i<3;i++){
                        float2 axis=float2(cos(i*PI/3.0),sin(i*PI/3.0));
                        Over(dst,hot,Mask(Segment(p,-axis*blastSize,axis*blastSize)-.006,.005)*blast*.80);
                    }
                }
                else if(_Element<3.5)
                {
                    float curl=sin(theta*5-r*21-t*39)*.069+sin(theta*9+t*19)*.028;
                    float ember=Ring(r+curl,.455,.031);
                    Over(dst,color,ember*ready);
                    Over(dst,hot,Ring(r+curl*.76,.433,.014)*ready*.83);
                    float spikes=.21+.52*pow(abs(sin(theta*4.5+sin(theta*2.0)*1.1-r*5.1)),7.0);
                    Over(dst,color,Mask(r-spikes*blastSize*1.67,.019)*blast);
                    Over(dst,hot,Mask(r-spikes*blastSize*1.09,.019)*blast);
                }
                else
                {
                    float fracture=0;
                    for(int i=0;i<7;i++)
                    {
                        float a=(i+Hash(i+seed*19)*.54)*TAU/7.0;
                        fracture=max(fracture,Crack(p,a,.010,.27+Hash(i+9)*.19,.27,seed*9+i));
                    }
                    float fault=Ring(r+sin(theta*10.0)*.019,.30,.011);
                    Over(dst,dark,(fracture+fault)*(ready+blast*.65));
                    Over(dst,hot,(fracture+fault)*Pulse(t,.045,.095,.14,.26)*.76);
                    float bulge=blastSize*.83;
                    Over(dst,dark,Mask(r-bulge*(1+.075*sin(theta*9.0)),.02)*blast*.83);
                    Over(dst,color,Mask(r-bulge*.72,.042)*blast*.73);
                }

                for(int i=0;i<15;i++)
                {
                    float group=i+seed*41.47;
                    float h=Hash(group), k=Hash(group+24.6);
                    float local=t-.162-h*.036;
                    float age=max(0.0,local);
                    float life=lerp(.19,.34,k);
                    float alpha=Pulse(local,0,.038,life*.49,life);
                    float angle=(i+h*.82)*TAU/15.0;
                    float2 course=float2(cos(angle),sin(angle));
                    bool earth=_Element>3.5;
                    float speed=lerp(1.15,3.0,h)*(earth?1.40:1.0);
                    float2 center=course*((earth?.17:.11)+speed*age) - float2(0,age*age*1.47);
                    float2 q=Rotate(p-center,-angle-age*(3.0+h*6.0));
                    float size=lerp(.021,.071,k)*(1.0-smoothstep(.053,life,age)*.55)*(earth?1.49:1.0);
                    bool smoke=_Element>2.5 && _Element<3.5 && i%3==0;
                    bool dust=earth && i%4==0;
                    if(smoke || dust)
                    {
                        float plume=Mask(length(q)-size*(1.1+age*8.1),.042);
                        Over(dst,smoke?float3(.23,.171,.146):float3(.54,.36,.22),plume*alpha*.48);
                    }
                    else
                    {
                        float shape=abs(q.x)+abs(q.y)*((_Element<2.5&&_Element>1.5)?2.25:1.33);
                        if(earth) shape=Hex(q*float2(.93,1.19))*1.28;
                        float body=_Element>=.5&&_Element<1.5 ? Leaf(q,size*1.25) : Mask(shape-size,.006);
                        Over(dst,dark,Mask(shape-size*1.14,.009)*alpha*.78);
                        Over(dst,lerp(color,hot,h),body*alpha);
                        if(earth)
                            Over(dst,dark,body*alpha*step(q.x*.45,q.y)*.48);
                        Over(dst,hot,Mask(length(q+float2(size*.24,0))-size*.24,.006)*alpha*.64);
                    }
                }
                return dst*strength;
            }

            float Lightning(float x,float t,float seed)
            {
                float steps=x*12.0;
                float i=floor(steps);
                float state=floor(t*31.0)+seed;
                return (lerp(Hash(i+state),Hash(i+1.0+state),frac(steps))-.5)*.79;
            }

            float4 Link(float2 p,float2 uv, float t,float seed,float strength)
            {
                if(t<.018 || t>.39) return 0;
                float x=uv.x;
                float fill=smoothstep(.020,.133,t);
                float head=1.0-smoothstep(fill-.064,fill+.035,x);
                float taper=smoothstep(0,.105,x)*(1.0-smoothstep(.89,1,x));
                float alpha=Pulse(t,.018,.063,.179,.325)*head;
                float3 color,hot,dark;
                Palette(color,hot,dark);
                float4 dst=0;
                float y=p.y;
                float y1=sin(x*TAU-t*23.0), y2=sin(x*TAU-t*23.0+PI);
                if(_Element<.5)
                {
                    y1=Lightning(x,t,seed*13.6)*taper*(.33+.67*smoothstep(.055,.13,t));
                    y2=Lightning(x,t,seed*13.6+10.3)*taper*.76;
                    float d=abs(y-y1);
                    Over(dst,color,exp(-d*8.7)*alpha*.70);
                    Over(dst,hot,Mask(d-.044,.029)*alpha);
                    Over(dst,color,Mask(abs(y-y2)-.012,.023)*alpha*.77);
                }
                else if(_Element<1.5)
                {
                    y1*=.292*taper; y2*=.292*taper;
                    Over(dst,dark,Mask(abs(y-y1)-.13,.027)*alpha);
                    Over(dst,color,Mask(abs(y-y2)-.096,.029)*alpha);
                    Over(dst,hot,Mask(abs(y-y1+.021)-.054,.027)*alpha*.87);
                    for(int i=0;i<4;i++)
                    {
                        float v=.14+i*.232;
                        float bend=sin(v*TAU-t*23.0)*.24;
                        float2 leaf=Rotate(float2((x-v)*2.8,y-bend-(i%2==0?.23:-.23)),i%2==0?.43:-.43);
                        Over(dst,color,Leaf(leaf,.20)*alpha*.92);
                    }
                }
                else if(_Element<2.5)
                {
                    y1*=.30*taper;
                    float d=abs(y-y1);
                    Over(dst,dark,Mask(d-.25,.067)*alpha);
                    Over(dst,color,Mask(d-.18,.06)*alpha);
                    Over(dst,hot,Mask(abs(y-y1-.049)-.056,.045)*alpha*.91);
                    float frost=.55+.45*sin(x*83.0-t*48.0);
                    Over(dst,hot,Mask(d-.28,.03)*frost*alpha*.26);
                }
                else if(_Element<3.5)
                {
                    y1*=.21*taper;
                    float flame=pow(.5+.5*sin(x*39.0-t*37.0+sin(x*19-t*23)*1.1),3.0);
                    float plume=(.19+flame*.30)*max(.27,taper);
                    float sd=abs(y-y1-flame*.105)-plume;
                    Over(dst,color,exp(-max(sd,0.0)*10.0)*alpha*.60);
                    Over(dst,color,Mask(sd,.036)*alpha);
                    Over(dst,hot,Mask(abs(y-y1)-plume*.45,.037)*alpha*.92);
                }
                else
                {
                    y1*=.10;
                    float seam=Lightning(x,t*.04,seed*21)*.40*taper;
                    float rise=smoothstep(.064,.138,t);
                    float stones=rise*.33*pow(.5+.5*sin(x*31+seed*9),2.5);
                    Over(dst,dark,Mask(abs(y-seam)-.075,.022)*alpha);
                    Over(dst,color,Mask(abs(y-seam-.044)-.026,.026)*alpha*.91);
                    Over(dst,color,Mask(abs(y-y1-stones*.42)-stones,.033)*alpha*.89);
                    Over(dst,hot,Mask(abs(y-y1-stones*.84)-.021,.032)*alpha*rise*.80);
                }

                for(int i=0;i<9;i++)
                {
                    float h=Hash(i+seed*16.7), k=Hash(i+8.4);
                    float u=frac(h+t*(1.03+k*.39));
                    float side=lerp(-.77,.77,k)*smoothstep(.07,.25,t);
                    float shape=abs(x-u)*4.4+abs(y-side);
                    float spark=Mask(shape-lerp(.023,.076,k),.014);
                    Over(dst,lerp(color,hot,h),spark*head*Pulse(t,.057,.105,.207,.388)*sin(u*PI));
                }
                return dst*strength;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 p=input.uv*2.0-1.0;
                if(_Kind>3.5) return Board(p);
                if(_Kind>2.5)
                {
                    float4 color=tex2D(_MainTex,_SpriteRect.xy+input.uv*_SpriteRect.zw);
                    float alpha=color.a*_Strength;
                    return float4(color.rgb*alpha,alpha);
                }
                if(_Kind>1.5) return Tile(p);
                bool runtime=input.timing.w>1.5;
                float age=runtime ? _MatchStoneClock-input.timing.x : _Age;
                float seed=runtime ? input.timing.y : _Seed;
                float strength=runtime ? input.timing.z*input.alpha : _Strength;
                if(_Kind>.5) return Link(p,input.uv,age,seed,strength);
                return Node(p,age,seed,strength);
            }
            ENDCG
        }
    }
}
