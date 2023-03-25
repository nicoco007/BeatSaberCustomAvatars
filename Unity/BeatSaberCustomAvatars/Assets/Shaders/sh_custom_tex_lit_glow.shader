// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "BeatSaber/Tex Lit-Unlit Glow"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Tex("Texture", 2D) = "white" {}
		_Ambient("Ambient Lighting", Range(0, 1)) = 0
		_LightDir("Light Direction", Vector) = (-1,-1,0,1)
		_GlowColor ("Glow Color", Color) = (1,1,1,1)
		_Cutout("Cutout", Range(0, 1)) = 0.5
		_Glow("Glow", Range(0, 2)) = 0
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
				float3 normal : NORMAL;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
				float4 worldPos : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 normal : NORMAL;
				
                UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _Color;
			float _Glow;
			sampler2D _Tex;
			float _Cutout;
			float4 _Tex_ST;

			float4 _GlowColor;
			float _Ambient;
			float4 _LightDir;
			
			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
				o.normal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 lightDir = normalize(_LightDir.xyz) * -1.0;
				float shadow = max(dot(lightDir,i.normal),0);
				// sample the texture
				fixed4 col = _Color * tex2D(_Tex, TRANSFORM_TEX(i.uv, _Tex));

				fixed cmax = max(max(col.x, col.y), col.z);

				col = col * clamp(col * _Ambient + shadow,0.0,1.0);
				col.a = 0.0;
				
				if (cmax > _Cutout) {
					// sample the color
					col = _GlowColor;
					col.a = col.a * _Glow;
				}

				return col * i.color;
			}
			ENDCG
		}
	}
}
