// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D tiled_velocity; // Velocity and depth information
uniform vec2 dimensions;

const float shutter_angle = 0.5; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
// Shutter angles > 1 are unrealistic; blur length exceeds frame time, or max "shutter speed".
const float max_steps = 16.; // Number of blur samples.
const float threshold = 1.;	 // Minimum pixel movement required to blur. Using one pixel.
uniform vec2 fov;						 // Field of view, horizontal and vertical.
uniform float uv_depth;			 // Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 reso;

// const vec3 hdr_correct = vec3(0.5, 0.5, 0.);

const float neighbor_size = 3.;

void fragment()
{
	float count = 1.;
	//	vec3 max_tile = hdr_correct;
	vec3 max_tile = vec3(0.5, 0.5, 0.);
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
			//	float sample_length = length(sample.xy - 0.5);
			if (sample.z > max_tile.z)
				max_tile = sample;
			//	max_tile_length = sample_length;
		}
	}
	// Go ahead and account for shutter speed and projection distortion.
	// vec3 w_shutter = shutter_angle * max_tile;
	// vec2 cos_sq = cos((SCREEN_UV - 0.5) * fov - w_shutter * 0.5); // Last term puts blur "point" at mid-blur
	//// Final output in pixels.
	// vec2 pixel_dist = w_shutter * uv_depth / (cos_sq * cos_sq);
	// float real_dist = max_tile_length * 2. * 3.14159 * max_tile.z;
	// float pixel_depth = max_tile.z * length(pixel_dist) / real_dist;
	//	COLOR = vec4(pixel_dist + 5000., pixel_depth + 5000., 1.);

	COLOR = vec4(max_tile, 1.);
}