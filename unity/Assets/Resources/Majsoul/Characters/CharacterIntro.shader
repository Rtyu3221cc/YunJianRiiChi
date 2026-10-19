Shader "YunJian/CharacterIntro" {
 Properties {_MainTex("Source",2D)="white"{} _Background("Native mask",2D)="white"{} _Portrait("Character",2D)="white"{} }
 SubShader {Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _Background,_Portrait;float4 _PortraitRect;float _Flip;
 fixed4 frag(v2f_img i):SV_Target {fixed4 bg=tex2D(_Background,i.uv);float2 uv=(i.uv-_PortraitRect.xy)/_PortraitRect.zw;float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);if(_Flip>.5)uv.x=1-uv.x;fixed4 p=tex2D(_Portrait,uv);p.a*=inside;return fixed4(lerp(bg.rgb,p.rgb,p.a),bg.a);}
 ENDCG }}
}
