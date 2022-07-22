// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D neighbor_buffer; // Plane image of 3D world as viewed by player, used for color
uniform sampler2D color_buffer;		 // Plane image of 3D world as viewed by player, used for color
uniform sampler2D velocity_buffer; // Velocity and depth information

uniform float shutter_angle; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
//  Shutter angles > 1 are unrealistic; blur length exceeds frame time, or max "shutter speed".
const float steps = 16.;		// Number of samples. There actually [2*steps + 1] steps, both sides and center
const float threshold = 1.; // Minimum pixel movement required to blur. Using one pixel.
const float framerate_target = 60.;

uniform vec2 fov;				// Field of view, horizontal and vertical.
uniform vec2 half_fov;	// Field of view, horizontal and vertical.
uniform float uv_depth; // Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 uv_depth_vec;
uniform vec2 half_uv_depth_vec;
uniform vec2 reso;
uniform vec2 dim_check;
uniform int short_dim;
uniform int long_dim;
uniform float pixel_count;

uniform float buffer_correction;
const mat4 inv_mat0 = mat4(vec4(0.), vec4(0.), vec4(0.), vec4(0., 0., -1., 0.));
uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;
uniform int halton_num;
uniform int halton_shift;
uniform int halton_mod;
uniform float tile_size;
const float epsilon = 10e-5;
const float length_in_samples = steps * 2. - 1.;
const float pi = 3.14159265359;
const float two_pi = 2. * pi;
const float big_n = 35., kk = 40., nn = 0.95, gamma = 1.5, phi = 27.;
const float big_e = 0.00833333333;
const float j_prime_term = nn * phi / big_n;

varying mat4 ipm;

vec3 tap(vec3 pixel_vel)
{
	vec2 corrected_vel = (pixel_vel.xy - 0.5); // * reso;
	float vel_mag = length(corrected_vel);
	corrected_vel = 0.5 + 0.5 * corrected_vel * clamp(vel_mag * big_e, 0.5, tile_size) / (tile_size * (vel_mag + epsilon));
	//	float final_mag = length(uv2pix);
	corrected_vel *= reso;
	vel_mag = length(corrected_vel);
	return vec3(corrected_vel, vel_mag);
	//	return vec3(uv2pix, final_mag);
}
vec4 correct(vec3 pixel_vel)
{
	vec2 corrected = (pixel_vel.xy - 0.5) * 2. * tile_size;
	return vec4(corrected, pixel_vel.z, length(corrected));
}
// vec2 halton(inout float result, float n)
// {
//
// 	float p, u, v, ip, kk, k, p2 = 2.;
// 	int pos = 0, a;
// 	for (k = 0.; k < n; k++)
// 	{
// 		u = 0.;
// 		kk = k;
// 		for (p = 0.5; kk >= 1.; p *= 0.5)
// 			if (mod(kk, 2.) == 1.) // kk mod 2 == 1
// 				u += p;
// 		v = 0.;
// 		ip = 1.0 / p2; // inverse of p2
// 		p = ip;
// 		kk = k;
// 		p *= ip;
// 		kk /= p2;
// 		if ((a == int(kk) % int(p2)))
// 			v += float(a) * p;
// 		//	if (a == mod(kk, p2))
// 		//		v += float(a) * p;
// 		result[pos++] = u;
// 		result[pos++] = v;
// 		return vec2(0.);
// 	}
// }
// vec2 haltonint(inout float result, int n)
//{
//
//	int p, u, v, ip, kk, k, p2 = 3;
//	int pos = 0, a;
//	for (k = 0; k < n; k++)
//	{
//		u = 0;
//		kk = k;
//		for (p = 0.5; kk >= 1.; p *= 0.5)
//			if (mod(kk, 2) == 1) // kk mod 2 == 1
//				u += p;
//		v = 0.;
//		ip = 1.0 / p2; // inverse of p2
//		p = ip;
//		kk = k;
//		p *= ip;
//		kk /= p2;
//		if ((a == int(kk) % int(p2)))
//			v += float(a) * p;
//		//	if (a == mod(kk, p2))
//		//		v += float(a) * p;
//		result[pos++] = u;
//		result[pos++] = v;
//		return vec2(0.);
//	}
//}
// void haltonpy(float result, inout ivec4 vars) // b=2000
//{
//	//	int n = 0, d = 1, x, y, base = 2;
//	// vars is in the order x, y, n, d.
//	x = d - n; // x=1
//	if (x == 1)
//	{
//		n = 1;
//		d *= base; // d=2000
//	}
//	else
//	{
//		y = d / base;
//		while (x <= y)
//			y = y / base;
//		n = (base + 1) * y - x;
//	}
//	result = float(n) / float(d);
//}
float halton3(vec2 frag) // https://www.gsn-lib.org/apps/raytracing/index.php?name=example_halton
{
	float short_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
	float long_frag = ceil(dot(dim_check, frag));

	int n = int(long_frag + float(long_dim) * short_frag);
	float base = 3.;
	float r = 0.0;
	float f = 1.0;
	while (n > 0)
	{
		f = f / base;
		r = r + f * float(n % int(base));
		n = int(floor(float(n) / base));
	}
	return r;
}

vec3 filter(vec2 uv, vec2 frag)
{
	//	vec3 vp = texture(velocity_buffer, uv).xyz;
	//	vp.xy -= 0.5;
	//	float vp_mag = length(vp.xy);
	//
	vec3 color_p = texture(color_buffer, uv).xyz;

	float j = halton3(frag);

	vec2 j_tile_falloff = abs(0.5 - fract(frag.xy / tile_size));
	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;
	// if (j_tile_falloff.x == j_tile_falloff.y)

	//	vec2 j_frag = frag.xy + mix(vec2(-1.), vec2(1.), j * j_tile_falloff + 0.5);
	vec2 j_frag = frag + mix(vec2(-1.), vec2(1.), j * j_tile_falloff);

	//	vec2 j_frag = frag.xy + mix(vec2(-1.), vec2(1.), dot(vec2(j), j_tile_falloff) + 0.5);

	// vec3 v_max = tap(j_frag / reso);
	vec4 v_max = correct(texture(neighbor_buffer, j_frag / reso).xyz);

	//	v_max.xy = (v_max.xy - 0.5) * 2. * tile_size;
	// v_max.xy -= buffer_correction;
	//	v_max.xy *= reso;
	//	v_max.z = length(v_max.xy);
	//	if (v_max.z < 0.5)
	//		return color_p;

	vec2 wn = normalize(v_max.xy);
	//	vec2 vc = 0.5 + 0.5 * vp.xy * clamp(vp_mag * big_e, 0.5, tile_size) / (tile_size * (vp_mag + epsilon));

	// float vc_norm = normalize(vc.xy);
	vec4 vc = correct(texture(velocity_buffer, uv).xyz);
	//	vc.xy -= 0.5;
	//	vc.xy = (vc.xy - 0.5) * 2. * tile_size;
	// float vc.w = vc.w;

	vec2 wp = vec2(-wn.y, wn.x);
	if (dot(wp, vc.xy) < 0.)
		wp = -wp;

	//	vec2 vc = mix(wp, normalize(vp.xy), (vp_mag * sqrt(2.) - 0.0)); // / gamma);
	// vec2 vc = mix(wp, normalize(vp.xy), (vp_mag * sqrt(2.)));

	//	vc.xy -= 0.5;
	// float vc.w = length(vc.xy);
	//  vec2 vc = mix(wp, normalize(vp.xy), (vp_mag - 0.5) / gamma);
	//  float vc.w = length(vc);
	//	vec2 vc = mix(wp, normalize(vp.xy), (length(vp.xy) - 0.5) / gamma);
	//   float vc.w = length(vc);
	//   vc = 0.5 + 0.5 * vc * clamp(vc.w, 0.5, tile_size) / tile_size;

	vec2 wc = normalize(mix(wp, normalize(vc.xy), (vc.w - 0.5) / gamma));
	//	vec2 wc = normalize(vc);

	float total_weight = big_n / (kk * vc.w);
	total_weight = 0.7;

	vec3 result = color_p * total_weight;

	float j_prime = j * j_prime_term;
	for (float i = 0.; i < big_n; i++)
	{
		// float i = big_n - 1.;
		float tee = mix(-1., 1., (i + j_prime + 1.) / (big_n + 1.));
		tee = mix(-1., 1., (i + 1.) / (big_n + 1.));
		vec2 d;
		if (int(i) % 2 == 0)
			d = vc.xy;
		else
			d = v_max.xy;

		d = v_max.xy;

		float big_tee = abs(tee * v_max.w);

		vec2 big_s = floor(tee * d) + frag;

		//	vec3 vps = texture(velocity_buffer, big_s / reso).xyz;
		//	vps.xy -= 0.5;
		//	vps.xy *= reso;
		//		float vps_mag = length(vps.xy);
		//	vec2 vs = 0.5 + 0.5 * vps.xy * clamp(vps_mag * big_e, 0.5, tile_size) / (tile_size * (vps_mag + epsilon));
		vec4 vs = correct(texture(velocity_buffer, big_s / reso).xyz);
		//	vs.xy -= 0.5;
		//	vs.xy = (vs.xy - 0.5) * 2. * tile_size;

		//	float vs.w = length(vs.xy);
		// float vs.w = vs.w;
		vec3 color_s = texture(color_buffer, big_s / reso).xyz;

		vec2 z_cmp = vec2(float(vc.z > vs.z), float(vs.z > vc.z));
		// vec2 z_cmp;
		// z_cmp.x = clamp(1. - (vc.z - vs.z) / min(vc.z, vs.z), 0., 1.);
		// z_cmp.y = clamp(1. - (vs.z - vc.z) / min(vs.z, vc.z), 0., 1.);

		float weight = 0.;
		float wA = dot(wc, d);
		// float wA = dot(wc, normalize(v_max.xy));

		float wB = dot(normalize(vs.xy), d);

		float cone_s = max(1. - big_tee / vs.w, 0.);
		float cone_c = max(1. - big_tee / vc.w, 0.);
		float v_min = min(vs.w, vc.w);
		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee);
		// float cylinder = 1.;
		// if (big_tee > v_min * 1.05)
		//	cylinder = 0.;
		// else if (big_tee > v_min * 0.95)
		//{
		//	float ex = (big_tee - v_min * 0.95) / (v_min * 0.1);
		//	float ex_sq = ex * ex;
		//	cylinder = 1. - (3. * ex_sq - 2.0 * ex_sq * ex);
		//}
		weight += z_cmp.x * cone_s * wB;
		weight += z_cmp.y * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;

		total_weight += weight;
		result += color_s * weight;
	}
	//	return vec3(j_prime);
	// return vec3(uv, 0.);
	// return vec3(vc.w);
	return result / total_weight;
	// return vec3(vc.xy / reso, 0.);
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
	vec3 output = filter(SCREEN_UV, FRAGCOORD.xy);

	//	COLOR = vec4(sum.rgb + (1. - sum.w) * pixel_color, 1.);

	//	COLOR = vec4(texture(neighbor_buffer, SCREEN_UV).xy, 0., 1.);
	//	COLOR = vec4(vec3((output.x + 1.) * 0.5), 1.);
	COLOR = vec4(output, 1.);
	//	COLOR = vec4(fract(TIME));
	//	 COLOR = vec4(v_max, 1.);
}