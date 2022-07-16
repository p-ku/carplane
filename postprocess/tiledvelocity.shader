// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D velocity_buffer; // Velocity and depth information
uniform vec2 reso;
uniform float tile_size = 20.;
uniform float tile_inv = 0.05;
const float tile_inv_2 = 0.025;

uniform vec2 dimensions;

void fragment()
{
	//	vec2 Vee = mod(FRAGCOORD.xy, 20.);
	//	vec4 O = vec4(float(Vee.x == .5 || Vee.y == .5));
	//	if (length(Vee - 10.) < 5.)
	//		O.x++;

	float Vee = float(floor(FRAGCOORD.xy / 20.) == floor(FRAGCOORD.xy));
	// vec4 O = vec4(float(Vee.x == .5 || Vee.y == .5));
	// if (length(Vee - 10.) < 5.)
	// 	O.x++;

	float count = 1.;
	vec3 max_tile = vec3(0.);
	float max_tile_length = 0.;

	vec2 center_tile = floor(FRAGCOORD.xy) * tile_size;
	vec2 first_pixel = floor(FRAGCOORD.xy / tile_size) * tile_size;

	for (float i = center_tile.x + 0.5; i < center_tile.x + tile_size; i++)
	{
		for (float j = center_tile.y + 0.5; j < center_tile.y + tile_size; j++)
		{
			vec3 sample = texture(velocity_buffer, vec2(i, j) / reso).xyz; // - 0.5;
			float sample_length = length(sample.xy);
			if (sample_length > max_tile_length)
			{
				max_tile = sample;
				max_tile_length = sample_length;
			}
		}
	}
	// COLOR = vec4(max_tile + 0.5, max_tile_length, 1.);

	COLOR = vec4(max_tile, 1.);
}