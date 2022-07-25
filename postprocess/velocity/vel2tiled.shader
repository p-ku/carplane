// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D velocity_buffer; // Velocity and depth information
uniform vec2 reso;
uniform vec2 inv_reso;
uniform vec2 tile_uv;
void vertex()
{
	UV = 1. - UV;
}

void fragment()
{
	vec3 max_tile = vec3(0.5, 0.5, 0.);

	// vec2 first_uv = floor(FRAGCOORD.xy) * tile_uv;
	//	vec2 first_uv = UV;

	for (float i = UV.x; i < UV.x + tile_uv.x; i += inv_reso.x)
	{
		for (float j = UV.y; j < UV.y + tile_uv.y; j += inv_reso.y)
		{
			vec2 sample = texture(velocity_buffer, vec2(i, j)).xy;

			float sample_length = length(sample.xy - 0.5);

			if (sample_length > max_tile.z)
			{
				max_tile.xy = sample;
				max_tile.z = sample_length;
			}
		}
	}
	COLOR = vec4(max_tile, 1.);
	//	COLOR = vec4(texture(velocity_buffer, UV).xyz, 1.);
}