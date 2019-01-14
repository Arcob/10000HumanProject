// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'glstate.matrix.mvp' with 'UNITY_MATRIX_MVP'

Shader "Tutorial/PortalMappingLB" {
Properties {		
		_MainTex ("_MainTex", 2D) = "white" { }
		width("ltWidth", Float) = 0
		height("ltWidth", Float) = 0
}

SubShader {
		ZTest Always
    Pass {
 
CGPROGRAM
#pragma target 3.0
//#pragma only_renderers d3d9
//#pragma profileoption MaxTexIndirections=1024
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _MainTex;

uniform sampler2D LookupTableU1;
uniform sampler2D LookupTableV1;
uniform sampler2D LookupTableA1;

uniform sampler2D LookupTableU2;
uniform sampler2D LookupTableV2;
uniform sampler2D LookupTableA2;

uniform sampler2D LookupTableU3;
uniform sampler2D LookupTableV3;
uniform sampler2D LookupTableA3;

uniform sampler2D LookupTableU4;
uniform sampler2D LookupTableV4;
uniform sampler2D LookupTableA4;
 
float width;
float height;

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
	float4 coodU1 = tex2D(LookupTableU1, float2(i.uv.x, i.uv.y));	
	float4 coodV1 = tex2D(LookupTableV1, float2(i.uv.x, i.uv.y));
	float4 coodA1 = tex2D(LookupTableA1, float2(i.uv.x, i.uv.y));
	
	float4 coodU2 = tex2D(LookupTableU2, float2(i.uv.x, i.uv.y));	
	float4 coodV2 = tex2D(LookupTableV2, float2(i.uv.x, i.uv.y));	
	float4 coodA2 = tex2D(LookupTableA2, float2(i.uv.x, i.uv.y));
	
	float4 coodU3 = tex2D(LookupTableU3, float2(i.uv.x, i.uv.y));	
	float4 coodV3 = tex2D(LookupTableV3, float2(i.uv.x, i.uv.y));	
	float4 coodA3 = tex2D(LookupTableA3, float2(i.uv.x, i.uv.y));
	
	float4 coodU4 = tex2D(LookupTableU4, float2(i.uv.x, i.uv.y));	
	float4 coodV4 = tex2D(LookupTableV4, float2(i.uv.x, i.uv.y));	
	float4 coodA4 = tex2D(LookupTableA4, float2(i.uv.x, i.uv.y));
	
	float alphaA, alphaB, alphaC, alphaD;
	
	float2 q11A, q12A, q21A, q22A, r1A, r2A, offsetA;
	float2 q11B, q12B, q21B, q22B, r1B, r2B, offsetB;
	float2 q11C, q12C, q21C, q22C, r1C, r2C, offsetC;
	float2 q11D, q12D, q21D, q22D, r1D, r2D, offsetD;

	float4 outputQ11A, outputQ12A, outputQ21A, outputQ22A;
	float4 outputQ11B, outputQ12B, outputQ21B, outputQ22B;
	float4 outputQ11C, outputQ12C, outputQ21C, outputQ22C;
	float4 outputQ11D, outputQ12D, outputQ21D, outputQ22D;
	
	float4 outputR1A, outputR2A;
	float4 outputR1B, outputR2B;
	float4 outputR1C, outputR2C;
	float4 outputR1D, outputR2D;
	
	float4 outputA, outputB, outputC, outputD, output; 
	
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
		
	// tex 3
		
	offsetC.x = getFloat(coodU3);
	offsetC.y = getFloat(coodV3);
	alphaC = getFloat(coodA3);
		
	int offsetX1C = (int)(offsetC.x * (int)width);
	int offsetY1C = (int)(offsetC.y * (int)height);
		
	int offsetX2C = offsetX1C + 1;
	int offsetY2C = offsetY1C + 1;
		
	q11C.x = ((float)offsetX1C) / (int)width;
	q11C.y = ((float)offsetY1C) / (int)height;

	q21C.x = ((float)offsetX2C) / (int)width;
	q21C.y = ((float)offsetY1C) / (int)height;

	q12C.x = ((float)offsetX1C) / (int)width;
	q12C.y = ((float)offsetY2C) / (int)height;
		
	q22C.x = ((float)offsetX2C) / (int)width;
	q22C.y = ((float)offsetY2C) / (int)height;
	 
	r1C.x = offsetC.x;
	r1C.y = q11C.y;
		
	r2C.x = offsetC.x;
	r2C.y = q12C.y;
		
	// tex 2
		
	offsetD.x = getFloat(coodU4);
	offsetD.y = getFloat(coodV4);
	alphaD = getFloat(coodA4);
		
	int offsetX1D = (int)(offsetD.x * (int)width);
	int offsetY1D = (int)(offsetD.y * (int)height);
		
	int offsetX2D = offsetX1D + 1;
	int offsetY2D = offsetY1D + 1;
		
	q11D.x = ((float)offsetX1D) / (int)width;
	q11D.y = ((float)offsetY1D) / (int)height;

	q21D.x = ((float)offsetX2D) / (int)width;
	q21D.y = ((float)offsetY1D) / (int)height;

	q12D.x = ((float)offsetX1D) / (int)width;
	q12D.y = ((float)offsetY2D) / (int)height;
		
	q22D.x = ((float)offsetX2D) / (int)width;
	q22D.y = ((float)offsetY2D) / (int)height;
	 
	r1D.x = offsetD.x;
	r1D.y = q11D.y;
		
	r2D.x = offsetD.x;
	r2D.y = q12D.y;
	
	outputQ11A =  tex2D(_MainTex, q11A);
	outputQ21A =  tex2D(_MainTex, q21A);
	outputQ12A =  tex2D(_MainTex, q12A);
	outputQ22A =  tex2D(_MainTex, q22A);
	
	outputQ11B = tex2D(_MainTex, q11B);
	outputQ21B = tex2D(_MainTex, q21B);
	outputQ12B = tex2D(_MainTex, q12B);
	outputQ22B = tex2D(_MainTex, q22B);
	
	outputQ11C = tex2D(_MainTex, q11C);
	outputQ21C = tex2D(_MainTex, q21C);
	outputQ12C = tex2D(_MainTex, q12C);
	outputQ22C = tex2D(_MainTex, q22C);
	
	outputQ11D = tex2D(_MainTex, q11D);
	outputQ21D = tex2D(_MainTex, q21D);
	outputQ12D = tex2D(_MainTex, q12D);
	outputQ22D = tex2D(_MainTex, q22D);
	
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
		
	// tex 3
		
	outputR1C = (q21C.x - r1C.x) / (q21C.x - q11C.x) * outputQ11C +
		(r1C.x - q11C.x) / (q21C.x - q11C.x) * outputQ21C;
		
	outputR2C = (q21C.x - r1C.x) / (q21C.x - q11C.x) * outputQ12C +
		(r1C.x - q11C.x) / (q21C.x - q11C.x) * outputQ22C;
		
	outputC = (r2C.y - offsetC.y) / (r2C.y - r1C.y) * outputR1C +
		(offsetC.y - r1C.y) / (r2C.y - r1C.y) * outputR2C;
			
	// tex 4
		
	outputR1D = (q21D.x - r1D.x) / (q21D.x - q11D.x) * outputQ11D +
		(r1D.x - q11D.x) / (q21D.x - q11D.x) * outputQ21D;
		
	outputR2D = (q21D.x - r1D.x) / (q21D.x - q11D.x) * outputQ12D +
		(r1D.x - q11D.x) / (q21D.x - q11D.x) * outputQ22D;
		
	outputD = (r2D.y - offsetD.y) / (r2D.y - r1D.y) * outputR1D +
		(offsetD.y - r1D.y) / (r2D.y - r1D.y) * outputR2D;
		
	output = outputA * alphaA + outputB * alphaB + outputC * alphaC + outputD * alphaD;
	
	return output;
}

ENDCG

    }
}
Fallback "VertexLit"
} 
