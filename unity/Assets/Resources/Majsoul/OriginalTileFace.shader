Shader "YunJian/OriginalTileFace"
{
    Properties { _MainTex ("Original table face", 2D) = "white" {} _Match ("Same visible tile", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;float4 _MainTex_ST;float _Match;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // ViewPai.OnChoosedPai / setMeshColor uses this tint on every material.
                c.rgb*=lerp(fixed3(1,1,1),fixed3(.615,.827,.976),_Match);
                return c;
            }
            ENDCG
        }
    }
}
