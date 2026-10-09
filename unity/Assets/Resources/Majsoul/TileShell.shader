Shader "YunJian/TileShell"
{
    Properties { _Color ("Ivory", Color) = (.9,.89,.86,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.normal=v.normal; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                // Stable soft lighting for the camera-facing hand; bevels remain visible.
                float light=.69+.31*saturate(n.y)-.09*n.x+.04*n.z;
                return fixed4(_Color.rgb*light,1);
            }
            ENDCG
        }
    }
}
