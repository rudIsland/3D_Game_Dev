/*

    림 라이트Rim Light(역광) 효과

    물체의 가장자리 경계를 따라 밝게 빛나는 효과

*/



Shader "Custom/shRimLight"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)

        //림라이트효과 두께
        _RimLightWidth("rimlight width", Range(0,10)) = 4
        //림라이트효과 색상
        [HDR]_RimLightColor("rimlight color", Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        
        //렌더링 파이프라인에서 전달받은 데이터의 구조체 정의의 선언
        struct Input
        {
            //float2 uv_MainTex;
            float3 viewDir; //렌더링 파이프라인에서 전달받을 시선벡터
        };


        fixed4 _Color;

        fixed _RimLightWidth;
        fixed4 _RimLightColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb;

            //N dot V       법선벡터 dot 시선벡터
            //              시선벡터는 유니티에 의해 뒤집어져 주어진다.
            fixed tDot = dot(o.Normal, IN.viewDir); //내적값을 구한다. [-1,1]사이값이 나옴
            tDot = saturate(tDot); //[0,1]로 값을 제한시킴

            tDot = 1 - tDot;    //OneMinus
            fixed tRim = pow(tDot, _RimLightWidth); //거듭제곱 소수점이라 곱할수록 0에 가까워져서 연해짐 


            //물체 자체가 빛나는 emisson을 이용하여 표현하겠다.
            o.Emission = tRim * _RimLightColor;

            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
