Shader "YunJian/WinParticle"
{
    Properties
    {
        _MainTex ("Original winning tile particle", 2D) = "white" {}
        _DstBlend ("Destination blend", Float) = 1
        _SrcBlend ("Source blend", Float) = 5
        _ZTest ("Depth test", Float) = 4
        _TintColor ("Original tint", Color) = (.5,.5,.5,.913)
        _CalloutStyle ("Callout intensity and face protection", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_ST;
            fixed4 _TintColor;
            float _SrcBlend;
            float4 _CalloutStyle,_CalloutFaceCenter,_CalloutFaceRight,_CalloutFaceForward,_CalloutFaceNormal;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float3 world:TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.color=v.color; o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            // Original legacy particle tint convention and SrcAlpha/One blend.
            fixed4 frag(v2f i):SV_Target {
                fixed4 color=2*i.color*_TintColor*tex2D(_MainTex,i.uv);
                if(_CalloutStyle.y>.5){
                    // Intersect the viewing ray with the tile's top plane, so the
                    // protection follows its perspective from all four seats.
                    float3 ray=i.world-_WorldSpaceCameraPos;
                    float denominator=dot(ray,_CalloutFaceNormal.xyz);
                    float face=0;
                    if(abs(denominator)>.00001){
                        float distance=dot(_CalloutFaceCenter.xyz-_WorldSpaceCameraPos,_CalloutFaceNormal.xyz)/denominator;
                        float3 offset=_WorldSpaceCameraPos+ray*distance-_CalloutFaceCenter.xyz;
                        float x=dot(offset,_CalloutFaceRight.xyz)/_CalloutFaceRight.w;
                        float y=dot(offset,_CalloutFaceForward.xyz)/_CalloutFaceForward.w;
                        face=distance>0?1-smoothstep(.65,1.03,max(abs(x),abs(y))):0;
                    }
                    float visibility=_CalloutStyle.x*lerp(1,_CalloutFaceCenter.w,face);
                    if(_SrcBlend<1.5)color*=visibility;
                    else color.a*=visibility;
                }
                return color;
            }
            ENDCG
        }
    }
}
