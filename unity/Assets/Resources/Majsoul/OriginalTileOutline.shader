Shader "YunJian/OriginalTileOutline"
{
    Properties { _Color ("Original outline color", Color) = (.283,.283,.283,1) _Match ("Same visible tile", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;float _Match;
            float4 vert(float4 vertex:POSITION):SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag():SV_Target { return fixed4(_Color.rgb*lerp(fixed3(1,1,1),fixed3(.615,.827,.976),_Match),_Color.a); }
            ENDCG
        }
    }
}
