Shader "Custom/MaskErvi" {
    Properties {
        _MainTex ("灰度图", 2D) = "white" {}
        _MainColor ("颜色", Color) = (1,1,1,1)
        _EmissColor("自发光颜色",Color)=(1,1,1,1)
        _EmissIntensity("自发光强度",Float)=5
        _FlashSpeed("频闪速度",Vector)=(2,0.5,0,0)
        _FlashBool("是否频闪",Int)=0
        
    }
    SubShader {
        Tags { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // 定义与Properties块对应的变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _EmissColor;
            float _EmissIntensity;
            float4 _FlashSpeed;
            int _FlashBool;
            
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
            };
            
            // 顶点着色器
            Varyings vert(Attributes v) {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                
                return o;
            }
            
            // 片元着色器
            half4 frag(Varyings i) : SV_Target {
                // 采样纹理
                half Gray = tex2D(_MainTex, i.uv) ;
                float3 Diffuse=(1-Gray)*_MainColor;
                float Flash=1;
                if(_FlashBool==1)
                {
                    Flash=sin(_Time.y*_FlashSpeed.x)+_FlashSpeed.y;
                }
                float3 Emiss=Gray*Flash*_EmissColor*_EmissIntensity;
                float3 finalcolor=Emiss+Diffuse;
                return half4(finalcolor,1.0);
            }
            ENDHLSL
        }
    }
}