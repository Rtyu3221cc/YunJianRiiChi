Shader "YunJian/WinParticle"
{
    Properties
    {
        _MainTex ("Original winning tile particle", 2D) = "white" {}
        _DstBlend ("Destination blend", Float) = 1
        _SrcBlend ("Source blend", Float) = 5
        _TintColor ("Original tint", Color) = (.5,.5,.5,.913)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_ST;
            fixed4 _TintColor;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color; return o; }
            // Original legacy particle tint convention and SrcAlpha/One blend.
            fixed4 frag(v2f i):SV_Target { return 2 * i.color * _TintColor * tex2D(_MainTex,i.uv); }
            ENDCG
        }
    }
}
