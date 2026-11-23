// FizzFX/Normal adapter from the pinned native GLES variants. This batch keeps
// its own shader so previously approved effects retain their rendering profile.
Shader "YunJian/NovR10EffectParticle" {
 Properties {
  _DistortTex ("Standard distortion red",2D)="black" {} _Distort1UV ("Distortion UV",Vector)=(1,1,0,0)
  _DistortAnim ("Distortion animation",Vector)=(0,0,0,0) _DistortCustom ("Distortion custom2 Z",Float)=0
  _Distort2Tex ("Secondary distortion green",2D)="black" {} _Distort2UV ("Secondary distortion UV",Vector)=(1,1,0,0)
  _Distort2Anim ("Secondary distortion animation",Vector)=(0,0,0,0) _DistortDistort ("Distort primary UV",Float)=0
  _DistortMaskTex ("Distortion mask blue",2D)="white" {} _DistortMaskUV ("Distortion mask UV",Vector)=(1,1,0,0)
  _DistortMaskAnim ("Distortion mask animation",Vector)=(0,0,0,0) _DistortDistortMask ("Distort mask UV",Float)=0
  _DistortMain ("Distort main",Float)=0 _DistortMask1 ("Distort primary mask",Float)=0 _DistortMask2 ("Distort secondary mask",Float)=0
  _DistortDissolve ("Distort dissolve",Float)=0 _DistortDissolveMask ("Distort dissolve mask",Float)=0
  _StandardDistort ("Distortion keyword",Float)=0 _StandardDistort2 ("Secondary distortion keyword",Float)=0 _StandardDistortMask ("Distortion mask keyword",Float)=0
  _DissolveTexUV ("Native dissolve UV",Vector)=(1,1,0,0) _DissolveUvCostom ("Native dissolve custom2 XY",Float)=0
  _DissolveClampU ("Clamp dissolve U",Float)=0 _DissolveClampV ("Clamp dissolve V",Float)=0

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
  _StandardDissolve ("Standard dissolve enabled",Float)=0 _CustomStreams ("Source particle custom streams",Float)=0
  _MainCustom ("Main custom offset",Float)=0 _MaskCustom ("Mask custom offset",Float)=0
  _DissolveUV ("Dissolve UV",Vector)=(1,1,0,0) _DissolveAmount ("Dissolve amount",Float)=0
  _DissolveDetail ("Dissolve power",Float)=1 _DissolveOneMinus ("Invert dissolve",Float)=0 _DissolveCustom ("Dissolve particle input",Float)=0
  _DissolveEdgeSoftness ("Dissolve softness",Float)=0 _DissolveEdgeWidth ("Dissolve edge width",Float)=0
  _DissolveMaskTex ("Dissolve mask",2D)="black" {} _DissolveMaskEnable ("Use dissolve mask",Float)=0
  _DissolveMaskDetail ("Dissolve mask power",Float)=1 _DissolveMaskUV ("Dissolve mask UV",Vector)=(1,1,0,0) _DissolveMaskAnim ("Dissolve mask animation",Vector)=(0,0,0,0)
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
   float _StandardDissolve,_CustomStreams,_MainCustom,_MaskCustom,_DissolveAmount,_DissolveDetail,_DissolveOneMinus,_DissolveCustom,_DissolveEdgeSoftness,_DissolveEdgeWidth;
   float _DissolveMaskEnable,_DissolveMaskDetail;
   float4 _DissolveUV,_DissolveMaskUV,_DissolveMaskAnim;
   sampler2D _DissolveMaskTex;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 data1:TEXCOORD1;float4 data2:TEXCOORD2;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 data1:TEXCOORD1;float4 data2:TEXCOORD2;float4 color:COLOR;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.data1=v.data1*_CustomStreams;o.data2=v.data2*_CustomStreams;o.color=v.color;return o;}
   float2 sourceUV(float2 uv,float4 st,float4 anim){
    float time=_EffectAge>=0?_EffectAge:_Time.y;
    float2 p=uv*st.xy+st.zw-.5;float angle=anim.z*time+anim.w;
    float s=sin(angle),c=cos(angle);
    return float2(c*p.x+s*p.y,-s*p.x+c*p.y)+.5+anim.xy*time;
   }
   float sourceSmooth(float distance,float width){float t=saturate(distance/max(width,.000001));return t*t*(3-2*t);}
   float2 standardUV(float2 uv,float4 st,float4 anim){
    float time=_EffectAge>=0?_EffectAge:_Time.y;float2 p=uv*st.xy+st.zw;
    p-=.5;
    float angle=anim.z*(time-anim.w*.0174532924)+anim.w*.0174532924;
    float sn=sin(angle),cs=cos(angle);return float2(cs*p.x+sn*p.y,-sn*p.x+cs*p.y)+.5+anim.xy*time;
   }
   sampler2D _DistortTex,_Distort2Tex,_DistortMaskTex;
   float4 _Distort1UV,_Distort2UV,_DistortMaskUV,_DistortAnim,_Distort2Anim,_DistortMaskAnim,_DissolveTexUV;
   float _DistortCustom,_DistortDistort,_DistortDistortMask,_DistortMain,_DistortMask1,_DistortMask2,_DistortDissolve,_DistortDissolveMask;
   float _StandardDistort,_StandardDistort2,_StandardDistortMask,_DissolveUvCostom,_DissolveClampU,_DissolveClampV;
   float4 frag(v2f i):SV_Target {
    if(_SourceFamily<1.5){float4 c=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw)*i.color;if(_SourceFamily<.5)c*=2*_TintColor;c.a=saturate(c.a);return c;}
    if(_SourceFamily>3.5){float4 c=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw)*_Tint*_AlbedoIntensity;c.a*=_AlphaScale;return c;}
    if(_SourceFamily>2.5){
     float distortion=0;
     if(_StandardDistort>.5){
      float secondary=_StandardDistort2>.5?tex2D(_Distort2Tex,standardUV(i.uv,_Distort2UV,_Distort2Anim)).g:0;
      distortion=tex2D(_DistortTex,standardUV(i.uv,_Distort1UV,_DistortAnim)+secondary*_DistortDistort).r;
      float mask=_StandardDistortMask>.5?tex2D(_DistortMaskTex,standardUV(i.uv,_DistortMaskUV,_DistortMaskAnim)+distortion*_DistortDistortMask).b:1;
      distortion*=mask*lerp(1,i.data2.z,_DistortCustom*_CustomStreams);
     }
     float4 mainST=_MainTex_ST;mainST.zw+=i.data1.xy*_MainCustom;
     float2 mainUV=i.uv*mainST.xy+mainST.zw;mainUV=lerp(mainUV,saturate(mainUV),float2(_MainClampU,_MainClampV));
     float4 c=tex2D(_MainTex,standardUV(mainUV,float4(1,1,0,0),_MainAnim)+distortion*_DistortMain);float grey=dot(c.rgb,float3(.299,.587,.114));c.rgb=lerp(c.rgb,grey.xxx,_MainDesaturate);
     c.rgb=lerp(pow(max(c.rgb,0),_MainDetail.xxx),c.rgb*_MainDetail.z,_MainDetail.w)*_MainColor.rgb;
     c.a*=_MainColor.a*_MainAlpha;
     float4 maskUV=_MaskUV;maskUV.zw+=i.data1.zw*_MaskCustom;
     if(_UseMaskTex>.5)c.a*=tex2D(_MaskTex,standardUV(i.uv,maskUV,_MaskAnim)+distortion*_DistortMask1).a;
     c.a*=lerp(1,tex2D(_Mask2Tex,standardUV(i.uv,_Mask2UV,_Mask2Anim)+distortion*_DistortMask2).b,_Mask2Enable);
     if(_StandardDissolve>.5){
      float2 dissolveUV=i.uv*_DissolveTexUV.xy+_DissolveTexUV.zw+i.data2.xy*_DissolveUvCostom;
      dissolveUV=lerp(dissolveUV,saturate(dissolveUV),float2(_DissolveClampU,_DissolveClampV));
      float noise=tex2D(_DissolveTex,standardUV(dissolveUV,float4(1,1,0,0),_DissolveAnim)+distortion*_DistortDissolve).r;
      noise=pow(lerp(noise,1-noise,_DissolveOneMinus),_DissolveDetail);
      noise+=_DissolveMaskEnable*pow(tex2D(_DissolveMaskTex,standardUV(i.uv,_DissolveMaskUV,_DissolveMaskAnim)+distortion*_DistortDissolveMask).g,_DissolveMaskDetail);
      float cut=noise-lerp(_DissolveAmount,i.data2.w,_DissolveCustom*_CustomStreams)+1;
      float softness=clamp(2-_DissolveEdgeSoftness,0,.5);
      float width=max(1-2*softness,.0001);
      float inner=sourceSmooth(cut-softness,width),outer=sourceSmooth(cut+_DissolveEdgeWidth-softness,width);
      float3 edge=(outer-inner)*_DissolveEdgeColor.rgb;
      c.rgb=lerp(lerp(edge,c.rgb,inner),c.rgb+edge,_DissolveEdgeMode);
      c.a*=outer;
     }else c.rgb+=_DissolveEdgeMode*_DissolveEdgeColor.rgb;
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
