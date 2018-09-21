#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Matrix
float4x4 World;
float4x4 View;
float4x4 Projection;

// Light related
float4 AmbientColor;
float AmbientIntensity;

float3 LightDirection;
float4 DiffuseColor;
float DiffuseIntensity;

float4 SpecularColor;
float3 EyePosition;

texture2D ColorMap;
sampler2D ColorMapSampler = sampler_state
{
	Texture = <ColorMap>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
};

texture2D NormalMap;
sampler2D NormalMapSampler = sampler_state
{
	Texture = <NormalMap>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
};

texture2D SpecularMap;
sampler2D SpecularMapSampler = sampler_state
{
	Texture = <SpecularMap>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
};

// The input for the VertexShader
struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float3 Normal : NORMAL0;
	float3 Binormal : BINORMAL0;
	float3 Tangent : TANGENT0;
};

// The output from the vertex shader, used for later processing
struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float3 View : TEXCOORD1;
	float3x3 WorldToTangentSpace : TEXCOORD2;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
	output.TexCoord = input.TexCoord;

	output.WorldToTangentSpace[0] = mul(normalize(input.Tangent), World);
	output.WorldToTangentSpace[1] = mul(normalize(input.Binormal), World);
	output.WorldToTangentSpace[2] = mul(normalize(input.Normal), World);
	
	output.View = normalize(float4(EyePosition,1.0) - worldPosition);

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(ColorMapSampler, input.TexCoord);
    
    float3 nmapcol = tex2D(NormalMapSampler, input.TexCoord);
    
    //nmapcol.x = -nmapcol.x;
    
	float3 normalMap = 2.0 *(nmapcol) - 1.0;
	normalMap = normalize(mul(normalMap, input.WorldToTangentSpace));
	float4 normal = float4(normalMap,1.0);

	float4 diffuse = saturate(dot(-LightDirection,normal));
	float4 reflect = normalize(2*diffuse*normal-float4(LightDirection,1.0));
	float4 specular = pow(saturate(dot(reflect,input.View)),32);

    return  color * AmbientColor * AmbientIntensity + 
			color * DiffuseIntensity * DiffuseColor * diffuse + 
			color * tex2D(SpecularMapSampler, input.TexCoord) * specular;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};

