// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SH/ProjectorCorrection" {
	Properties {
			_MainTex ("MainTex", 2D) = "white" { }
			_AlphaTex ("AlphaTex", 2D) = "white" { }
			_Tex1 ("Tex1", 2D) = "white" { }
			_Tex2 ("Tex2", 2D) = "white" { }
			_Tex3 ("Tex3", 2D) = "white" { }
			_lowR ("lowR", float) =0.0
			_lowG ("lowG", float) =0.0
			_lowB ("lowB", float) =0.0
			_highR ("highR", float) =0.0
			_highG ("highG", float) =0.0
			_highB ("highB", float) =0.0		
	}
	
	SubShader {
		ZTest Always
		Pass {

			CGPROGRAM
			#pragma target 3.0
			#pragma profileoption MaxTexIndirections=1024
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _AlphaTex;
			uniform sampler2D _Tex1;
			uniform sampler2D _Tex2;
			uniform sampler2D _Tex3;
			float _lowR;
			float _lowG;
			float _lowB;
			float _highR;
			float _highG;
			float _highB;

			float3 function0(float3 rgb)
			{
				float3 output; 	
				output.r = tex2D(_Tex1,float2(rgb.r,0.5)).r;	
				output.g = tex2D(_Tex1,float2(rgb.g,0.5)).g; 	
				output.b = tex2D(_Tex1,float2(rgb.b,0.5)).b;    
				return output;
			}

			float3 function1(float3 rgb)
			{
				float3 output;
				float lumirangeR = _highR-_lowR;
				output.r = rgb.r*lumirangeR+_lowR;    
				float lumirangeG = _highG-_lowG;
				output.g = rgb.g*lumirangeG+_lowG;    
				float lumirangeB = _highB-_lowB;
				output.b = rgb.b*lumirangeB+_lowB;    
				return output;
			}

			float3 function2(float3 rgb)
			{
				float3 output;	
				output.r = tex2D(_Tex2,float2(rgb.r,0.5)).r;	
				output.g = tex2D(_Tex2,float2(rgb.g,0.5)).g; 	
				output.b = tex2D(_Tex2,float2(rgb.b,0.5)).b;    
				return output;
			}

			float3 function3(float3 itfvalue)
			{
				float3 output;	
				output.r = tex2D(_Tex3,float2(itfvalue.r,0.5)).r;	
				output.g = tex2D(_Tex3,float2(itfvalue.g,0.5)).g; 	
				output.b = tex2D(_Tex3,float2(itfvalue.b,0.5)).b;	
				return output;
			}

			struct a2v {
				float4 vertex: POSITION;
				float2 texCoord: TEXCOORD0; 
				float2 texCoord1: TEXCOORD1; 
			};

			struct v2f {
				float4 vertex: POSITION; 
				float2  uv: TEXCOORD0;
				float2  uv1: TEXCOORD1;
			};

			v2f vert (a2v v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);	
				o.uv = v.texCoord;
				o.uv1 = v.texCoord1;
				return o;
			}

			half4  frag (v2f i) : COLOR
			{
				half4 output = tex2D(_MainTex, i.uv);

				// -------- exclude by SH
				// output.rgb = function0(output.rgb);
				// function1(output.rgb);
				// output.rgb = function2(output.rgb);
				// --------- end

				float4 alpha = tex2D(_AlphaTex, i.uv1);	
				output = output * alpha;
				
				// -------- exclude by SH
				// output.rgb = function3(output.rgb);

				return output;
			}
			ENDCG
		}
	}
	Fallback "VertexLit"
} 
