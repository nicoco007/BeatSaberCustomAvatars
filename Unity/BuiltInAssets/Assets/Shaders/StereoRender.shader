Shader "Beat Saber Custom Avatars/Stereo Render"
{
	Properties
	{
		_ReflectionTex("Reflection Texture", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _ReflectionTex;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 screenUV = i.screenPos.xy / i.screenPos.w;

				#ifdef UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
				screenUV = (screenUV - scaleOffset.zw) / scaleOffset.xy;
				#endif

				#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)
				return tex2D(_ReflectionTex, unity_StereoEyeIndex == 1 ? screenUV * float2(0.5, 1) + float2(0.5, 0) : screenUV * float2(0.5, 1));
				#else
				return tex2D(_ReflectionTex, screenUV);
				#endif
			}
			ENDCG
		}
	}
}
