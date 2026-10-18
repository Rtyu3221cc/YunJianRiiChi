Shader "YunJian/NativeCallout" {
    Properties {_MainTex("APK sprite",2D)="white"{} _MaskTex("APK alpha mask",2D)="white"{} _UseMask("Use mask",Float)=0}
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_MaskTex;float _UseMask;float4x4 _WorldToMask;
            struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 mask:TEXCOORD1;};
            output vert(input i){output o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.mask=mul(_WorldToMask,mul(unity_ObjectToWorld,i.vertex)).xy;return o;}
            fixed4 frag(output i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);if(_UseMask>.5){float inside=step(0,i.mask.x)*step(i.mask.x,1)*step(0,i.mask.y)*step(i.mask.y,1);c.a*=tex2D(_MaskTex,i.mask).a*inside;}return c;}
            ENDCG
        }
    }
}
