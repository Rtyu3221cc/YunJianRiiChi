Shader "YunJian/OriginalTileBody"
{
    Properties { _MainTex ("Original back atlas", 2D) = "white" {} _Depth ("Mesh depth", Float) = .649 }
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
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float height:TEXCOORD2; };
            sampler2D _MainTex; float _Depth;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.normal=UnityObjectToWorldNormal(v.normal); o.height=v.vertex.y/_Depth; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float shade=.81+.19*saturate(dot(normalize(i.normal),normalize(float3(-.3,1,-.4))));
                fixed3 paper=fixed3(.875,.875,.865);
                fixed3 color=i.height<.17?tex2D(_MainTex,i.uv).rgb:paper;
                return fixed4(color*shade,1);
            }
            ENDCG
        }
    }
}
