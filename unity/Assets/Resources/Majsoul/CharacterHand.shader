Shader "YunJian/CharacterHand" {
    Properties {
        _MainTex("Source texture",2D)="white"{} _Color("Source tint",Color)=(1,1,1,1)
        _SourceBRDF("Source BRDF variant",Float)=0
        _RampDirection("Source ramp direction",Vector)=(0,1,0,1)
        _RampMask("Source ramp mask",2D)="white"{} _RimMaskMap("Source rim mask",2D)="white"{}
        _ViewRampStrength("View ramp strength",Float)=1 _ViewRampWidth("View ramp width",Float)=.5
        _BumpMap("Source packed normal",2D)="bump"{} _NormalScale("Normal scale",Float)=1
        _ORMMap("Source ORM",2D)="white"{} _RampMap("Source ramp",2D)="white"{}
        _MatCapTex("Source MatCap",2D)="black"{} _BaseColor("Base color",Color)=(1,1,1,1)
        _RAMP_ON("Ramp enabled",Float)=1 _RampOffset("Ramp offset",Float)=0
        _CUBERMAP("MatCap enabled",Float)=0 _CubeStrength("MatCap strength",Float)=1
        _SpecMetalColor("Specular color",Color)=(1,1,1,1) _SpecMetalScale("Specular scale",Float)=1
        _SpecDirection2("Second source light",Vector)=(0,1,0,0)
        _SpecMetalColor2("Second specular color",Color)=(1,1,1,1) _SpecMetalScale2("Second specular scale",Float)=0
        _RimColor("Rim color",Color)=(0,0,0,0) _RimPower("Rim range",Float)=.4
        _RimSoftness("Rim softness",Float)=.9 _RimIntensity("Rim intensity",Float)=4
        _LightStrength("Source light strength",Float)=1
        _HandLightDir("Source table light direction",Vector)=(0,1,0,0)
        [HDR] _HandLightColor("Source table light color",Color)=(1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off ZWrite On
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;float4 tangent:TANGENT;};
            struct Output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float3 tangent:TEXCOORD3;float3 bitangent:TEXCOORD4;};
            sampler2D _MainTex,_BumpMap,_ORMMap,_RampMap,_MatCapTex,_RampMask,_RimMaskMap;float4 _MainTex_ST,_RimMaskMap_ST;
            float4 _Color,_BaseColor,_SpecMetalColor,_SpecMetalColor2,_SpecDirection2,_RimColor,_RampDirection,_HandLightDir,_HandLightColor;
            float _ViewRampStrength,_ViewRampWidth,_SourceBRDF,_NormalScale,_RAMP_ON,_RampOffset,_CUBERMAP,_CubeStrength,_SpecMetalScale,_SpecMetalScale2,_RimPower,_RimSoftness,_RimIntensity,_LightStrength;
            Output vert(Input v){Output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.tangent=UnityObjectToWorldDir(v.tangent.xyz);o.bitangent=cross(o.normal,o.tangent)*v.tangent.w*unity_WorldTransformParams.w;return o;}
            // Reconstructed from the pinned FxBRDF2 GLES3 program. Its ramp has
            // separate specular (y=.3) and diffuse (y=.7) bands; albedo alone is grey.
            float3 Specular(float3 n,float3 v,float3 l,float roughness,float3 fresnel){
                float nh=saturate(dot(n,normalize(v+l))),nl=saturate(dot(n,l)),nv=saturate(dot(n,v));
                float a2=roughness*roughness,a4=a2*a2;
                float d=a4/(UNITY_PI*(pow(nh*nh*(a4-1)+1,2)+1e-7));
                float vis=.5/(nv*sqrt(nl*nl*(1-a2)+a2)+nl*sqrt(nv*nv*(1-a2)+a2)+1e-5);
                float spec=min(d*vis,1);
                float3 band=_RAMP_ON>.5?clamp(tex2D(_RampMap,float2(spec,.3)).rgb,0,10):spec;
                return band*fresnel*nl*UNITY_PI;
            }
            float4 frag(Output i):SV_Target{
                float4 c=tex2D(_MainTex,i.uv)*_Color;clip(c.a-.01);if(_SourceBRDF<.5)return c;
                float4 packed=tex2D(_BumpMap,i.uv);float2 xy=(float2(packed.x*packed.a,packed.y)*2-1)*_NormalScale;
                float3 n=normalize(normalize(i.tangent)*xy.x+normalize(i.bitangent)*xy.y+normalize(i.normal)*sqrt(1-saturate(dot(xy,xy))));
                float3 v=normalize(_WorldSpaceCameraPos-i.world),l=normalize(_HandLightDir.xyz);
                float2 rm=pow(max(tex2D(_ORMMap,i.uv).gb,1e-6),.45);float roughness=rm.x,metallic=rm.y;
                float3 base=tex2D(_MainTex,i.uv).rgb*_BaseColor.rgb;
                float3 f0=lerp(.04,base,metallic),f=f0+(1-f0)*pow(1-saturate(dot(l,normalize(l+v))),5);
                float nl=saturate(dot(n,l)),nv=saturate(dot(n,v));
                float3 light=_HandLightColor.rgb*_LightStrength;
                float3 diffuse=_RAMP_ON>.5?tex2D(_RampMap,float2(nl+_RampOffset,.7)).rgb:nl;
                // Custom2 and CustomView use the authored ramp direction rather
                // than the scene light for diffuse; their GLES main pass does
                // not multiply diffuse by _LightColor0.
                if(_SourceBRDF>1.5){
                    float3 direction=normalize(_RampDirection.xyz);float q;
                    if(_SourceBRDF<2.5){
                        q=saturate(dot(n,direction));
                        q=saturate((q-_RampOffset)/(tex2D(_RampMask,i.uv).r*1.5+.5)+_RampOffset);
                    }else{
                        float shift=((dot(direction,v)*.5*(1-abs(v.y))+.5)*.5-.1)*_ViewRampStrength;
                        float center=shift*(1-nv)+_RampOffset+.5,w=max(_ViewRampWidth,1e-4);
                        q=saturate((dot(n,direction)*.5+.5-(center-w))/(2*w));
                        q=q*q*(3-2*q);q=q*q*(3-2*q);
                    }
                    diffuse=tex2D(_RampMap,float2(q,.7)).rgb;
                }
                float3 rgb=base*(1-metallic)*diffuse*(_SourceBRDF>1.5?_RampDirection.www:light);
                rgb+=Specular(n,v,l,roughness,f)*light*_SpecMetalColor.rgb*_SpecMetalScale;
                rgb+=Specular(n,v,normalize(_SpecDirection2.xyz),roughness,f)*light*_SpecMetalColor2.rgb*_SpecMetalScale2;
                if(_CUBERMAP>.5){
                    float2 uv=normalize(mul((float3x3)UNITY_MATRIX_V,n)).xy*.5+.5;
                    float4 r=roughness*float4(-1,-.0275,-.572,.022)+float4(1,.0425,1.04,-.04);
                    float a=min(r.x*r.x,exp2(-9.28*nv))*r.x+r.y;
                    float2 ab=float2(-1.04,1.04)*a+r.zw;
                    rgb+=tex2Dlod(_MatCapTex,float4(uv,0,0)).rgb*(f0*ab.x+saturate(50*f0.g)*ab.y)*_CubeStrength;
                }
                float range=max(1-_RimPower,.001),softness=max(1-_RimSoftness,.001);
                float rimMask=_SourceBRDF>1.5?tex2D(_RimMaskMap,i.uv*_RimMaskMap_ST.xy+_RimMaskMap_ST.zw).r:1;
                rgb+=smoothstep(range-softness,range+softness,1-nv)*rimMask*_RimColor.rgb*_RimIntensity*.1;
                return float4(rgb,1);
            }
            ENDCG
        }
    }
}
