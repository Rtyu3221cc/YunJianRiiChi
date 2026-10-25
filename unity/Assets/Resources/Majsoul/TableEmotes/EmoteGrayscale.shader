Shader "YunJian/EmoteGrayscale" {
    Properties { _MainTex ("Image", 2D) = "white" {} }
    SubShader { Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);fixed gray=dot(c.rgb,fixed3(.299,.587,.114));return fixed4(gray,gray,gray,c.a);}
        ENDCG }
    }
}
