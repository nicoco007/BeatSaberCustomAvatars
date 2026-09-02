Shader "Beat Saber Custom Avatars/Glow Overlay"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
    	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

		Pass
		{
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            Lighting Off
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

    			UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				return o;
			}
			
			fixed4 frag (v2f i/*, out float depth : SV_Depth*/) : SV_Target
			{
				// This forces the object to render as if it were very close to the camera
				// (similar to ZTest Always) but keeping some depth so convex objects still
				// mostly work. Kind of a hack but works for our purposes!
				/*#ifdef UNITY_REVERSED_Z
				depth = i.vertex.z * 0.01 + 0.99;
				#else
				depth = i.vertex.z * 0.01;
				#endif*/
				
				return _Color * tex2D(_MainTex, i.uv);
			}
			ENDCG
		}

		// this pass takes care of dealing with glow (alpha channel)
        Pass {
            ZWrite Off
            ColorMask A
            Lighting Off

            // since the glow value (alpha channel) is 0 = no glow and 1 = max glow, we want the destination value
            // (existing value in render target) to be subtracted by alpha value from this pass so it is attenuated
            // see https://docs.unity3d.com/Manual/SL-Blend.html and https://docs.unity3d.com/Manual/SL-BlendOp.html
            Blend One One
            BlendOp RevSub

            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

    			UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				
                return o;
            }

            fixed4 frag(v2f i/*, out float depth : SV_Depth*/) : SV_Target
            {
				/*#ifdef UNITY_REVERSED_Z
				depth = i.vertex.z * 0.01 + 0.99;
				#else
				depth = i.vertex.z * 0.01;
				#endif*/
                
				return float4(0, 0, 0, tex2D(_MainTex, i.uv).a * _Color.a);
            }
            ENDCG
        }
	}
}