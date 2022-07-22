// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D tiled_velocity; // Velocity and depth information
uniform vec2 dims;

void fragment()
{
	vec3 max_tile = vec3(0.5, 0.5, 0.);

	vec2 center_vmax = texture(tiled_velocity, SCREEN_UV).xy - 0.5;
	float variance = 0.;
	float sum = dot(center_vmax, center_vmax);
	for (float i = -1.; i < 1.5; i++)
	{
		for (float j = -1.; j < 1.5; j++)
		{
			vec2 rel_frag = vec2(i, j);
			vec2 tiled_frag = (rel_frag + FRAGCOORD.xy);
			if (rel_frag == vec2(0.))
				continue;
			if (tiled_frag.x > dims.x || tiled_frag.y > dims.y || tiled_frag.x < 0. || tiled_frag.y < 0.)
				continue;
			vec3 sample = texture(tiled_velocity, tiled_frag / dims).xyz;

			vec2 corrected_sample = sample.xy - 0.5;

			if (sample.z > max_tile.z)
			{
				// If diagonal.
				if (abs(i) == 1. && abs(j) == 1.)
					// Check if velocity is pointed at the center.
					if (dot(rel_frag, corrected_sample) >= 0.)
						continue;

				max_tile = sample;
			}
		}
	}
	COLOR = vec4(max_tile, 1.);
}