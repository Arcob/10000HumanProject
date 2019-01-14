// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/LookupTableTwoWarp" {
	Properties {		
			_MainTex ("_MainTex", 2D) = "white" { }
	}

	CGINCLUDE
		#pragma target 3.0
		#include "UnityCG.cginc"

		uniform sampler2D _MainTex;

		uniform sampler2D LookupTableU1;
		uniform sampler2D LookupTableV1;
		uniform sampler2D LookupTableA1;

		uniform sampler2D LookupTableU2;
		uniform sampler2D LookupTableV2;
		uniform sampler2D LookupTableA2;
 
		float width;
		float height;

		float4 _MainTex_TexelSize;

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

		float getFloat(float4 cood)
		{
			float4 f = float4(cood.r, cood.g, cood.b, cood.a);
		
			int i = 0;
		
			for(int j = 0; j < 3; j++)
			{
				i += (int)(f[j] * 255);
			
				if(j < 2)
					i *= 100;
			}
		
			i *= 10;
			i += f[3] * 255;
	
			return ((float)i) / 10000000;
		}

		float4  frag (v2f i) : COLOR
		{	
			width = _MainTex_TexelSize.z;
			height =  _MainTex_TexelSize.w;

			float4 coodU1 = tex2D(LookupTableU1, float2(i.uv.x, i.uv.y));	
			// return float4(getFloat(coodU1), 0, 0, 1);
			float4 coodV1 = tex2D(LookupTableV1, float2(i.uv.x, i.uv.y));
			float4 coodA1 = tex2D(LookupTableA1, float2(i.uv.x, i.uv.y));
	
			/*coodU1 = tex2D(LookupTableU1, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));	
			coodV1 = tex2D(LookupTableV1, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));
			coodA1 = tex2D(LookupTableA1, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));*/
	
			float4 coodU2 = tex2D(LookupTableU2, float2(i.uv.x, i.uv.y));	
			float4 coodV2 = tex2D(LookupTableV2, float2(i.uv.x, i.uv.y));	
			float4 coodA2 = tex2D(LookupTableA2, float2(i.uv.x, i.uv.y));
	
			/*coodU2 = tex2D(LookupTableU2, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));	
			coodV2 = tex2D(LookupTableV2, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));
			coodA2 = tex2D(LookupTableA2, float2(i.uv.x + 0.5f / (int)width, i.uv.y + 0.5f / (int)height));*/
	
			float alphaA, alphaB;
	
			float2 q11A, q12A, q21A, q22A, r1A, r2A, offsetA;
			float2 q11B, q12B, q21B, q22B, r1B, r2B, offsetB;

			float4 outputQ11A, outputQ12A, outputQ21A, outputQ22A;
			float4 outputQ11B, outputQ12B, outputQ21B, outputQ22B;
	
			float4 outputR1A, outputR2A;
			float4 outputR1B, outputR2B;
	
			float4 outputA = float4(0, 0, 0, 0);
			float4 outputB = float4(0, 0, 0, 0);
			float4 output;
	
			int pos = 0;
	
			if((coodU1.r == 1 && coodU1.g == 1 && coodU1.b == 1 && coodU1.a == 1) &&
				(coodV1.r == 1 && coodV1.g == 1 && coodV1.b == 1 && coodV1.a == 1) &&
					(coodA1.r == 1 && coodA1.g == 1 && coodA1.b == 1 && coodA1.a == 1))
			{
				pos = 0;
		
				output = float4(0, 0, 0, 0);
			}
			else
			{
				pos = 1;

				width -= 1;
				height -= 1;		
		
				// tex 1
		
				offsetA.x = getFloat(coodU1);
				offsetA.y = getFloat(coodV1);
				alphaA = getFloat(coodA1);
		
				int offsetX1A = (int)(offsetA.x * (int)width);
				int offsetY1A = (int)(offsetA.y * (int)height);
		
				int offsetX2A = offsetX1A + 1;
				int offsetY2A = offsetY1A + 1;
		
				q11A.x = ((float)offsetX1A) / (int)width;
				q11A.y = ((float)offsetY1A) / (int)height;

				q21A.x = ((float)offsetX2A) / (int)width;
				q21A.y = ((float)offsetY1A) / (int)height;

				q12A.x = ((float)offsetX1A) / (int)width;
				q12A.y = ((float)offsetY2A) / (int)height;
		
				q22A.x = ((float)offsetX2A) / (int)width;
				q22A.y = ((float)offsetY2A) / (int)height;
	 
				r1A.x = offsetA.x;
				r1A.y = q11A.y;
		
				r2A.x = offsetA.x;
				r2A.y = q12A.y;
		
				// tex 2
		
				offsetB.x = getFloat(coodU2);
				offsetB.y = getFloat(coodV2);
				alphaB = getFloat(coodA2);
		
				int offsetX1B = (int)(offsetB.x * (int)width);
				int offsetY1B = (int)(offsetB.y * (int)height);
		
				int offsetX2B = offsetX1B + 1;
				int offsetY2B = offsetY1B + 1;
		
				q11B.x = ((float)offsetX1B) / (int)width;
				q11B.y = ((float)offsetY1B) / (int)height;

				q21B.x = ((float)offsetX2B) / (int)width;
				q21B.y = ((float)offsetY1B) / (int)height;

				q12B.x = ((float)offsetX1B) / (int)width;
				q12B.y = ((float)offsetY2B) / (int)height;
		
				q22B.x = ((float)offsetX2B) / (int)width;
				q22B.y = ((float)offsetY2B) / (int)height;
	 
				r1B.x = offsetB.x;
				r1B.y = q11B.y;
		
				r2B.x = offsetB.x;
				r2B.y = q12B.y;
			}
	
			outputQ11A = (pos == 1) ? tex2D(_MainTex, q11A) : outputA;
			outputQ21A = (pos == 1) ? tex2D(_MainTex, q21A) : outputA;
			outputQ12A = (pos == 1) ? tex2D(_MainTex, q12A) : outputA;
			outputQ22A = (pos == 1) ? tex2D(_MainTex, q22A) : outputA;
	
			outputQ11B = (pos == 1) ? tex2D(_MainTex, q11B) : outputB;
			outputQ21B = (pos == 1) ? tex2D(_MainTex, q21B) : outputB;
			outputQ12B = (pos == 1) ? tex2D(_MainTex, q12B) : outputB;
			outputQ22B = (pos == 1) ? tex2D(_MainTex, q22B) : outputB;
	
			if(pos == 1)
			{
				// tex 1
		
				outputR1A = (q21A.x - r1A.x) / (q21A.x - q11A.x) * outputQ11A +
					(r1A.x - q11A.x) / (q21A.x - q11A.x) * outputQ21A;
		
				outputR2A = (q21A.x - r1A.x) / (q21A.x - q11A.x) * outputQ12A +
					(r1A.x - q11A.x) / (q21A.x - q11A.x) * outputQ22A;
		
				outputA = (r2A.y - offsetA.y) / (r2A.y - r1A.y) * outputR1A +
					(offsetA.y - r1A.y) / (r2A.y - r1A.y) * outputR2A;
			
				// tex 2
		
				outputR1B = (q21B.x - r1B.x) / (q21B.x - q11B.x) * outputQ11B +
					(r1B.x - q11B.x) / (q21B.x - q11B.x) * outputQ21B;
		
				outputR2B = (q21B.x - r1B.x) / (q21B.x - q11B.x) * outputQ12B +
					(r1B.x - q11B.x) / (q21B.x - q11B.x) * outputQ22B;
		
				outputB = (r2B.y - offsetB.y) / (r2B.y - r1B.y) * outputR1B +
					(offsetB.y - r1B.y) / (r2B.y - r1B.y) * outputR2B;
		
				output = outputA * alphaA + outputB * alphaB;
			}
	
			//output = tex2D(_MainTex, float2(i.uv.x, i.uv.y));
	
			//output = outputA;
	
			return output;
		}
	ENDCG

	SubShader {
		ZTest Always
		Pass {
 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG

		}
	}
	Fallback "VertexLit"
} 
