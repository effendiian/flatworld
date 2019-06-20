Shader "Custom/combinedVoxelSurf"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_GrassColor("Grass Color", Color) = (1,1,1,1)
		_DirtColor("Dirt Color", Color) = (1,1,1,1)
		_StoneColor("Stone Color", Color) = (1,1,1,1)
		_BedrockColor("Bedrock Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
		#include "Assets\Packages\RetroAA\RetroAA.cginc"

        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		struct VertIn {
			float4 vertex    : POSITION;
			float3 normal    : NORMAL;
			float4 texcoord  : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
		};
        struct Input
        {
            float2 uv_MainTex;
			fixed cust1434;
        };

        half _Glossiness, _Metallic;
        fixed4 _GrassColor, _DirtColor, _StoneColor, _BedrockColor;

		void vert(inout VertIn v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.cust1434 = v.texcoord1;
		}

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
			fixed4 c = RetroAA(_MainTex, IN.uv_MainTex, _MainTex_TexelSize);

			switch (round(IN.cust1434)) {
				case 1: c *= _GrassColor; break;
				case 2: c *= _DirtColor; break;
				case 3: c *= _StoneColor; break;
				case 4: c *= _BedrockColor; break;
			}

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
