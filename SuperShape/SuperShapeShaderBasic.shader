Shader "Hidden/BlendModes/SuperShape/Basic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTex2 ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0, 0, 0, 1)
		_Tint("Tint", float) = 0
		[PerRendererData] _GroupColor("GroupTint", Color) = (1,1,1,1)
		[PerRendererData] _GroupBGIndex("GroupBGIndex", Float) = 0

        // UI-related masking options, Unity complains if they aren't here
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
	    Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
        ColorMask [_ColorMask]

        Pass
        {
			Name "Default"

			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile NO_FADE FADE
			#pragma multi_compile NO_TEXTURE TEXTURE_COORDINATES_DOUBLE_UV TEXTURE_COORDINATES_UV TEXTURE_COORDINATES_SCREENSPACE
			#pragma multi_compile BLENDMODES_MODE_NONE BLENDMODES_MODE_PASS BLENDMODES_MODE_DARKEN BLENDMODES_MODE_MULTIPLY BLENDMODES_MODE_COLORBURN BLENDMODES_MODE_LINEARBURN BLENDMODES_MODE_DARKERCOLOR BLENDMODES_MODE_LIGHTEN BLENDMODES_MODE_SCREEN BLENDMODES_MODE_COLORDODGE BLENDMODES_MODE_LINEARDODGE BLENDMODES_MODE_LIGHTERCOLOR BLENDMODES_MODE_OVERLAY BLENDMODES_MODE_SOFTLIGHT BLENDMODES_MODE_HARDLIGHT BLENDMODES_MODE_VIVIDLIGHT BLENDMODES_MODE_LINEARLIGHT BLENDMODES_MODE_PINLIGHT BLENDMODES_MODE_HARDMIX BLENDMODES_MODE_DIFFERENCE BLENDMODES_MODE_EXCLUSION BLENDMODES_MODE_SUBTRACT BLENDMODES_MODE_DIVIDE BLENDMODES_MODE_HUE BLENDMODES_MODE_SATURATION BLENDMODES_MODE_COLOR BLENDMODES_MODE_LUMINOSITY BLENDMODES_MODE_BLACK

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#include "Assets/Plugins/BlendModes/Shaders/BlendModesCG.cginc"
			
			// UI-related masking options
			float4 _ClipRect;
			
            // values passed in from user space
			fixed4 _Color;
			float _Tint;

			#if FADE
				fixed4 _GroupColor;
				sampler2D _Background0Tex;
				sampler2D _Background1Tex;
				float _GroupBGIndex;
			#endif
            
			sampler2D _MainTex;
            float4 _MainTex_ST;
			float _MainTex_Alpha;
            sampler2D _MainTex2;
            float4 _MainTex2_ST;
			float _MainTex2_Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
				float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				#if TEXTURE_COORDINATES_SCREENSPACE
					float4 screenPosition : TEXCOORD3;
				#endif
				#if FADE
					float4 grabPos : TEXCOORD4;
				#endif
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				#if TEXTURE_COORDINATES_SCREENSPACE
					o.screenPosition = ComputeScreenPos(o.pos);
				#endif
				#if FADE
					o.grabPos = ComputeGrabScreenPos(o.pos);
				#endif
				o.color = v.color;
				o.uv = v.uv;
				o.uv2 = v.uv;
				o.worldPosition = v.vertex;
				#if TEXTURE_COORDINATES_DOUBLE_UV
					if (o.uv.x * o.uv.y >= 0)
					{
						o.uv = TRANSFORM_TEX(v.uv, _MainTex);
						o.uv2 = TRANSFORM_TEX(v.uv, _MainTex2);
					}
				#elif TEXTURE_COORDINATES_UV
					if (o.uv.x * o.uv.y >= 0)
					{
						o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					}
				#endif
				return o;
			}

			fixed4 frag (v2f o) : SV_Target
			{
				#if FADE
					half4 bg0color = tex2Dproj(_Background0Tex, o.grabPos);
					half4 bg1color = tex2Dproj(_Background1Tex, o.grabPos);
					half4 bg = bg0color * (1 - _GroupBGIndex) + bg1color * _GroupBGIndex;
					fixed4 g = _GroupColor;
					float gi = 1 - g.a;
				#endif

				fixed4 temp = o.color;
				if (o.uv.x * o.uv.y != 0)
				{
					temp = temp * (1 - _Tint) + _Color * _Tint;
					#if TEXTURE_COORDINATES_DOUBLE_UV
						fixed4 tex_c1 = tex2D(_MainTex, o.uv);
						temp.rgb = temp.rgb * (1 - _MainTex_Alpha * tex_c1.a) + tex_c1.rgb * _MainTex_Alpha * tex_c1.a;
						fixed4 tex_c2 = tex2D(_MainTex2, o.uv);
						temp.rgb = temp.rgb * (1 - _MainTex2_Alpha * tex_c2.a) + tex_c2.rgb * _MainTex2_Alpha * tex_c2.a;
					#elif TEXTURE_COORDINATES_UV
						fixed4 tex_c1 = tex2D(_MainTex, o.uv);
						temp.rgb = temp.rgb * (1 - _MainTex_Alpha * tex_c1.a) + tex_c1.rgb * _MainTex_Alpha * tex_c1.a;
					#elif TEXTURE_COORDINATES_SCREENSPACE
						float2 textureCoordinate = o.screenPosition.xy / o.screenPosition.w;
						textureCoordinate.x = textureCoordinate.x * _ScreenParams.x / _ScreenParams.y;
						textureCoordinate = TRANSFORM_TEX(textureCoordinate, _MainTex);
						fixed4 tex_c1 = tex2D(_MainTex, textureCoordinate);
						temp.rgb = temp.rgb * (1 - _MainTex_Alpha * tex_c1.a) + tex_c1.rgb * _MainTex_Alpha * tex_c1.a;
					#endif

					#if FADE && !BLENDMODES_MODE_NONE
						temp.rgb = BLENDMODES_BLEND_COLORS(bg.rgb, temp.rgb);
					#endif
				}
				#if FADE
					return fixed4(temp.r * g.r * g.a + bg.r * gi,
					              temp.g * g.g * g.a + bg.g * gi,
					              temp.b * g.b * g.a + bg.b * gi, temp.a);
				#else
					return temp;
				#endif
			}
			ENDCG
		}
	}
}
