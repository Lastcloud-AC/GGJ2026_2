Shader "Custom/MaskErvi" {
    Properties {
        _MainTex ("灰度图", 2D) = "white" {}
        _MainColor ("颜色", Color) = (1,1,1,1)
        _EmissColor("自发光颜色",Color)=(1,1,1,1)
        _EmissIntensity("自发光强度",Float)=5
        _FlashSpeed("频闪速度",Vector)=(2,0.5,0,0)
        _FlashBool("是否频闪",Int)=0
        _PosterizeSteps("色调分离阶数", Range(1, 20)) = 4
        _LightInfluence("点光源影响强度", Range(0, 2)) = 1.0
        _LightBool("是否受光照影响",Int)=0
    }
    SubShader {
        Tags { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "LightMode" = "UniversalForward"  // 添加光照模式标签
        }
        
        Pass {
            
             Stencil {
                Ref 2           // 与遮挡物相同的参考值
                Comp Equal      // 只在遮挡区域渲染
                Pass Keep       // 保持模板缓冲区值
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // 定义与Properties块对应的变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _EmissColor;
            float _EmissIntensity;
            float4 _FlashSpeed;
            int _FlashBool;
            float _PosterizeSteps;
            float _LightInfluence;
            int _LightBool;
            // 顶点着色器的输入结构
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            // 顶点着色器的输出结构（也是片元着色器的输入）
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };
             // 色调分离函数
            float3 Posterize(float3 color, float steps) {
                float3 posterized = floor(color * steps) / steps;
                return posterized;
            }
            
            // 计算点光源影响
            float3 CalculatePointLights(float3 worldPos, float3 worldNormal) {
                float3 totalLight = 0;
                int additionalLightsCount = GetAdditionalLightsCount();
                
                for (int i = 0; i < additionalLightsCount; ++i) {
                    Light light = GetAdditionalLight(i, worldPos);
                    
                    // 只处理点光源
                    if (light.distanceAttenuation > 0) {
                        // 计算漫反射
                        float NdotL = saturate(dot(worldNormal, light.direction));
                        float3 diffuse = light.color * NdotL * light.distanceAttenuation;
                        
                        // 应用色调分离
                        diffuse = Posterize(diffuse, _PosterizeSteps);
                        
                        totalLight += diffuse;
                    }
                }
                
                return totalLight * _LightInfluence;
            }
            // 顶点着色器
            Varyings vert(Attributes v) {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                o.worldNormal = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            
            // 片元着色器
            half4 frag(Varyings i) : SV_Target {
                // 采样纹理
                half Gray = tex2D(_MainTex, i.uv) ;
                float3 Diffuse=(1-Gray)*_MainColor;
                float3 pointLights = CalculatePointLights(i.worldPos, normalize(i.worldNormal));
                float Flash=1;
                if(_FlashBool==1)
                {
                    Flash=sin(_Time.y*_FlashSpeed.x)+_FlashSpeed.y;
                }
                float3 Emiss=Gray*Flash*_EmissColor*_EmissIntensity;
                float3 LightFlash=pointLights*Flash;
                
                float3 finalcolor=Emiss+Diffuse;
                if(_LightBool==1)
                {
                    finalcolor+=LightFlash;
                }
                
                return half4(finalcolor,1.0);
            }
            ENDHLSL
        }
    }
}