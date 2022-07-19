// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D velocity_buffer; // Velocity and depth information
uniform sampler2D tiled_velocity;	 // Velocity and depth information
uniform vec2 dims;

uniform float shutter_angle; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
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
	vec3 max_tile = vec3(0.5, 0.5, 0.);

	vec2 center_vmax = texture(tiled_velocity, SCREEN_UV).xy - 0.5;
	float variance = 0.;
	float sum = dot(center_vmax, center_vmax);
	float denom = sum;
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
			denom += dot(corrected_sample, corrected_sample);
			sum += abs(dot(center_vmax, corrected_sample));

			if (sample.z > max_tile.z)
			{
				// If diagonal.
				if (abs(i) == 1. && abs(j) == 1.)
					// Check if velocity is pointed at the center.
					if (dot(rel_frag, corrected_sample) >= 0.)
						// if (abs(atan(sample.y / sample.x) - atan(sample.y / sample.x)) < 3.14159 / 2.)
						continue;

				max_tile = sample;
			}
		}
	}
	variance = 1. - variance / denom;

	// 	for (float i = -1.; i < 1.5; i++)
	// 	{
	// 		for (float j = -1.; j < 1.5; j++)
	// 		{
	// 			//	vec3 sample = texture(tiled_velocity, (vec2((i - 1.), (j - 1.)) + FRAGCOORD.xy) / dims).xyz;
	// 			//	if (sample.z > max_tile.z)
	// 			//		max_tile = sample;
	// 			vec2 rel_frag = vec2(i, j);
	// 			vec2 tiled_frag = (rel_frag + FRAGCOORD.xy);
	// 			if (rel_frag == vec2(0.))
	// 				continue;
	// 			if (tiled_frag.x > dims.x || tiled_frag.y > dims.y || tiled_frag.x < 0. || tiled_frag.y < 0.)
	// 				continue;
	// 			vec3 sample = texture(tiled_velocity, tiled_frag / dims).xyz;
	// 			//	float sample_length = length(sample.xy - 0.5);
	// 			if (sample.z > max_tile.z)
	// 			{
	// 				// If diagonal.
	// 				if (abs(i) == 1. && abs(j) == 1.)
	// 					// Check if velocity is pointed at the center.
	// 					if (dot(rel_frag, sample.xy - 0.5) >= 0.)
	// 						// if (abs(atan(sample.y / sample.x) - atan(sample.y / sample.x)) < 3.14159 / 2.)
	// 						continue;
	//
	// 				max_tile = sample;
	// 			}
	// 			//	max_tile_length = sample_length;
	// 		}
	// 	}
	// Go ahead and account for shutter speed and projection distortion.
	// vec3 w_shutter = shutter_angle * max_tile;
	// vec2 cos_sq = cos((SCREEN_UV - 0.5) * fov - w_shutter * 0.5); // Last term puts blur "point" at mid-blur
	//// Final output in pixels.
	// vec2 pixel_dist = w_shutter * uv_depth / (cos_sq * cos_sq);
	// float real_dist = max_tile_length * 2. * 3.14159 * max_tile.z;
	// float pixel_depth = max_tile.z * length(pixel_dist) / real_dist;
	//	COLOR = vec4(pixel_dist + 5000., pixel_depth + 5000., 1.);
	COLOR = vec4(max_tile.xy * shutter_angle, variance, 1.);
	// COLOR = vec4(max_tile, 1.);
}