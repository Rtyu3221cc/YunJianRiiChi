Shader "YunJian/CharacterStraightAlpha" {
 Properties {_MainTex("Texture",2D)="white"{}}
 SubShader {Cull Off ZWrite Off ZTest Always Pass {CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;fixed4 frag(v2f_img i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);if(c.a>.0001)c.rgb/=c.a;return c;}
 ENDCG }}
}
