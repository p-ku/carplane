// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D tiled_velocity; // Velocity and depth information
uniform vec2 tile_uv_size;

void vertex()
{
	//	UV.y = 1. - UV.y;
}
void fragment()
{
	vec3 max_tile = vec3(0.5, 0.5, 0.);
	for (float i = -1.; i < 1.5; i++)
	{
		for (float j = -1.; j < 1.5; j++)
		{
			vec2 rel_tile = vec2(i, j);
			vec2 tiled_uv = rel_tile * tile_uv_size + UV;
			if (rel_tile == vec2(0.))
				continue;
			if (tiled_uv.x > 1. || tiled_uv.y > 1. || tiled_uv.x < 0. || tiled_uv.y < 0.)
				continue;

			vec3 sample = texture(tiled_velocity, tiled_uv).xyz;
			vec2 corrected_sample = sample.xy - 0.5;

			if (sample.z > max_tile.z)
			{
				// If diagonal.
				if (abs(i) == 1. && abs(j) == 1.)
					// Check if velocity is pointed at the center.
					if (dot(rel_tile, corrected_sample) >= 0.)
						continue;

				max_tile = sample;
			}
		}
	}
	COLOR = vec4(max_tile, 1.);
}