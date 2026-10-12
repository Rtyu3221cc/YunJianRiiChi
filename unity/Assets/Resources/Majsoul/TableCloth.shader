Shader "YunJian/TableCloth"
{
    Properties { _MainTex ("Baked cloth", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            // The runtime cloth material is not in the APK. This gain is fitted to the
            // 1012-2 reference screenshot (median cloth ratio R .914, G .935, B .955 x 1.1).
            fixed4 frag(v2f i):SV_Target { fixed4 c=tex2D(_MainTex,i.uv); c.rgb*=fixed3(1.005,1.029,1.051); return c; }
            ENDCG
        }
    }
}
