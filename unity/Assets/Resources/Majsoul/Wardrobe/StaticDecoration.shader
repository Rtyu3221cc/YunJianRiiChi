Shader "YunJian/StaticDecoration" {
 Properties {
  _MainTex ("Original texture",2D)="white" {}
  _Color ("Original color",Color)=(1,1,1,1)
  _Tint ("Original tint",Color)=(1,1,1,1)
  _AlbedoIntensity ("Albedo intensity",Float)=1
  _AlphaScale ("Alpha scale",Float)=1
  _AlphaTest ("Alpha test",Float)=0
  _Cutoff ("Cutoff",Float)=.5
  _Cartoon ("Cartoon lighting",Float)=0
  _ColorLight ("Light color",Color)=(1,1,1,1)
  _ColorUnlight ("Shade color",Color)=(1,1,1,1)
  _SplitThreshold ("Shade threshold",Float)=.4
  _SrcBlend ("Source blend",Float)=1
  _DstBlend ("Destination blend",Float)=0
  _ZTest ("Depth test",Float)=4
  _SourceZWrite ("Depth write",Float)=1
  _SourceCull ("Cull",Float)=2
 }
 SubShader {
  Pass {
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_SourceZWrite]
   ZTest [_ZTest]
   Cull [_SourceCull]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST,_Color,_Tint,_ColorLight,_ColorUnlight;
   float _AlbedoIntensity,_AlphaScale,_AlphaTest,_Cutoff,_Cartoon,_SplitThreshold;
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   float4 frag(v2f i):SV_Target {
    float4 c=tex2D(_MainTex,i.uv)*_Color*_Tint;c.rgb*=_AlbedoIntensity;c.a*=_AlphaScale;
    if(_AlphaTest>.5)clip(c.a-_Cutoff);
    if(_Cartoon>.5)c.rgb*=lerp(_ColorUnlight.rgb,_ColorLight.rgb,step(_SplitThreshold,dot(normalize(i.normal),normalize(float3(.2,.8,-.4)))));
    return c;
   }
   ENDCG
  }
 }
}
