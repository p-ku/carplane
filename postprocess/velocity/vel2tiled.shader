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

void fragment()
{
	vec3 max_tile = vec3(0.5, 0.5, 0.);

	// vec2 first_uv = floor(FRAGCOORD.xy) * tile_uv;
	//	vec2 first_uv = SCREEN_UV;

	for (float i = SCREEN_UV.x; i < SCREEN_UV.x + tile_uv.x; i += inv_reso.x)
	{
		for (float j = SCREEN_UV.y; j < SCREEN_UV.y + tile_uv.y; j += inv_reso.y)
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
}