// Made with Amplify Shader Editor v1.9.3.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "NonTranspWaterShader_BuiltIn"
{
	Properties
	{
		_WaveNormal("WaveNormal", 2D) = "white" {}
		_WaterCol("WaterCol", Color) = (0.490566,0.490566,0.490566,0)
		_Wave01_Tiling("Wave01_Tiling", Vector) = (1,1,0,0)
		_Wave02_Tiling("Wave02_Tiling", Vector) = (1,1,0,0)
		_WavOffsetMul01("WavOffsetMul01", Float) = 0.5
		_Smoothness("Smoothness", Float) = 1
		_WavOffsetMul02("WavOffsetMul02", Float) = -1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _WaveNormal;
		uniform float2 _Wave01_Tiling;
		uniform float _WavOffsetMul01;
		uniform float2 _Wave02_Tiling;
		uniform float _WavOffsetMul02;
		uniform float4 _WaterCol;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_cast_0 = (( _WavOffsetMul01 * _Time.y )).xx;
			float2 uv_TexCoord8 = i.uv_texcoord * _Wave01_Tiling + temp_cast_0;
			float2 temp_cast_2 = (( _WavOffsetMul02 * _Time.y )).xx;
			float2 uv_TexCoord9 = i.uv_texcoord * _Wave02_Tiling + temp_cast_2;
			float3 temp_output_5_0 = BlendNormals( tex2D( _WaveNormal, uv_TexCoord8 ).rgb , tex2D( _WaveNormal, uv_TexCoord9 ).rgb );
			o.Normal = temp_output_5_0;
			o.Albedo = _WaterCol.rgb;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19302
Node;AmplifyShaderEditor.SimpleTimeNode;11;-2252.626,148.8279;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;15;-2000.587,870.8;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-2265.626,271.8279;Inherit;False;Property;_WavOffsetMul01;WavOffsetMul01;4;0;Create;True;0;0;0;False;0;False;0.5;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-2276.038,804.3301;Inherit;False;Property;_WavOffsetMul02;WavOffsetMul02;6;0;Create;True;0;0;0;False;0;False;-1;-0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;10;-2022.788,553.8166;Inherit;False;Property;_Wave02_Tiling;Wave02_Tiling;3;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;4;-2059.943,-79.94569;Inherit;False;Property;_Wave01_Tiling;Wave01_Tiling;2;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-1938.626,201.8279;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;-1686.587,923.8;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;1;-1626.359,-38.43084;Inherit;True;Property;_WaveNormal;WaveNormal;0;0;Create;True;0;0;0;False;0;False;None;2c6a59b6105cda64aaba0def373cd6e4;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TextureCoordinatesNode;9;-1616.788,538.8166;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-1645.656,207.8974;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-1218.585,42.92389;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-1177.816,502.0035;Inherit;True;Property;_TextureSample1;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendNormalsNode;5;-609.8367,388.7914;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-505.7076,126.7432;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;1;1.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;7;-700.6481,-98.9404;Inherit;False;Property;_WaterCol;WaterCol;1;0;Create;True;0;0;0;False;0;False;0.490566,0.490566,0.490566,0;0.4871839,0.614333,0.6415094,0.5686275;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-282.1361,251.6586;Inherit;False;Property;_Refraction;Refraction;7;0;Create;True;0;0;0;False;0;False;0;1.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;6;-167.6479,471.1595;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;NonTranspWaterShader_BuiltIn;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;0;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;12;0;13;0
WireConnection;12;1;11;0
WireConnection;16;0;14;0
WireConnection;16;1;15;0
WireConnection;9;0;10;0
WireConnection;9;1;16;0
WireConnection;8;0;4;0
WireConnection;8;1;12;0
WireConnection;2;0;1;0
WireConnection;2;1;8;0
WireConnection;3;0;1;0
WireConnection;3;1;9;0
WireConnection;5;0;2;0
WireConnection;5;1;3;0
WireConnection;6;0;5;0
WireConnection;0;0;7;0
WireConnection;0;1;5;0
WireConnection;0;4;17;0
WireConnection;0;8;18;0
WireConnection;0;9;7;4
ASEEND*/
//CHKSM=1A680DED4E21104E01B6BDDF6569F9D24EC636CA