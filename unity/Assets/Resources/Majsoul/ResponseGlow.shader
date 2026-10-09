Shader "YunJian/ResponseGlow"
{
    Properties { _MainTex ("Original majiangkuang", 2D) = "black" {} _Tint ("Indicator tint", Color) = (1,.89,.12,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Tint;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o; }
            fixed4 frag(v2f i):SV_Target { return tex2D(_MainTex,i.uv)*_Tint*(.88+.12*sin(_Time.y*5)); }
            ENDCG
        }
    }
}
