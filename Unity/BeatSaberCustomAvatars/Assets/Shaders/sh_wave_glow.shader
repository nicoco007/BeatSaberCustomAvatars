Shader "BeatSaber/Unlit Wave"
{
	Properties
	{
		_Color ("Color 1", Color) = (1,1,1,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Tex2("Texture 2", 2D) = "white" {}
		_Speed ("Speed", Float) = 1
		_Freq ("Frequency", Float) = 3
		_Bloom ("Bloom", Range (0, 1)) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
				
                UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _Color;
			float4 _Color2;
			float _Speed;
			float _Freq;
			float _Bloom;

			sampler2D _Tex2;
			float4 _Tex2_ST;
			
			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = lerp(_Color,_Color2 * tex2D(_Tex2, TRANSFORM_TEX(i.uv, _Tex2)),sin(_Time.y*_Speed + i.uv.x * _Freq)/2.0 + 1.0);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col * float4(1.0,1.0,1.0,_Bloom) * i.color;
			}
			ENDCG
		}
	}
}
