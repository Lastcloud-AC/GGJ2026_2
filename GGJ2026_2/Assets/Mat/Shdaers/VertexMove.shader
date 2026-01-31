Shader "Custom/WorldSpaceNoiseDisplacement" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NoiseScale ("噪声尺度", Float) = 5.0
        _DisplacementStrength ("顶点偏移强度", Float) = 0.1
        _Speed ("噪声流动速度", Float) = 0.5
        _Color ("颜色", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // 定义与Properties块对应的变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _NoiseScale;
            float _DisplacementStrength;
            float _Speed;
            float4 _Color;
            
            // 引用内置的GradientNoise函数
            float unity_gradientNoise(float2 p) {
                // 此函数实现梯度噪声，返回范围约为[-0.5, 0.5]
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            float GradientNoise(float2 UV, float Scale) {
                return unity_gradientNoise(UV * Scale) + 0.5;
            }
            
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
                
                // 将顶点位置和法线从模型空间变换到世界空间
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(v.normalOS);
                
                // 基于世界坐标生成噪声[2](@ref)
                float2 noiseUV = worldPos.xz; // 使用世界XZ平面作为噪声输入
                float2 speed = float2(_Speed, _Speed) * _Time.y; // 加入时间流动[2](@ref)
                float noiseValue = GradientNoise(noiseUV + speed, _NoiseScale);
                
                // 将噪声值映射到更明显的范围，并应用于顶点偏移[1](@ref)
                // 噪声原始范围约为[0,1]，调整到[-1,1]使偏移有正有负
                float displacement = (noiseValue * 2.0 - 1.0) * _DisplacementStrength;
                
                // 沿世界法线方向偏移顶点[1](@ref)
                worldPos.xz += worldNormal * displacement;
                
                // 将偏移后的世界坐标变换到裁剪空间
                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = worldPos;
                
                return o;
            }
            
            // 片元着色器
            half4 frag(Varyings i) : SV_Target {
                // 采样纹理
                half4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}