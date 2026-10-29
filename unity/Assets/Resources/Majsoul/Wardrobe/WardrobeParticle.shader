Shader "YunJian/WardrobeParticle" {
 Properties {
  _MainTex ("Source texture",2D)="white" {}
  _MaskTex ("Source mask",2D)="white" {}
  _DistortionTex ("Source distortion green channel",2D)="white" {}
  _AMBlightTex ("Source ambient texture",2D)="white" {}
  _DissolveTex ("Source dissolve red channel",2D)="white" {}
  _DissolveAnim ("Source dissolve UV animation",Vector)=(0,0,0,0)
  _Dissolve ("Dissolve threshold",Float)=0
  _DissMode ("Constant or particle alpha threshold",Float)=0
  _DissolveReverse ("Dissolve inversion",Float)=0
  _InsideEdge ("Inside softness",Float)=0
  _OutsideEdge ("Outside softness",Float)=0
  _EdgeWidth ("Edge width",Float)=0
  _EdgeColor ("Edge color",Color)=(0,0,0,1)
  _EdgeColorMulti ("Edge multiplier",Float)=0
  _UseDissolve ("Has dissolve texture",Float)=0
  _DistortionAnim ("Source distortion UV animation",Vector)=(0,0,0,0)
  _AMBlightAnim ("Source ambient UV animation",Vector)=(0,0,0,0)
  _DistortionReverse ("Distortion inversion",Float)=0
  _AMBReverse ("Ambient inversion",Float)=0
  _MainTexDistortion ("Main distortion strength",Float)=0
  _MaskTexDistortion ("Mask distortion strength",Float)=0
  _AMBlightTexDistortion ("Ambient distortion strength",Float)=0
  _AMBlightLerp ("Ambient blend",Float)=0
  _RenderSrcBlend ("Render source blend",Float)=5
  _PreviewBuffer ("Premultiplied preview",Float)=0
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
   Blend [_RenderSrcBlend] [_DstBlend], One OneMinusSrcAlpha
   ZWrite [_SourceZWrite]
   ZTest [_ZTest]
   Cull [_SourceCull]
   ColorMask [_SourceColorMask]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex,_MaskTex,_DistortionTex,_AMBlightTex,_DissolveTex;float4 _MainTex_ST,_MaskTex_ST,_DistortionTex_ST,_AMBlightTex_ST,_DissolveTex_ST;
   float4 _DissolveAnim,_EdgeColor;
   float _Dissolve,_DissMode,_DissolveReverse,_InsideEdge,_OutsideEdge,_EdgeWidth,_EdgeColorMulti,_UseDissolve;
   float4 _DistortionAnim,_AMBlightAnim;
   float _MainTexDistortion,_MaskTexDistortion,_AMBlightTexDistortion,_AMBlightLerp,_DistortionReverse,_AMBReverse,_PreviewBuffer,_SrcBlend,_DstBlend;
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
     float distortion=abs(tex2D(_DistortionTex,sourceUV(i.uv,_DistortionTex_ST,_DistortionAnim)).g-_DistortionReverse);
     float4 main=tex2D(_MainTex,uv+distortion*_MainTexDistortion);
     color.rgb=abs(main.rgb-_MainReverse)*_MainColor.rgb*_MainColorMulti*i.color.rgb;
     color.a=abs(main.a)*_Alpha*i.color.a;
     if(_UseMask>.5)color.a*=abs(tex2D(_MaskTex,sourceUV(i.uv,_MaskTex_ST,_MaskAnim)+distortion*_MaskTexDistortion).b-_MaskReverse);
     if(_AMBlightLerp>0)color.rgb*=lerp(float3(1,1,1),abs(tex2D(_AMBlightTex,sourceUV(i.uv,_AMBlightTex_ST,_AMBlightAnim)+distortion*_AMBlightTexDistortion).rgb-_AMBReverse),_AMBlightLerp);
     if(_UseDissolve>.5){
      float level=_DissMode>.5?1-i.color.a:_Dissolve;
      float distance=abs(tex2D(_DissolveTex,sourceUV(i.uv,_DissolveTex_ST,_DissolveAnim)).r-_DissolveReverse)-level;
      float softness=max(.00001,_InsideEdge+_OutsideEdge);
      color.a*=saturate((distance+_OutsideEdge)/softness);
      if(_EdgeColorMulti>0)color.rgb+=_EdgeColor.rgb*_EdgeColorMulti*saturate(1-abs(distance)/max(.00001,abs(_EdgeWidth)));
     }
    }
    color.a=saturate(color.a);
    if(_PreviewBuffer>.5)return float4(color.rgb*(_SrcBlend==5?color.a:1),_DstBlend==1?0:color.a);
    return color;
   }
   ENDCG
  }
 }
}
