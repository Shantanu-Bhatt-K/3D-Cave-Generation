Shader "Custom/MarchingCubesShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Uniform variables
            StructuredBuffer<float3> verticesBuffer;
            StructuredBuffer<int> trianglesBuffer;

            struct appdata
            {
                uint vertexID : SV_VertexID;  // Unique ID for each vertex
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 color : COLOR;
            };

            float4 _Color;

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;

                // Use the vertex ID to fetch the position from the vertices buffer
                float3 vertexPosition = verticesBuffer[trianglesBuffer[v.vertexID]];

                // Transform the vertex position to clip space
                o.pos = UnityObjectToClipPos(float4(vertexPosition, 1.0));

                // Set vertex color (optional: could use normals or other data instead)
                o.color = _Color.rgb;

                return o;
            }

            // Fragment shader
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(i.color, 1.0);  // Output color
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
