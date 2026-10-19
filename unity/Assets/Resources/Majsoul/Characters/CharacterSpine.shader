Shader "YunJian/CharacterSpine" {
 Properties { _MainTex("Texture",2D)="white"{} _SrcBlend("Source",Float)=1 _DstBlend("Destination",Float)=10 _PMA("PMA input",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float _PMA;float4x4 _CharacterProjection;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float3 dark:TEXCOORD1;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float3 dark:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.vertex=mul(_CharacterProjection,v.vertex);o.uv=v.uv;o.color=v.color;o.dark=v.dark;return o;}
   fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);if(_PMA>.5&&c.a>.0001)c.rgb/=c.a;c.rgb=c.rgb*i.color.rgb+(1-c.rgb)*i.dark;c.a*=i.color.a;c.rgb*=c.a;return c;}
  ENDCG }
 }
}
