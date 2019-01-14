// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/SpherePixelWarp"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_row("Row", int) = 8
		_column("Column", int) = 8
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			// // make fog work
			// #pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			struct Proxy
			{
				float3 eye;
				float3 pt;
				float3 normal;
				float3 leftBottom;
				float3 rightBottom;
				float3 rightTop;
				float3 leftTop;
				float rowIndex;
				float columnIndex;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			int _row;
			int _column;

			float2 uv2LongLat(float2 uv_){
				float longitude = uv_.x * 180;
				float latitude = uv_.y * 180 - 90;
				return float2(longitude, latitude);
			}

			float2 portal2LongLat(float2 uv_){
				return uv_;
			}

			float3 longLat2Sphere(float2 longLat_){

				float degree2Radian = 1.0 / 180.0 * 3.141592654;
				longLat_.x *= degree2Radian;
				longLat_.y *= degree2Radian;

				float longitude = longLat_.x;
				float latitude = longLat_.y;

				float radius = 1;

				float cosLat = cos(latitude);
				float sinLat = sin(latitude);

				float y = sinLat * radius;
				float magXoZ = cosLat * radius;
				float x = -cos(longitude) * magXoZ;
				float z = sin(longitude) * magXoZ;

				return float3(x, y, z);
			}

			Proxy rowColumn2Proxy(float rowIndex, float columnIndex){

				int row = _row;
				int column = _column;

				float longFov = 180;
				float latFov = 180;

				float longStrideFov = longFov / column;
				float latStrideFov = latFov / row;

				float startLongitude = columnIndex * longStrideFov;
				float endLongitude = startLongitude + longStrideFov;

				float startLatitude = rowIndex * latStrideFov - 90;
				float endLatitude = startLatitude + latStrideFov;

				float3 leftBottom = longLat2Sphere(float2(startLongitude, startLatitude));
				float3 rightBottom = longLat2Sphere(float2(endLongitude, startLatitude));
				float3 rightTop = longLat2Sphere(float2(endLongitude, endLatitude));
				float3 leftTop = longLat2Sphere(float2(startLongitude, endLatitude));

				float3 center = (leftBottom + rightBottom + rightTop + leftTop) / 4;

				float3 bottomLeft2Right = rightBottom - leftBottom;
				float bottomLength = length(bottomLeft2Right);

				float3 topLeft2Right = rightTop - leftTop;
				float topLength = length(topLeft2Right);

				float3 middleBottom2Top = (leftTop + rightTop) / 2 - (leftBottom + rightBottom) / 2;
				float3 proxyNormal = float3(0,0,0);
				if(bottomLength > topLength){
					proxyNormal = cross(bottomLeft2Right, middleBottom2Top);
				}else{
					proxyNormal = cross(topLeft2Right, middleBottom2Top);
				}

				Proxy proxy;
				proxy.eye = float3(0,0,0);
				proxy.normal = normalize(proxyNormal);
				proxy.pt = center;
				proxy.leftBottom = leftBottom;
				proxy.rightBottom = rightBottom;
				proxy.rightTop = rightTop;
				proxy.leftTop = leftTop;

				proxy.rowIndex = rowIndex;
				proxy.columnIndex = columnIndex;
				return proxy;
			}

			Proxy longLat2Proxy(float2 longLat_){
				// assert longitude [0, 180], latitude [-90, 90]
				float longitude = longLat_.x;
				float latitude = longLat_.y + 90;

				int row = _row;
				int column = _column;

				float longFov = 180;
				float latFov = 180;

				float longStrideFov = longFov / column;
				float latStrideFov = latFov / row;

				// [floor](http://http.developer.nvidia.com/Cg/floor.html)
				// [clamp](http://http.developer.nvidia.com/Cg/clamp.html)
				float columnIndex = floor(longitude / longStrideFov);
				columnIndex = clamp(columnIndex, 0, column - 1);

				float rowIndex = floor(latitude / latStrideFov);
				rowIndex = clamp(rowIndex, 0, row - 1);

				return rowColumn2Proxy(rowIndex, columnIndex);
			}

			float3 castProxy(float3 pt_, float3 eye_, float3 proxyNormal_, float3 proxyPoint_){
				float3 rayDir = pt_ - eye_;
				rayDir = normalize(rayDir);

				float t = (dot(proxyNormal_, proxyPoint_) - dot(proxyNormal_, eye_)) / dot(proxyNormal_, rayDir);
				float3 castPoint = t * rayDir + eye_;
				return castPoint;
			}

			float2 castProxyUniform(float3 pointOnSphere_, Proxy proxy_){
				float3 pointOnProxy = castProxy(pointOnSphere_, proxy_.eye, proxy_.normal, proxy_.pt);

				float3 leftBottom = proxy_.leftBottom;
				float3 rightBottom = proxy_.rightBottom;
				float3 rightTop = proxy_.rightTop;
				float3 leftTop = proxy_.leftTop;

				float3 bottomLeft2Right = rightBottom - leftBottom;
				float bottomLength = length(bottomLeft2Right);

				float3 topLeft2Right = rightTop - leftTop;
				float topLength = length(topLeft2Right);

				float3 middleBottom2Top = (leftTop + rightTop) / 2 - (leftBottom + rightBottom) / 2;
				float height = length(middleBottom2Top);

				float2 uv = float2(0,0);
				if(bottomLength > topLength){
					float3 fromLeftBottom = pointOnProxy - leftBottom;
					uv.x = dot(fromLeftBottom, bottomLeft2Right) / bottomLength / bottomLength;
					uv.y = dot(fromLeftBottom, middleBottom2Top) / height / height;
				}else{
					float3 fromLeftTop = pointOnProxy - leftTop;
					uv.x = dot(fromLeftTop, topLeft2Right) / topLength / topLength;				
					uv.y = 1 + dot(fromLeftTop, middleBottom2Top) / height / height;
				}

				return uv;
			}

			float2 castProxy(float3 pointOnSphere_, Proxy proxy_){
				float2 uv = castProxyUniform(pointOnSphere_, proxy_);

				float columnIndex = proxy_.columnIndex;
				float rowIndex = proxy_.rowIndex;

				int row = _row;
				int column = _column;

				if(uv.y < 0 && rowIndex > 0){
					rowIndex -= 1;
					Proxy proxy2 = rowColumn2Proxy(rowIndex, columnIndex);
					uv = castProxyUniform(pointOnSphere_, proxy2);
				}else if(uv.y > 1 && rowIndex < row - 1){
					rowIndex += 1;
					Proxy proxy2 = rowColumn2Proxy(rowIndex, columnIndex);
					uv = castProxyUniform(pointOnSphere_, proxy2);
				}

				float uStride = 1.0 / column;
				float vStride = 1.0 / row;

				uv.x *= uStride;
				uv.y *= vStride;
				uv.x += uStride * columnIndex;
				uv.y += vStride * rowIndex;
				
				return uv;
			}

			float2 castOnce(float2 uv_){
				float2 longLat = uv2LongLat(uv_);
				float3 pointOnSphere = longLat2Sphere(longLat);

				Proxy proxy = longLat2Proxy(longLat);

				return castProxy(pointOnSphere, proxy);
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				// UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float2 uv = castOnce(i.uv);
				fixed4 col = tex2D(_MainTex, uv);

 				// apply fog
				// UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
