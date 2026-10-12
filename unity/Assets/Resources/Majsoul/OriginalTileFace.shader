Shader "YunJian/OriginalTileFace"
{
    Properties
    {
        _MainTex ("Original table face", 2D) = "white" {}
        _Match ("Same visible tile", Range(0,1)) = 0
        _SplitThreshold ("Split Threshold", Range(-1,1)) = .08
        _ColorLight ("Color Light", Color) = (1,1,1,1)
        _ColorUnlight ("Color Unlight", Color) = (.8113208,.8113208,.8113208,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; };
            sampler2D _MainTex;float4 _MainTex_ST;float _Match,_SplitThreshold;fixed4 _ColorLight,_ColorUnlight;
            // Toward-light direction of scene_mj/light, set by TableRenderer (TableLook/lighting.json).
            float4 _MjLightDir;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed3 albedo=tex2D(_MainTex,i.uv).rgb;
                // Catfood/Cartoon_nonoutline light/band/shade terms from the APK (GLES3).
                float d=dot(normalize(_MjLightDir.xyz),normalize(i.normal));
                float t=(d-_SplitThreshold)*10;
                fixed3 band=(_ColorLight.rgb*t+_ColorUnlight.rgb*(1-t))*albedo;
                fixed3 c=_SplitThreshold<d?band:albedo*_ColorUnlight.rgb;
                fixed3 lit=_SplitThreshold+.1<d?albedo*_ColorLight.rgb:c;
                // ViewPai.OnChoosedPai / setMeshColor: one blue tint over the whole model,
                // identical on face, shell and outline so lit and shaded parts match.
                return fixed4(lit*lerp(fixed3(1,1,1),fixed3(.615,.827,.976),_Match),1);
            }
            ENDCG
        }
    }
}
