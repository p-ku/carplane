// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D velocity_buffer; // Velocity and depth information
uniform vec2 inv_reso;
uniform vec2 tile_uv_size;
void vertex()
{
	//	UV.y = 1. - UV.y;
}

void fragment()
{
	vec3 max_tile = vec3(0.5, 0.5, 0.);
	vec2 first_pixel = floor(UV / tile_uv_size) * tile_uv_size + 0.5 * inv_reso;
	for (float i = first_pixel.x; i < first_pixel.x + tile_uv_size.x; i += inv_reso.x)
	{
		for (float j = first_pixel.y; j < first_pixel.y + tile_uv_size.y; j += inv_reso.y)
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