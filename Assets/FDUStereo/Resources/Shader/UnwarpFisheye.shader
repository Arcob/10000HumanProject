// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/UnwarpFisheye" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	CGINCLUDE
		#pragma target 3.0
		#include "UnityCG.cginc"

		uniform sampler2D _MainTex;

		struct a2v {
			float4 vertex : POSITION;
			float2 texCoord		: TEXCOORD0; 
		};

		struct v2f {
			float4 vertex : POSITION; 
			float2  uv : TEXCOORD0;
		};

		v2f vert (a2v v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);	
			o.uv = v.texCoord;
			return o;
		}


		float3 cast2Sphere(float2 uv){
			float PI = 3.141592654;

			float lat = uv.y - 0.5;
			lat = lat *  PI;

			float lg = uv.x;
			lg = lg * PI;	// -X -> +X

			float R = 1;

			float3 pt;
			pt.y = R * sin(lat); 
			
			float magPrj = R * cos(lat);
			pt.x = magPrj * -cos(lg); 
			pt.z = magPrj * sin(lg);

			return pt;
		}

		float2 map2Fisheye(float3 ptr){
			float2 prjXoY = float2(ptr.x, ptr.y);
			float mag = length(prjXoY);
			
			// assert float R = 1;
			float cosPhi = clamp(ptr.z, -1, 1);
			float phi = acos(cosPhi);
			
			float PI = 3.141592654;
			float radiusXoY = 1 * phi / PI;	// why 1
			
			bool isZero  = (0 == mag);
			float cosLg = 0;
			float sinLg = 0;
			if(!isZero){
				cosLg = ptr.x / mag;
				sinLg = ptr.y / mag;
			}

			float2 uv;
			uv.x = radiusXoY * cosLg + 0.5;
			uv.y = radiusXoY * sinLg + 0.5;

			return uv;
		}


		float2 unwarpFisheyeUV(float2 uv){
			float3 ptr = cast2Sphere(uv);
			float2 uvFisheye = map2Fisheye(ptr);

			return uvFisheye;
		}

		float4  frag (v2f i) : COLOR
		{	
			//float3 ptr = cast2Sphere(i.uv);
			//if(ptr.x <=  -0.99999){
			//	return float4(1, 1, 1, 1);	
			//}
			//return float4(ptr.x, 0, 0, 1);

			float2 uv = unwarpFisheyeUV(i.uv);
			//return float4(uv.y, 0, 0, 1);
			return tex2D(_MainTex, uv);
		}
	ENDCG

	SubShader {
		//Tags { "RenderType"="Opaque" }
		//LOD 200

		ZTest Always
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
