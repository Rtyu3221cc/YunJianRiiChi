// FizzFX/Normal adapter from the pinned native GLES variants. This batch keeps
// its own shader so previously approved effects retain their rendering profile.
Shader "YunJian/WardrobeEffectParticle" {
 Properties {
  _MainTex ("Main",2D)="white" {} _MainAnim ("Main UV",Vector)=(0,0,0,0)
  _MaskTex ("Mask alpha",2D)="white" {} _MaskAnim ("Mask UV",Vector)=(0,0,0,0)
  _DistortionTex ("Distortion green",2D)="white" {} _DistortionAnim ("Distortion UV",Vector)=(0,0,0,0)
  _DMaskTex ("Distortion mask red",2D)="white" {} _DMaskAnim ("Distortion mask UV",Vector)=(0,0,0,0)
  _DissolveTex ("Dissolve red",2D)="white" {} _DissolveAnim ("Dissolve UV",Vector)=(0,0,0,0)
  _AMBlightTex ("Ambient",2D)="white" {} _AMBlightAnim ("Ambient UV",Vector)=(0,0,0,0)
  _MainColor ("Main color",Color)=(1,1,1,1) _MainColorMulti ("Main multiplier",Float)=1
  _Alpha ("Alpha",Float)=1 _SpineAnimAlpha ("Spine alpha",Float)=1
  _MainReverse ("Main inversion",Float)=0 _MaskReverse ("Mask inversion",Float)=0
  _DistortionReverse ("Distortion inversion",Float)=0 _DMaskReverse ("Distortion mask inversion",Float)=0
  _DissolveReverse ("Dissolve inversion",Float)=0 _AMBReverse ("Ambient inversion",Float)=0
  _MainTexDistortion ("Main distortion",Float)=0 _MaskTexDistortion ("Mask distortion",Float)=0
  _AMBlightTexDistortion ("Ambient distortion",Float)=0 _EdgeDistortion ("Dissolve distortion",Float)=0
  _AMBlightLerp ("Ambient blending",Float)=0 _Dissolve ("Dissolve",Float)=0
  _DissMode ("Dissolve mode",Float)=0 _InsideEdge ("Inner softness",Float)=0 _OutsideEdge ("Outer softness",Float)=0
  _EdgeWidth ("Edge width",Float)=0 _EdgeColor ("Edge color",Color)=(1,1,1,1) _EdgeColorMulti ("Edge multiplier",Float)=1
  _UseMainTex ("Main enabled",Float)=0 _UseMaskTex ("Mask enabled",Float)=0
  _UseDistortionTex ("Distortion enabled",Float)=0 _UseDMaskTex ("Distortion mask enabled",Float)=0
  _UseDissolveTex ("Dissolve enabled",Float)=0 _UseAMBlightTex ("Ambient enabled",Float)=0
  _SrcBlend ("Source blend",Float)=5 _DstBlend ("Destination blend",Float)=1
  _ZTest ("Depth test",Float)=0 _SourceZWrite ("Depth write",Float)=0 _SourceCull ("Cull",Float)=0
  _EffectAge ("Sampled effect time",Float)=-1
  _SourceFamily ("Source shader family",Float)=2
  _TintColor ("Legacy tint",Color)=(.5,.5,.5,.5)
  _Tint ("Catfood tint",Color)=(1,1,1,1)
  _AlbedoIntensity ("Catfood intensity",Float)=1 _AlphaScale ("Catfood opacity",Float)=1
  _MainAlpha ("Standard main alpha",Float)=1 _MainDesaturate ("Standard desaturation",Float)=0
  _MainDetail ("Standard main detail",Vector)=(1,1,1,.5)
  _MainClampU ("Standard clamp U",Float)=0 _MainClampV ("Standard clamp V",Float)=0
  _MaskUV ("Standard mask UV",Vector)=(1,1,0,0)
  _Mask2Tex ("Standard secondary mask",2D)="white" {} _Mask2UV ("Secondary mask UV",Vector)=(1,1,0,0)
  _Mask2Anim ("Secondary mask animation",Vector)=(0,0,0,0) _Mask2Enable ("Secondary mask",Float)=0
  _DissolveEdgeMode ("Standard edge mode",Float)=0 _DissolveEdgeColor ("Standard edge color",Color)=(1,1,1,1)
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Blend [_SrcBlend] [_DstBlend]
   ZWrite [_SourceZWrite] ZTest [_ZTest] Cull [_SourceCull]
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex,_MaskTex,_DistortionTex,_DMaskTex,_DissolveTex,_AMBlightTex;
   float4 _MainTex_ST,_MaskTex_ST,_DistortionTex_ST,_DMaskTex_ST,_DissolveTex_ST,_AMBlightTex_ST;
   float4 _MainAnim,_MaskAnim,_DistortionAnim,_DMaskAnim,_DissolveAnim,_AMBlightAnim,_MainColor,_EdgeColor;
   float _Alpha,_SpineAnimAlpha,_MainColorMulti,_MainReverse,_MaskReverse,_DistortionReverse,_DMaskReverse,_DissolveReverse,_AMBReverse;
   float _MainTexDistortion,_MaskTexDistortion,_AMBlightTexDistortion,_EdgeDistortion,_AMBlightLerp;
   float _Dissolve,_DissMode,_InsideEdge,_OutsideEdge,_EdgeWidth,_EdgeColorMulti,_EffectAge;
   float _SourceFamily,_AlbedoIntensity,_AlphaScale,_MainAlpha,_MainDesaturate,_MainClampU,_MainClampV,_Mask2Enable,_DissolveEdgeMode;
   float4 _TintColor,_Tint,_MainDetail,_MaskUV,_Mask2UV,_Mask2Anim,_DissolveEdgeColor;sampler2D _Mask2Tex;
   float _UseMainTex,_UseMaskTex,_UseDistortionTex,_UseDMaskTex,_UseDissolveTex,_UseAMBlightTex;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   float2 sourceUV(float2 uv,float4 st,float4 anim){
    float time=_EffectAge>=0?_EffectAge:_Time.y;
    float2 p=uv*st.xy+st.zw-.5;float angle=anim.z*time+anim.w;
    float s=sin(angle),c=cos(angle);
    return float2(c*p.x+s*p.y,-s*p.x+c*p.y)+.5+anim.xy*time;
   }
   float sourceSmooth(float distance,float width){float t=saturate(distance/max(width,.000001));return t*t*(3-2*t);}
   float2 standardUV(float2 uv,float4 st,float4 anim){
    float time=_EffectAge>=0?_EffectAge:_Time.y;float2 p=uv*st.xy+st.zw;
    p=lerp(p,saturate(p),float2(_MainClampU,_MainClampV));p-=.5;
    float angle=anim.z*(time-anim.w*.0174532924)+anim.w*.0174532924;
    float sn=sin(angle),cs=cos(angle);return float2(cs*p.x+sn*p.y,-sn*p.x+cs*p.y)+.5+anim.xy*time;
   }
   float4 frag(v2f i):SV_Target {
    if(_SourceFamily<1.5){float4 c=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw)*i.color;if(_SourceFamily<.5)c*=2*_TintColor;c.a=saturate(c.a);return c;}
    if(_SourceFamily>3.5){float4 c=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw)*_Tint*_AlbedoIntensity;c.a*=_AlphaScale;return c;}
    if(_SourceFamily>2.5){
     float4 c=tex2D(_MainTex,standardUV(i.uv,_MainTex_ST,_MainAnim));float grey=dot(c.rgb,float3(.299,.587,.114));c.rgb=lerp(c.rgb,grey.xxx,_MainDesaturate);
     c.rgb=lerp(pow(max(c.rgb,0),_MainDetail.xxx),c.rgb*_MainDetail.z,_MainDetail.w)*_MainColor.rgb+_DissolveEdgeMode*_DissolveEdgeColor.rgb;
     c.a*=_MainColor.a*_MainAlpha;
     if(_UseMaskTex>.5)c.a*=tex2D(_MaskTex,standardUV(i.uv,_MaskUV,_MaskAnim)).a;
     c.a*=lerp(1,tex2D(_Mask2Tex,standardUV(i.uv,_Mask2UV,_Mask2Anim)).b,_Mask2Enable);
     c*=i.color;c.a=saturate(c.a);return c*_SpineAnimAlpha;
    }
    float distortion=0;
    if(_UseDistortionTex>.5){
     distortion=abs(tex2D(_DistortionTex,sourceUV(i.uv,_DistortionTex_ST,_DistortionAnim)).g-_DistortionReverse);
     if(_UseDMaskTex>.5)distortion*=abs(tex2D(_DMaskTex,sourceUV(i.uv,_DMaskTex_ST,_DMaskAnim)).r-_DMaskReverse);
    }
    float4 main=float4(1,1,1,1);
    if(_UseMainTex>.5){main=tex2D(_MainTex,sourceUV(i.uv,_MainTex_ST,_MainAnim)+distortion*_MainTexDistortion);main.rgb=abs(main.rgb-_MainReverse);main.a=abs(main.a);}
    float3 rgb=main.rgb;
    if(_UseAMBlightTex>.5){
     float3 ambient=abs(tex2D(_AMBlightTex,sourceUV(i.uv,_AMBlightTex_ST,_AMBlightAnim)+distortion*_AMBlightTexDistortion).rgb-_AMBReverse);
     rgb=_UseMainTex>.5 ? ambient*rgb+_AMBlightLerp*(ambient+rgb-ambient*rgb) : ambient+_AMBlightLerp;
    }
    float alpha=main.a*_Alpha*_SpineAnimAlpha;
    if(_UseDissolveTex>.5){
     float noise=abs(tex2D(_DissolveTex,sourceUV(i.uv,_DissolveTex_ST,_DissolveAnim)+distortion*_EdgeDistortion).r-_DissolveReverse);
     float level=lerp(_Dissolve,1-i.color.a,_DissMode);
     float inside=sourceSmooth(noise-level-_EdgeWidth,_InsideEdge),outside=sourceSmooth(noise-level,_OutsideEdge);
     rgb=rgb*inside*_MainColor.rgb*_MainColorMulti+(outside-inside)*_EdgeColor.rgb*_EdgeColorMulti;
     alpha*=outside*saturate(i.color.a+_DissMode);
    }else{rgb*=_MainColor.rgb*_MainColorMulti;alpha*=i.color.a;}
    if(_UseMaskTex>.5)alpha*=abs(tex2D(_MaskTex,sourceUV(i.uv,_MaskTex_ST,_MaskAnim)+distortion*_MaskTexDistortion).a-_MaskReverse);
    return float4(rgb*i.color.rgb,saturate(alpha));
   }
   ENDCG
  }
 }
}
