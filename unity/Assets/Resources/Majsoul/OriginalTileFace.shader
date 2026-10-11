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
                // Reference white face (223,224,222) -> blue face (137,186,215).
                // Applied only to the face submesh, leaving the rim/back untouched.
                c.rgb*=lerp(fixed3(1,1,1),fixed3(.614,.830,.968),_Match);
                return c;
            }
            ENDCG
        }
    }
}
