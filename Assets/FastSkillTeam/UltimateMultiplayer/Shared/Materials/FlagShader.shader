Shader "Ultimate Multiplayer/Flag"
{
    Properties
    {
        _MainTex("Flag Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _WaveFrequency("Wave Frequency", Range(0, 10)) = 1
        _WaveAmplitude("Wave Amplitude", Range(0, 1)) = 0.1
        _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0, 10)) = 1
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            Cull Off

            CGPROGRAM
            #pragma surface surf Lambert vertex:vert fullforwardshadows

            sampler2D _MainTex;
            fixed4 _Color;
            float _WaveFrequency;
            float _WaveAmplitude;
            float4 _WindDirection;
            float _WindSpeed;

            struct Input
            {
                float2 uv_MainTex;
                float3 worldPos;
            };

            void vert(inout appdata_full v)
            {
                float waveOffset = _Time.y * _WindSpeed;

                // Calculate the displacement along the wind direction
                float windDisplacement = sin(waveOffset + dot(_WindDirection.xyz, v.vertex.xyz));

                // Apply vertex displacement based on the wave effect
                v.vertex.xyz += _WindDirection.xyz * windDisplacement * _WaveAmplitude;
            }

            void surf(Input IN, inout SurfaceOutput o)
            {
                fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);

                o.Albedo = texColor.rgb * _Color.rgb;
                o.Alpha = texColor.a;

                // Set emission to color for unlit appearance
                o.Emission = texColor.rgb * _Color.rgb;
            }
            ENDCG
        }
            FallBack "Diffuse"
}