Shader "YunJian/CharacterOver" {
    Properties { _MainTex("Texture",2D)="white"{} }
    SubShader { Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img i):SV_Target{return tex2D(_MainTex,i.uv);}
        ENDCG }
    }
}
