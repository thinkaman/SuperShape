Shader "Custom/MaskShaderA"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "GrabPass"
            "PreviewType" = "Plane"
            "ForceNoShadowCasting" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
            Stencil {
                Ref 1
                Comp always
                Pass IncrSat
            }
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                };

                v2f vert(appdata_t IN)
                {
                    v2f OUT;
                    OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    return (0.0).xxxx;
                }
            ENDCG
        }
    }
}
