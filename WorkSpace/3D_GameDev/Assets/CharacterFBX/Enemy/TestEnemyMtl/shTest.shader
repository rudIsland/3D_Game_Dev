Shader "Custom/shTest"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _NoiseTex("Noise Tex", 2D)="white"{}

        //가장자리 타들어가는 것의 진행도
        _EdgeCutProgress("edge cut progress", Range(0, 1)) = 0.2

        //가장자리 타들어가는 부분의 너비와 색상        
        _OutlineWidth("outline width", Range(1, 2)) = 1.9
        [HDR]_OutlineColor("outline color", Color) = (1,1,1,1)      
    }
    SubShader
    {
        Tags { "RenderType"="Transparent"  "Queue" = "Transparent"}
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;


        sampler2D _NoiseTex;
        float _EdgeCutProgress;    //가장자리 타들어가는 부분의 너비와 색상
        float _OutlineWidth;    //가장자리 타는 너비
        float4 _OutlineColor;   //타들어갈 색상

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //메인 텍스처 적용
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            //재질설정
            o.Metallic = _Metallic;
            //매끄러움 설정
            o.Smoothness = _Glossiness;

            //Noise의 텍스처 정보를 가져온다.
            fixed4 Noise = tex2D(_NoiseTex, IN.uv_NoiseTex);

            float Alpha=0;
            Alpha = step(_EdgeCutProgress, Noise.r); 

            //투명도 설정
            o.Alpha = Alpha;

            float outLine = 1 - step(_EdgeCutProgress * _OutlineWidth , Noise.r);

            o.Emission = outLine * _OutlineColor;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
