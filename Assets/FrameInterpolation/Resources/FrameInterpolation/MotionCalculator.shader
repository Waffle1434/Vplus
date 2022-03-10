Shader "Unlit/MotionCalculator" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Next("Next",2D)="black"{}
		_Previous("Previous",2D)="black"{}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
		cull off
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #include "UnityCG.cginc"
			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Previous, _Next;
			float4 _MainTex_TexelSize;
			
            half PixV(sampler2D img, half2 uv) {
				fixed4 c = tex2D(img,uv);
				c += tex2D(img, uv + (half2(1, 0)*_MainTex_TexelSize.xy));
				c += tex2D(img, uv + (half2(0, 1)*_MainTex_TexelSize.xy));
				c += tex2D(img, uv + (half2(-1, 0)*_MainTex_TexelSize.xy));
				c += tex2D(img, uv + (half2(0, -1)*_MainTex_TexelSize.xy));
				return c.r + c.g + c.b;
			}
            
			v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0) {
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = uv;
				return o;
			}
            
			fixed4 frag (v2f i) : SV_Target {
				half prevValue = PixV(_Previous,i.uv);
                half nextValue = PixV(_Next,i.uv);
				half minDiff = abs(prevValue-nextValue);
                float2 finalOffset;
                
				for (int cicleR=1; cicleR<4; cicleR++) {
                    int circleSegments = 4*cicleR;
                    half angInc = UNITY_TWO_PI / circleSegments;
                    
					for (int j=0; j<circleSegments; j++) {
                        half ang = j * angInc;
                        float2 offset = half2(sin(ang),cos(ang))*_MainTex_TexelSize.xy*cicleR;// TODO: precompute offsets
                        half v = PixV(_Next, i.uv + offset);
                        half diff = abs(prevValue-v);
                        
						if (diff < minDiff) {
							minDiff = diff;
							finalOffset = offset;
						}
					}
				}
                
				return fixed4(finalOffset,0,1);
			}
			ENDCG
		}
	}
}
