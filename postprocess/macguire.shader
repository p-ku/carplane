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
uniform sampler2D vari_buffer;		 // Velocity and depth information

uniform float shutter_angle; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
//  Shutter angles > 1 are unrealistic; blur length exceeds frame time, or max "shutter speed".
const float steps = 16.;		// Number of samples. There actually [2*steps + 1] steps, both sides and center
const float threshold = 1.; // Minimum pixel movement required to blur. Using one pixel.
uniform vec2 fov;						// Field of view, horizontal and vertical.
uniform vec2 half_fov;			// Field of view, horizontal and vertical.
uniform float uv_depth;			// Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 uv_depth_vec;
uniform vec2 half_uv_depth_vec;
uniform vec2 reso;
uniform float buffer_correction;
const mat4 inv_mat0 = mat4(vec4(0.), vec4(0.), vec4(0.), vec4(0., 0., -1., 0.));
uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;
const float length_in_samples = steps * 2. - 1.;
const float two_pi = 2. * 3.14159;
const float gamma = 0.1;

varying mat4 ipm;

vec4 tap(vec2 uv)
{
	vec4 buffer_tap = vec4(texture(neighbor_buffer, uv).xyz, texture(velocity_buffer, uv).z);
	buffer_tap.xy -= buffer_correction;

	vec3 ndc = vec3(uv, buffer_tap.w) * 2.0 - 1.0;
	vec4 view = ipm * vec4(ndc, 1.0);
	view.xyz /= view.w;
	buffer_tap.w = length(view.xyz);

	// Spread in pixels.
	buffer_tap.xy *= reso;

	buffer_tap.z = length(buffer_tap.xy);
	//	float real_spread = (buffer_tap.w * two_pi) * buffer_tap.z;
	// Depth in pixels
	//	buffer_tap.w *= buffer_tap.z / real_spread;
	buffer_tap.w = abs(buffer_tap.x) * sqrt(half_uv_depth_vec.x + 1.) / half_uv_depth_vec.x;
	// Convert to UV.
	//	float real_spread = (buffer_tap.w * 2. * 3.14159) * buffer_tap.z;
	//	buffer_tap.xy /= fov;
	//	buffer_tap.z = length(buffer_tap.xy);
	//	buffer_tap.w *= buffer_tap.z / real_spread;
	return buffer_tap;
}

float calc_weight(vec4 center, vec4 sample, float pixel_sample_convert, float sample_unit_offset)
{
	vec2 spread_cmp = pixel_sample_convert * vec2(sample.z, center.z) - max(sample_unit_offset - 1., 0.);

	float depth_delta = (sample.w - center.w);
	// float depth_factor = 2. * depth_delta;
	float depth_adjust = pixel_sample_convert * depth_delta;
	vec2 depth_cmp = 0.5 + vec2(depth_adjust, -depth_adjust);

	return dot(clamp(depth_cmp, 0., 1.), clamp(spread_cmp, 0., 1.));
	// return dot(depth_cmp, spread_cmp);
}

void vertex()
{
	ipm = inv_mat0;
	ipm[0][0] = f1;
	ipm[1][1] = f2;
	ipm[2][3] = f3;
	ipm[3][3] = f4;
}

void fragment()
{
	vec2 vp = texture(velocity_buffer, SCREEN_UV).xy;
	//	float variance = texture(neighbor_buffer, SCREEN_UV).z;

	vec2 vperp = vec2(-vp.y, vp.x);
	if (dot(vperp, vp) <= 0.)
		vperp = vec2(vp.y, -vp.x);

	vec2 vcp = mix(vp.xy, vperp, (length(vp.xy) - 0.5) / gamma);

	float vmax_num = round(steps * texture(neighbor_buffer, SCREEN_UV).z);
	float vcp_num = steps - vmax_num;

	vec2 uv_sym = SCREEN_UV - 0.5;
	//	vec2 proj_warp = uv_depth_vec / (uv_depth_vec * uv_depth_vec * uv_sym * uv_sym + 1.);

	vec4 center = tap(SCREEN_UV);
	vec3 center_color = texture(color_buffer, SCREEN_UV).xyz;
	float pix2samp = length_in_samples / center.z;

	float center_weight = calc_weight(center, center, pix2samp, 0.);

	vec4 vel_test = vec4(texture(vari_buffer, SCREEN_UV).xy, 0., 1.);
	// vec4 d_test = vec4(texture(velocity_buffer, SCREEN_UV), 1.);

	//  Counter for how many blur layers are applied.
	vec3 offset_step = center.xyz / steps;
	float count = 1.;
	vec4 sum = vec4(0.);
	//	sum = vec4(center_color, center_weight);
	for (float i = -steps + 0.5; i < steps; i++)
	{
		vec3 offset = i * offset_step;

		vec2 new_frag = FRAGCOORD.xy - offset.xy;
		// vec2 sample_uv = SCREEN_UV - offset.xy;

		// If blur is occurring offscreen, no need to continue looping.
		//	if (newUV.x < 0. || newUV.y < 0. || newUV.x > 1. || newUV.y > 1.)
		//		break;
		//	if (new_frag.x < 0. || new_frag.y < 0. || new_frag.x > reso.x || new_frag.y > reso.y)
		//		break;

		vec2 posMod = fract(new_frag / 2.);

		if (posMod.x > 0.5 && posMod.y > 0.5 || posMod.x < 0.5 && posMod.y < 0.5)
		{
			//	sum.rgb += center_color;
			//	sum.w += 1.;
			//		alternate = !alternate;
			continue;
		}

		//	float truth_float = 0.25 * float(posMod.x < 0.5 && posMod.y < 0.5 || posMod.x > 0.5 && posMod.y > 0.5);

		vec2 sample_uv = new_frag / reso;

		float sample_weight = calc_weight(center, tap(sample_uv), pix2samp, abs(i));
		//	col += texture(color_buffer, sample_uv).rgb;
		sum.rgb += sample_weight * texture(color_buffer, sample_uv).xyz;
		//	sum.rgb += texture(color_buffer, sample_uv).xyz;

		sum.w += sample_weight;
		count++;
	}
	// Average the blur layers into final result.
	// sum /= (steps + 1.);
	sum /= (steps + 1.);
	// sum /= count;

	//}
	//	if (count == 0.)
	//		COLOR = vec4(1.);

	COLOR = vec4(sum.rgb + (1. - sum.w) * center_color, 1.);

	//	COLOR = vec4(sum.rgb, 1.);
	//	COLOR = vec4(center.xy + 0.5, 0., 1.);
	COLOR = vel_test;
	//	COLOR = vec4(center_color, 1.);
	// COLOR = vel_test;
	// sum.w = clamp(sum.w, 0.001, 1.);
	//   COLOR = vec4(vec3(sum.w), 1.);
}