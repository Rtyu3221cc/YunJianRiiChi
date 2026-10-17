Shader "YunJian/UiParticleBuffer" {
 Properties { _MainTex("Original texture",2D)="white"{} _TintColor("Original tint",Color)=(.5,.5,.5,.5) _SrcBlend("Original source blend",Float)=5 _DstBlend("Original destination blend",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Pass {
  Blend One [_DstBlend], One OneMinusSrcAlpha
  ZWrite Off Cull Off
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"
  sampler2D _MainTex;float4 _MainTex_ST;fixed4 _TintColor;float _SrcBlend,_DstBlend;
  struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
  struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
  Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.color=v.color;return o;}
  fixed4 frag(Output i):SV_Target{fixed4 c=2*i.color*_TintColor*tex2D(_MainTex,i.uv);float factor=_SrcBlend==5?c.a:1;return fixed4(c.rgb*factor,_DstBlend==1?0:c.a);}
  ENDCG
 }}
}
