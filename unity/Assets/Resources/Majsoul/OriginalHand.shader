Shader "YunJian/OriginalHand"
{
    Properties { _MainTex ("Original hand faces", 2D) = "white" {} _BackTex ("Original hand backs", 2D) = "white" {} _Dim ("Dim", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            sampler2D _MainTex, _BackTex; float4 _MainTex_ST; float _Dim;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 face=tex2D(_MainTex,TRANSFORM_TEX(i.uv,_MainTex));
                fixed4 back=tex2D(_BackTex,float2(i.uv.x*.5,i.uv.y));
                fixed4 c=fixed4(lerp(face.rgb,back.rgb,back.a),max(face.a,back.a));
                // A tile that keeps the hand tenpai stays bright; the rest are dimmed
                // while the riichi declaration is being chosen.
                fixed grey=dot(c.rgb,fixed3(.299,.587,.114));
                c.rgb=lerp(c.rgb,grey*.5,_Dim);
                return c;
            }
            ENDCG
        }
    }
}
