Shader "Custom/TestShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_OtherTex ("Other (RGB)", 2D) = "white" {}
		_Normal ("Normal", 2D) = "bump" {}
		_Normal2 ("Normal2", 2D) = "bump" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _OtherTex;
		sampler2D _Normal;
		sampler2D _Normal2;

		struct Input {
			float2 uv_MainTex;
			float3 position;
			float3 norm;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		
		void vert (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.position = v.vertex.xyz;
			o.norm = v.normal;
      	}
		
		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			float3 texturePos = IN.position / 4.0f;
			
			float slope = max(0.0f, 1.0f - IN.norm.y);

			float grass = 0.0f;
			float rock = 1.0f;

			if (slope < 0.1f)
			{
			    rock = 0.0f;
			    grass = 1.0f;
			}
			
			// Albedo comes from a texture tinted by color
			float3 color0 = tex2D (_MainTex, texturePos.yz).rgb;
			float3 tCol0 = rock * tex2D (_MainTex, texturePos.xz).rgb;
			float3 tCol1 = grass * tex2D (_OtherTex, texturePos.xz).rgb;
			
			float3 color1 = tCol0 + tCol1; 
			float3 color2 = tex2D(_OtherTex, texturePos.xy);
			
			
			fixed4 c = _Color;
			o.Albedo = c.rgb;
			o.Albedo *= ((color0 + color1 + color2) / 3.0f);
			if (slope >= 0.1f)
			{
				o.Normal = UnpackNormal(tex2D (_Normal, texturePos.xz));
			}
			else
			{
				o.Normal = UnpackNormal(tex2D (_Normal2, texturePos.xz));
			}
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
