Shader "Custom/StencilMask" {
    Properties {
        _MainTex ("主纹理", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { 
            "Queue" = "Geometry-100"  // 确保在其他物体之前渲染
            "RenderType" = "Opaque" 
        }
        
        Pass {
            // 关闭颜色写入，只影响模板缓冲区
            ColorMask 0
            ZWrite Off
            
            Stencil {
                Ref 2           // 模板参考值，设为2
                Comp Always     // 总是通过模板测试
                Pass Replace    // 用Ref值替换缓冲区值
                ZFail Keep      // 深度测试失败时保持原值
            }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            
            struct Varyings {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            Varyings vert (Attributes v) {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target {
                // 颜色掩码为0，所以不会实际输出颜色
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}
