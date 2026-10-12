Shader "YunJian/OriginalTileBody"
{
    Properties
    {
        _MainTex ("Original back atlas", 2D) = "white" {}
        _TileTex ("Original face atlas (body column)", 2D) = "white" {}
        _Match ("Same visible tile", Range(0,1)) = 0
        _SplitThreshold ("Split Threshold", Range(-1,1)) = .08
        _ColorLight ("Color Light", Color) = (1,1,1,1)
        _ColorUnlight ("Color Unlight", Color) = (.8113208,.8113208,.8113208,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 tile:TEXCOORD2; float3 normal:TEXCOORD1; };
            sampler2D _MainTex,_TileTex;float4 _TileTex_ST;float _Match,_SplitThreshold;fixed4 _ColorLight,_ColorUnlight;
            float4 _MjLightDir;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.tile=TRANSFORM_TEX(v.uv,_TileTex); o.normal=UnityObjectToWorldNormal(v.normal); return o; }
            fixed4 frag(v2f i):SV_Target
            {
                // Catfood/Cartoon_nonoutline_tile_back from the APK: the back skin's alpha
                // selects between its colour and the face atlas' body column (mjp_mid).
                fixed4 back=tex2D(_MainTex,i.uv);
                fixed3 albedo=tex2D(_TileTex,i.tile).rgb*(1-back.a)+back.rgb*back.a;
                float d=dot(normalize(_MjLightDir.xyz),normalize(i.normal));
                float t=(d-_SplitThreshold)*10;
                fixed3 band=(_ColorLight.rgb*t+_ColorUnlight.rgb*(1-t))*albedo;
                fixed3 c=_SplitThreshold<d?band:albedo*_ColorUnlight.rgb;
                fixed3 lit=_SplitThreshold+.1<d?albedo*_ColorLight.rgb:c;
                // Same whole-model blue as OriginalTileFace, so the shell rim matches the face.
                return fixed4(lit*lerp(fixed3(1,1,1),fixed3(.615,.827,.976),_Match),1);
            }
            ENDCG
        }
    }
}
