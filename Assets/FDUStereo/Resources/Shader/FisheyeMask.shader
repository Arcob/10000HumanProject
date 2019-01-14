// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/FisheyeMask" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AngleDegree("AngleDegree", Float) = 90
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		// Cull Off
		ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			float _AngleDegree = 90;

			struct vertOut
			{
				float4 pos:SV_POSITION;
				float4 uv:TEXCOORD0;
			};

			vertOut vert(appdata_base v) {
				vertOut o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			float calculatePhiDegree(float2 uv)
			{
				// toCenter
				float2 uvToCenter = uv - float2(0.5, 0.5);

				float radius = length(uvToCenter); // ratio

				// float phiRadian = radius * 3.14;
				float phiDegree = radius * 180;
				// return radius; //phiDegree;
				return phiDegree;
			}

			fixed4 frag(vertOut i) : COLOR {
				fixed4 color = tex2D(_MainTex, i.uv.xy);
				// uv -> phi +Y -> +X
				float phiDegree = calculatePhiDegree(i.uv.xy);

				if(phiDegree > _AngleDegree)
				{
					color.xyzw = fixed4(0,0,0,1);
				}

				//color.xyz = phiDegree;

				return color;
			}

			ENDCG
		}
	} 
	FallBack "Diffuse"
}
