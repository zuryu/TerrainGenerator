// Constants
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float xAmbient;
bool xEnableLighting;


// Texture Samplers

Texture xTexture0;
sampler TextureSampler0 = sampler_state { texture = <xTexture0>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

Texture xTexture1;
sampler TextureSampler1 = sampler_state { texture = <xTexture1>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

Texture xTexture2;
sampler TextureSampler2 = sampler_state { texture = <xTexture2>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

Texture xTexture3;
sampler TextureSampler3 = sampler_state { texture = <xTexture3>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror; };

// MultiTexture
struct MTVertexToPixel
{
	float4 Position         : POSITION;
	float4 Color            : COLOR0;
	float3 Normal            : TEXCOORD0;
	float2 TextureCoords    : TEXCOORD1;
	float4 LightDirection    : TEXCOORD2;
	float4 TextureWeights    : TEXCOORD3;
};

MTVertexToPixel MultiTexturedVS(float4 inPos : POSITION, float3 inNormal : NORMAL, float2 inTexCoords : TEXCOORD0, float4 inTexWeights : TEXCOORD1)
{
	MTVertexToPixel Output = (MTVertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
		float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
	Output.Normal = mul(normalize(inNormal), xWorld);
	Output.TextureCoords = inTexCoords;
	Output.LightDirection.xyz = -xLightDirection;
	Output.LightDirection.w = 1;
	Output.TextureWeights = inTexWeights;

	return Output;
}

struct MTPixelToFrame
{
	float4 Color : COLOR0;
};

MTPixelToFrame MultiTexturedPS(MTVertexToPixel PSIn)
{
	MTPixelToFrame Output = (MTPixelToFrame)0;

	float lightingFactor = 1;
	if (xEnableLighting)
		lightingFactor = saturate(saturate(dot(PSIn.Normal, PSIn.LightDirection)) + xAmbient);

	Output.Color = tex2D(TextureSampler0, PSIn.TextureCoords)*PSIn.TextureWeights.x;
	Output.Color += tex2D(TextureSampler1, PSIn.TextureCoords)*PSIn.TextureWeights.y;
	Output.Color += tex2D(TextureSampler2, PSIn.TextureCoords)*PSIn.TextureWeights.z;
	Output.Color += tex2D(TextureSampler3, PSIn.TextureCoords)*PSIn.TextureWeights.w;

	Output.Color *= lightingFactor;

	return Output;
}

technique MultiTextured
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 MultiTexturedVS();
		PixelShader = compile ps_2_0 MultiTexturedPS();
	}
}