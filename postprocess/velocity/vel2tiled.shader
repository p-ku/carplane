// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D velocity_buffer; // Velocity and depth information
uniform vec2 reso;
uniform float tile_size;
uniform float tile_inv = 0.05;
const float tile_inv_2 = 0.025;
// uniform vec2 dimensions;

void fragment()
{
	//	float count = 1.;
	vec3 max_tile = vec3(0.5, 0.5, 0.);
	// vec3 max_tile = vec3(0.);

	//	float max_tile_length = 0.;

	vec2 first_frag = floor(FRAGCOORD.xy) * tile_size;
	//	vec2 first_frag = 0.5 + floor(FRAGCOORD.xy / tile_size) * tile_size;
	//	vec2 first_frag = 0.5 + floor(SCREEN_UV * reso) * tile_size;

	for (float i = first_frag.x; i < first_frag.x + tile_size; i++)
	{
		for (float j = first_frag.y; j < first_frag.y + tile_size; j++)
		{
			vec2 sample = texture(velocity_buffer, vec2(i, j) / reso).xy;
			float sample_length = length(sample.xy - 0.5);

			if (sample_length > max_tile.z)
			{
				max_tile.xy = sample;
				max_tile.z = sample_length;
			}
		}
	}
	// COLOR = vec4(max_tile + 0.5, max_tile_length, 1.);

	COLOR = vec4(max_tile, 1.);
}