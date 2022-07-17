// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D neighbor_buffer; // Plane image of 3D world as viewed by player, used for color
uniform sampler2D color_buffer;		 // Plane image of 3D world as viewed by player, used for color
uniform sampler2D velocity_buffer; // Velocity and depth information
uniform sampler2D tiled_buffer;		 // Velocity and depth information

const float shutter_angle = 5.5; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
// Shutter angles > 1 are unrealistic; blur length exceeds frame time, or max "shutter speed".
const float steps = 7.;			// Number of blur samples.
const float threshold = 1.; // Minimum pixel movement required to blur. Using one pixel.
uniform vec2 fov;						// Field of view, horizontal and vertical.
uniform float uv_depth;			// Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 reso;

float calc_weight(vec2 current, vec2 sample_uv, float offset_length, float offset_step_length)
{
	float sample_spread = texture(neighbor_buffer, sample_uv).z;
	float sample_depth = texture(velocity_buffer, sample_uv).z;
	vec2 spread_cmp = vec2(current.x, sample_spread) - offset_length + offset_step_length;
	float depth_delta = sample_depth - current.y;
	float depth_factor = 2. * depth_delta;
	vec2 depth_cmp = 0.5 + vec2(depth_factor, -depth_factor) * depth_delta;
	return dot(depth_cmp, spread_cmp);
}

void fragment()
{
	vec3 color = texture(color_buffer, SCREEN_UV).xyz;

	vec4 current = vec4(texture(neighbor_buffer, SCREEN_UV).xyz, texture(velocity_buffer, SCREEN_UV).z);
	current.xy -= 0.5;

	//  Counter for how many blur layers are applied.
	vec3 offset_step = current.xyz / steps;
	//	float offset_step_length = current.z / steps;
	vec3 center_offset = 0.5 * steps * offset_step;
	vec2 center_frag = FRAGCOORD.xy - center_offset.xy;
	vec2 center_uv = center_frag / reso;
	//	vec2 center_uv = calc_uv(FRAGCOORD.xy, center_offset.xy);
	// vec4 center_sample = vec4(texture(neighbor_buffer, center_uv).xyz, texture(velocity_buffer, center_uv).z);
	float center_sample_weight = calc_weight(current.zw, center_uv, center_offset.z, offset_step.z);
	vec3 center_color = texture(color_buffer, center_uv).xyz;

	//	vec4 sum = vec4(center_sample_weight * center_color, center_sample_weight);
	vec4 sum = vec4(center_color, center_sample_weight);

	float count = 1.;

	for (float i = 1.; i < steps + 1.; i++)
	{

		// Apply offset multiplied by number of steps.
		//	vec2 offset = ((i + 1.) / steps) * current.xy;

		vec3 offset = i * offset_step;
		//	float offset_length = i * offset_step_length;

		vec2 new_frag = FRAGCOORD.xy - offset.xy;

		// If blur is occurring offscreen, no need to continue looping.
		//	if (newUV.x < 0. || newUV.y < 0. || newUV.x > 1. || newUV.y > 1.)
		//		break;
		//	if (new_frag.x < 0. || new_frag.y < 0. || new_frag.x > reso.x || new_frag.y > reso.y)
		//		break;

		vec2 posMod = fract(new_frag / 2.);
		//	float truth_float = 0.25 * float(posMod.x < 0.5 && posMod.y < 0.5 || posMod.x > 0.5 && posMod.y > 0.5);
		if (posMod.x > 0.5 && posMod.y > 0.5 || posMod.x < 0.5 && posMod.y < 0.5)
			continue;

		//	vec2 sample_uv = (floor(new_frag) + 0.5) / reso;
		vec2 sample_uv = new_frag / reso;

		//	vec4 sample = vec4(texture(neighbor_buffer, sample_uv).xyz, texture(velocity_buffer, sample_uv).z);
		//	sample.xy -= 0.5;

		float sample_weight = calc_weight(current.zw, sample_uv, offset.z, offset_step.z);
		//	col += texture(color_buffer, sample_uv).rgb;
		sum.rgb += sample_weight * texture(color_buffer, sample_uv).xyz;
		sum.rgb += texture(color_buffer, sample_uv).xyz;

		//	tester = texture(color_buffer, sample_uv).xyz;
		sum.w += sample_weight;
		count++;
	}
	// Average the blur layers into final result.
	sum /= count;
	//}
	//	if (count == 0.)
	//		COLOR = vec4(1.);

	COLOR = vec4(sum.rgb + (1. - sum.w) * center_color, 1.);
	//   COLOR = vec4(vec3(sum.w), 1.);
	COLOR = vec4(sum.rgb, 1.);
	// COLOR = vec4(current.xy, 1., 1.);

	//	COLOR = vec4(current.xy + 0.5, 0., 1.);

	// vec2 posMod = fract(FRAGCOORD.xy / 200.);

	// else
	//	COLOR = vec4(0., 0., 1., 1.);

	// float Vee = float(floor(FRAGCOORD.x / 20.) == floor(FRAGCOORD.y / 20.));

	//	float posMod = float((-dither_scale + 2. * dither_scale * FRAGCOORD.x) * (-1. + 2. * FRAGCOORD.y));

	// vec2 Vee = mod(FRAGCOORD.xy, 20.);
	// vec4 O = vec4(float(Vee.x == .5 || Vee.y == .5));
	// if (length(Vee - 10.) < 5.)
	// 	O.x++;
	// COLOR = vec4(O.xyz, 1.);
	//	COLOR = vec4(vec3(truth_float), 1.);
}