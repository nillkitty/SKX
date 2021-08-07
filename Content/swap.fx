
// 1-to-1 color swap shader

sampler2D input : register(s0);
float4 FromColors[16];
float4 ToColors[16];
int NumColors;

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 Color;
    Color = tex2D(input , uv.xy);

    for (int i = 0; i < NumColors; i++) {
        if (Color.r == FromColors[i].r && Color.g == FromColors[i].g && Color.b == FromColors[i].b)
            return ToColors[i];
    }
    
    return Color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 main();
       
    }
}
