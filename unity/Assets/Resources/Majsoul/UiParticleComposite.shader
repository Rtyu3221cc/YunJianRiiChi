Shader "YunJian/UiParticleComposite" {
 Properties {_MainTex("Premultiplied particle buffer",2D)="black"{} _Tint("Tint",Color)=(1,1,1,1)}
 SubShader {Tags {"Queue"="Overlay"} Pass {
  Blend One OneMinusSrcAlpha
  ZTest Always ZWrite Off Cull Off
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"
  sampler2D _MainTex;fixed4 _Tint;
  struct Input{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct Output{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
  Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
  fixed4 frag(Output i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);float2 edge=min(i.uv,1-i.uv);float fade=saturate(min(edge.x,edge.y)*64);return fixed4(c.rgb*_Tint.rgb*_Tint.a*fade,c.a*_Tint.a*fade);}
  ENDCG
 }}
}
