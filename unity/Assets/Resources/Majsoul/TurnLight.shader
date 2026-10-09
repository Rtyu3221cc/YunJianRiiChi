Shader "YunJian/TurnLight"
{
    Properties
    {
        _MainTex ("Original particle_shining", 2D) = "white" {}
        _TintColor ("Original tint", Color) = (.5,.5,.5,.913)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _TintColor;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            // Original legacy particle tint convention and SrcAlpha/One blend.
            fixed4 frag(v2f i):SV_Target { return 2 * i.color * _TintColor * tex2D(_MainTex,i.uv); }
            ENDCG
        }
    }
}
