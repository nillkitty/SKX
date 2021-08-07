
sampler2D input : register(s0);
float time;
float ticks;

float4 main(float4 position : SV_Position,
	         float4 color : COLOR0, 
	         float2 uv : TEXCOORD0) : COLOR0
{

	float2 R = float2(256, 208);		// Target resolution
	float2 U = float2(position.x, 208 - position.y); // Reverse Y coordinate
	float4 X;							// X is the pixel output from the vaporwave bg
	float4 O = tex2D(input, uv.xy);		// O is the pixel the game is intending to draw (starry bg)

	float C = 1. - U.y / R.y;			// Find vertical center
	U = 5. * (U + U - R) / R.y;         // normalized coordinates
	U.y = 1 - U.y * 2.;                 // swap vertical
	U /= 1. + U.y / 10.;                // perspective
	U.y -= time;
	U = abs(frac(U) - .90);             // distance to axis
	U = .2 / sqrt(U);					// blur line
	X = (U.x * C + U.y * C * 2.) * float4(.3,.3,1.,0) * C 
		+ float4(.4,0.1,.2,0.) * C;		// combine H+V + color * inverted fade

	// Combine vaporwave (X) with level background with quadratic fade
	return X + (O * (0.1 + 0.6 * abs(sin(ticks / 30 * abs(position.y)))));
}

technique T0
{
	pass P0
	{
		PixelShader = compile ps_3_0 main();
	}
};