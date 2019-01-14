// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/PureTexture" {
	Properties {
		_MainTex ("_MainTex", 2D) = "white" { }
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

		float4  frag (v2f i) : COLOR
		{	
			return tex2D(_MainTex, i.uv);
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
