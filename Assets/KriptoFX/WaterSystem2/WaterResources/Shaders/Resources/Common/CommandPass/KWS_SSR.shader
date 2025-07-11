Shader "Hidden/KriptoFX/KWS/SSR"
{
	Properties { }

	SubShader
	{
		ZWrite Off
		Cull Off
		ZTest Always

		Stencil
		{
			Ref [KWS_StencilMaskValue]
			ReadMask [KWS_StencilMaskValue]
			Comp Greater
			Pass keep
		}

		HLSLINCLUDE

		#pragma multi_compile _ STEREO_INSTANCING_ON

		//#pragma multi_compile_fragment _ KWS_USE_VOLUMETRIC_LIGHT
		#pragma multi_compile_fragment _ KWS_USE_PLANAR_REFLECTION
		#pragma multi_compile_fragment _ USE_HOLES_FILLING
		#pragma multi_compile_fragment _ KWS_USE_UNDERWATER_REFLECTION
		#pragma multi_compile_fragment _ KWS_USE_AQUARIUM_RENDERING

		#include "../../Common/KWS_WaterHelpers.cginc"
		
		DECLARE_TEXTURE(_SourceRT);
		float4 _SourceRTHandleScale;
		float4 _SourceRT_TexelSize;
		uint KWS_Frame;
		uint UseScreenSpaceReflectionSky;
		
		half OutOfBoundsFade(half2 uv)
		{
			// half2 fade = 0;
			//	fade.x = saturate(1 - abs(uv.x - 0.5) * 2);
			//	fade.y = saturate(1 - abs(uv.y - 0.5) * 2);
			//return saturate(fade.x * fade.y * 10);
			float fringeY = 1 - uv.y;
			float fringeX = fringeY * (1 - abs(uv.x * 2 - 1)) * 100;
			fringeY = fringeY * 10;
			return saturate(fringeY) * saturate(fringeX);
		}


		inline float4 GetScreenSpaceReflectionRayMarched(float3 worldPos, float3 worldNormal, float3 worldViewDir, float3 worldReflectionDir, float2 screenUV, float steps)
		{
			float2 sampleUV = 0;
			float alpha = 0;
			
			float3 currentPos = worldPos;
			float fresnel = 1 - saturate(KWS_Pow10(1.5*dot(worldViewDir, worldNormal)));
			float L = 0.5;
			
			UNITY_LOOP
			for (float i = 0; i < steps; i++)
			{
				currentPos = worldPos + worldReflectionDir * L;
				float4 newScreenPos = WorldPosToScreenPos(currentPos);
				sampleUV = newScreenPos.xy;
				
				float newScreenZ = newScreenPos.z / newScreenPos.w;
				//if(sampleUV.x < 0.0001 || sampleUV.x > 0.999 || sampleUV.y < 0.001 || sampleUV.y > 0.999) return 0;

				float depth = GetSceneDepth(sampleUV);
				float3 depthWorldPos = GetWorldSpacePositionFromDepth(sampleUV, depth);
				float rayLength = length(worldPos - depthWorldPos);
				if (!UseScreenSpaceReflectionSky && LinearEyeDepthUniversal(newScreenZ) > LinearEyeDepthUniversal(depth) + rayLength)  return 0;

				L = rayLength;
			}
			if(sampleUV.x < 0.0001 || sampleUV.x > 0.999 || sampleUV.y < 0.001 || sampleUV.y > 0.999) return 0;
			
			return float4(sampleUV, L, fresnel * OutOfBoundsFade(sampleUV));
		}

		struct vertexInput
		{
			uint vertexID : SV_VertexID;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct vertexOutput
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		vertexOutput vert(vertexInput v)
		{
			vertexOutput o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.vertex = GetTriangleVertexPosition(v.vertexID);
			o.uv = GetTriangleUVScaled(v.vertexID);
			return o;
		}

		ENDHLSL


		//pre pass
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.6
			#pragma editor_sync_compilation

			half4 frag(vertexOutput i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = i.uv;

				#ifdef KWS_USE_UNDERWATER_REFLECTION
					float mask = GetWaterMask(uv);
					if (mask > 0.99) return 0;
				#endif

			
				float waterDepth = GetWaterDepth(uv);
				//if (waterDepth == 0) discard; //avoid skylight leaking

				float3 waterWorldPos = GetWorldSpacePositionFromDepth(uv, waterDepth);
				float3 waterNormals = float3(0, 1, 0);
				//waterNormals = GetWaterNormals(uv);

				
				float3 viewDir = GetWorldSpaceViewDirNorm(waterWorldPos);
				float3 reflDir = reflect(-viewDir, waterNormals);

				//return float4(waterNormals, 1);

				float4 rayMarchData = GetScreenSpaceReflectionRayMarched(waterWorldPos, waterNormals, viewDir, reflDir, uv, 5);
				if (rayMarchData.w < 0.0001) return 0;

				float3 ssrColor = GetSceneColor(rayMarchData.xy);
				

				#ifdef KWS_USE_UNDERWATER_REFLECTION
				
					float waterHeight = KWS_WaterPosition.y;
					float transparent = KWS_Transparent;
					float3 turbidityColor = KWS_TurbidityColor;
					float3 waterColor = KWS_WaterColor;

					transparent = clamp(transparent + KWS_UnderwaterTransparentOffset, 1, KWS_MAX_TRANSPARENT * 2);
					
					//float3 volLight = GetVolumetricLightWithAbsorbtionByDistance(uv, uv, transparent, turbidityColor, waterColor, ssrColor, rayMarchData.z, GetExposure(), 0).xyz;
					//ssrColor = lerp(ssrColor, volLight, GetUnderwaterSurfaceMask(mask));
				#endif

				return float4(clamp(ssrColor, 0, 2), rayMarchData.a);
			}

			ENDHLSL
		}
	}
}