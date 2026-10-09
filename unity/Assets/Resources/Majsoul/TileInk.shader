Shader "YunJian/TileInk"
{
    Properties { _MainTex ("Tile artwork", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_ST;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float lo=min(c.r,min(c.g,c.b)), hi=max(c.r,max(c.g,c.b));
                // The atlas includes neutral paper. Draw only its ink onto the 3D shell,
                // preserving the glyph aspect without leaving a gray rectangular patch.
                c.a *= 1-smoothstep(.55,.76,lo)*(1-smoothstep(.014,.05,hi-lo));
                return c;
            }
            ENDCG
        }
    }
}
