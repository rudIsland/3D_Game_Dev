/*

    �� ����ƮRim Light(����) ȿ��

    ��ü�� �����ڸ� ��踦 ���� ��� ������ ȿ��

*/



Shader "Custom/shRimLight"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)

        //������Ʈȿ�� �β�
        _RimLightWidth("rimlight width", Range(0,10)) = 4
        //������Ʈȿ�� ����
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
        
        //������ ���������ο��� ���޹��� �������� ����ü ������ ����
        struct Input
        {
            //float2 uv_MainTex;
            float3 viewDir; //������ ���������ο��� ���޹��� �ü�����
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

            //N dot V       �������� dot �ü�����
            //              �ü����ʹ� ����Ƽ�� ���� �������� �־�����.
            fixed tDot = dot(o.Normal, IN.viewDir); //�������� ���Ѵ�. [-1,1]���̰��� ����
            tDot = saturate(tDot); //[0,1]�� ���� ���ѽ�Ŵ

            tDot = 1 - tDot;    //OneMinus
            fixed tRim = pow(tDot, _RimLightWidth); //�ŵ����� �Ҽ����̶� ���Ҽ��� 0�� ��������� ������ 


            //��ü ��ü�� ������ emisson�� �̿��Ͽ� ǥ���ϰڴ�.
            o.Emission = tRim * _RimLightColor;

            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
