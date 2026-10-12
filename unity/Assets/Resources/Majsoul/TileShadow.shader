Shader "YunJian/TileShadow"
{
    Properties
    {
        _MainTex ("Original effect_shadow texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Opacity ("Fade", Range(0,1)) = 1
    }
    SubShader
    {
        // mjp_new/effect_shadow material: renderQueue 3002, SrcAlpha/OneMinusSrcAlpha, ZWrite Off.
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;fixed4 _Tint;float _Opacity;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            // Catfood/Additive Transparent fragment program: texture * _Tint (albedo/alpha scale 1).
            fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv)*_Tint;c.a*=_Opacity;return c; }
            ENDCG
        }
    }
}
