// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D tiled_velocity; // Velocity and depth information
uniform vec2 dimensions;
// const vec3 hdr_correct = vec3(0.5, 0.5, 0.);

const float neighbor_size = 3.;

void fragment()
{
	float count = 1.;
	//	vec3 max_tile = hdr_correct;
	vec3 max_tile = vec3(0.);
	float max_tile_length = 0.;

	vec2 center_tile = floor(SCREEN_UV * dimensions);

	for (float i = 0.5; i < neighbor_size; i++)
	{
		for (float j = 0.5; j < neighbor_size; j++)
		{
			//	vec3 sample = texture(tiled_velocity, (vec2((i - 1.), (j - 1.)) + FRAGCOORD.xy) / dimensions).xyz;
			//	if (sample.z > max_tile.z)
			//		max_tile = sample;
			vec3 sample = texture(tiled_velocity, (vec2((i - 1.), (j - 1.)) + FRAGCOORD.xy) / dimensions).xyz;
			float sample_length = length(sample.xy);
			if (sample_length > max_tile_length)
			{
				max_tile = sample;
				max_tile_length = sample_length;
			}
		}
	}

	COLOR = vec4(max_tile, 1.);
}