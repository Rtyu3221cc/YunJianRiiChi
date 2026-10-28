Shader "YunJian/NativeEffectParticle" {
 Properties {
  _MainTex ("Source texture",2D)="white" {}
  _MaskTex ("Source mask",2D)="white" {}
  _TintColor ("Legacy tint",Color)=(.5,.5,.5,.5)
  _MainColor ("Main color",Color)=(1,1,1,1)
  _Color ("Source color",Color)=(1,1,1,1)
  _Alpha ("Alpha",Float)=1
  _MainColorMulti ("Color multiplier",Float)=1
  _MainAnim ("Source UV animation",Vector)=(0,0,0,0)
  _MaskAnim ("Source mask UV animation",Vector)=(0,0,0,0)
  _SourceFamily ("Source material family",Float)=0
  _UseMask ("Has active mask",Float)=0
  _MainReverse ("Source main inversion",Float)=0
  _MaskReverse ("Source mask inversion",Float)=0
  _SrcBlend ("Source blend",Float)=5
  _DstBlend ("Destination blend",Float)=1
  _ZTest ("Source depth test",Float)=4
  _SourceZWrite ("Source depth write",Float)=0
  _SourceCull ("Source culling",Float)=0
  _SourceColorMask ("Source color mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_SourceZWrite]
   ZTest [_ZTest]
   Cull [_SourceCull]
   ColorMask [_SourceColorMask]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex,_MaskTex;float4 _MainTex_ST,_MaskTex_ST;
   float4 _TintColor,_MainColor,_Color,_MainAnim,_MaskAnim;
   float _Alpha,_MainColorMulti,_SourceFamily,_UseMask,_MainReverse,_MaskReverse;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float2 sourceUV(float2 uv,float4 st,float4 anim){
    float2 p=uv*st.xy+st.zw-.5;float angle=anim.z*_Time.y+anim.w;
    float s=sin(angle),c=cos(angle);
    return float2(c*p.x+s*p.y,-s*p.x+c*p.y)+.5+anim.xy*_Time.y;
   }
   float4 frag(v2f i):SV_Target {
    float2 uv=i.uv*_MainTex_ST.xy+_MainTex_ST.zw;
    if(_SourceFamily>1.5)uv=sourceUV(i.uv,_MainTex_ST,_MainAnim);
    float4 color=tex2D(_MainTex,uv)*i.color;
    if(_SourceFamily<.5)color*=2*_TintColor;
    else if(_SourceFamily>1.5){
     float4 main=tex2D(_MainTex,uv);
     color.rgb=abs(main.rgb-_MainReverse)*_MainColor.rgb*_MainColorMulti*i.color.rgb;
     color.a=abs(main.a)*_Alpha*i.color.a;
     if(_UseMask>.5)color.a*=abs(tex2D(_MaskTex,sourceUV(i.uv,_MaskTex_ST,_MaskAnim)).a-_MaskReverse);
    }
    color.a=saturate(color.a);return color;
   }
   ENDCG
  }
 }
}
