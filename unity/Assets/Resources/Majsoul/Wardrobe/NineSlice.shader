Shader "YunJian/WardrobeNineSlice" {
    Properties { _MainTex("Sprite",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _Border("Border",Vector)=(0,0,0,0) _SourceSize("Source",Vector)=(40,40,0,0) _TargetSize("Target",Vector)=(100,100,0,0) _Desaturate("Desaturate",Range(0,1))=0 }
    SubShader {
        Tags {"Queue"="Overlay" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Tint,_Border,_SourceSize,_TargetSize;float _Desaturate;
            float mapped(float uv,float target,float source,float first,float last){
                float p=uv*target;
                if(p<first)return p/source;
                if(p>target-last)return 1-(target-p)/source;
                return (first+(p-first)*(source-first-last)/max(.001,target-first-last))/source;
            }
            fixed4 frag(v2f_img i):SV_Target {
                float2 uv=float2(mapped(i.uv.x,_TargetSize.x,_SourceSize.x,_Border.x,_Border.z),mapped(i.uv.y,_TargetSize.y,_SourceSize.y,_Border.y,_Border.w));
                fixed4 c=tex2D(_MainTex,uv)*_Tint;float grey=dot(c.rgb,float3(.299,.587,.114));c.rgb=lerp(c.rgb,grey.xxx,_Desaturate);return c;
            }
            ENDCG
        }
    }
}
