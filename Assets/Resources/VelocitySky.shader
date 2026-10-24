Shader "Velocity Rivals/Atmosphere"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.015, 0.025, 0.09, 1)
        _Horizon("Horizon", Color) = (0.5, 0.2, 0.3, 1)
        _SunColor("Sun", Color) = (1, 0.8, 0.6, 1)
        _SunDirection("Sun direction", Vector) = (0.2, 0.4, 0.8, 0)
        _Stars("Stars", Float) = 1
        _Clouds("Clouds", Float) = 0.4
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Zenith, _Horizon, _SunColor, _SunDirection;
            float _Stars, _Clouds;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.direction = v.positionOS.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float h = saturate(d.y);
                float3 color = lerp(_Horizon.rgb, _Zenith.rgb, pow(h, .45));
                float sun = saturate(dot(d, normalize(_SunDirection.xyz)));
                color += _SunColor.rgb * (pow(sun, 900) * 4 + pow(sun, 18) * .18);
                float2 plane = d.xz / max(.12, d.y + .15);
                float2 cells = plane * 160;
                float star = step(.997, hash(floor(cells))) * pow(saturate(1-length(frac(cells)-.5)*2), 4);
                color += star * _Stars * smoothstep(.1,.5,h) * 2;
                float2 cloudUV = plane * .7 + float2(_Time.y * .008, 0);
                float cloud = noise(cloudUV)*.55 + noise(cloudUV*2.1)*.3 + noise(cloudUV*4.3)*.15;
                float cover = smoothstep(.5,.78,cloud) * _Clouds * smoothstep(.02,.18,h);
                color = lerp(color, lerp(_Horizon.rgb, float3(.65,.72,.8), .5), cover);
                color = lerp(_Horizon.rgb * .3, color, smoothstep(-.2,.02,d.y));
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
