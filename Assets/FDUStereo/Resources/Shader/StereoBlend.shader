// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/StereoBlend" {
	Properties {
		_LeftTexture ("_LeftTexture", 2D) = "white" { }
		_RightTexture ("_RightTexture", 2D) = "white" { }
	}

	CGINCLUDE
		#pragma target 3.0
		#include "UnityCG.cginc"

		uniform sampler2D _LeftTexture;
		uniform sampler2D _RightTexture;

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

		float4  fragRedCyan (v2f i) : COLOR
		{	
			float4 outputL = tex2D(_LeftTexture, i.uv);
			float4 outputR = tex2D(_RightTexture, i.uv);
			outputL.g = 0;
			outputL.b = 0;
			outputR.r = 0;
			float4 output = outputL + outputR;
			return output;
		}
		
		float4  fragSideBySide (v2f i) : COLOR
		{	
			float2 leftUV = i.uv;
			leftUV.x = leftUV.x * 2;
			float4 outputL = tex2D(_LeftTexture, leftUV);
			
			float2 rightUV = i.uv;
			rightUV.x = rightUV.x * 2 - 1;
			float4 outputR = tex2D(_RightTexture, rightUV);

			if(i.uv.x <= 0.5f)
			{
				outputR.xyz = float3(0, 0, 0);
			}else
			{
				outputL.xyz = float3(0, 0, 0);
			}

			return outputL + outputR;
		}

		float4 fragLeftOnly(v2f i) : COLOR
		{
			return tex2D(_LeftTexture, i.uv);
		}
		
		float4 fragRightOnly(v2f i) : COLOR
		{
			return tex2D(_RightTexture, i.uv);
		}
	ENDCG
	
	SubShader {
		ZTest Always
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragRedCyan
			ENDCG
		}
		
		ZTest Always
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragSideBySide
			ENDCG
		}
		
		ZTest Always
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragLeftOnly
			ENDCG
		}
		
		ZTest Always
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragRightOnly
			ENDCG
		}
	}
	Fallback "VertexLit"
} 
