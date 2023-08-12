Shader "Unlit/sdf"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
    _SDFTex ("SDF Texture", 2D) = "black" {}
    _DepthFactor ("Depth Factor", Float) = 1
    _XOffset ("X Offset", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
      Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
      sampler2D _SDFTex;
      float4 _SDFTex_ST;
      float _DepthFactor;
      float _XOffset;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

        float depth = tex2Dlod(_SDFTex, float4(v.uv, 0, 1)).r;
        v.vertex.x += depth * _XOffset;
        v.vertex.z -= depth * _DepthFactor;

				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
        
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
