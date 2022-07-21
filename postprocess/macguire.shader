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
const float big_n = 35., lil_r = 40., kk = 40., nn = 0.95, gamma = 1.5, phi = 27.;
const float big_e = 0.00833333333;
const float j_prime_term = nn * phi / big_n;

varying mat4 ipm;

// vec3 tap(vec3 pixel_vel)
// {
// 	//	vec2 uv2pix = (pixel_vel.xy - 0.5) * reso;
// 	//	float vel_mag = length(uv2pix);
// 	////	uv2pix = 0.5 + 0.5 * uv2pix * clamp(vel_mag * big_e, 0.5, lil_r) / (lil_r * (vel_mag + epsilon));
// 	float final_mag = length(uv2pix);
//
// 	return vec3(uv2pix, final_mag);
// }

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
float halton(vec2 frag) // https://www.gsn-lib.org/apps/raytracing/index.php?name=example_halton
{
	float short_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
	float time_adjust = mod(ceil(TIME * framerate_target), float(short_dim)); // float(long_dim));
	time_adjust = ceil(TIME * framerate_target);

	float short_frag_time = short_frag + time_adjust;
	short_frag_time = short_frag + time_adjust;

	float long_frag = ceil(dot(dim_check, frag));
	//	float long_frag_time = long_frag + ceil(TIME * 60.);

	float even_odd = mod(short_frag_time, 2) * 2. - 1.;

	//	bool checker = fract((short_frag - time_adjust) / float(halton_mod)) <= 0.5;
	bool checker = fract((short_frag - time_adjust) / float(33)) <= 0.5;

	// checker = fract(short_frag_time / 2.) >= 0.5;
	checker = true;
	vec2 check_vec = vec2(float(checker), float(!checker));

	bool time_check = fract(TIME * framerate_target * 0.5) >= 0.5;
	vec2 time_check_vec = vec2(float(time_check), float(!time_check));

	float pixel_row = float(long_dim) * short_frag;

	float up_down = pixel_row;

	float left_right = dot(vec2(long_frag, float(long_dim + 1) - long_frag), check_vec);

	//	int pixel_id = int(float(long_dim) * short_frag + dot(vec2(long_frag, float(long_dim + 1) - long_frag), check_vec));
	int pixel_id = int(up_down + left_right);

	// int n = halton_num + pixel_id % (long_dim - 11 - int(10. * cos(short_frag * pi / float(short_dim))) % 21); //+ int(long_frag); // + int(TIME);
	// n = halton_num + pixel_id % (long_dim - 21 - int(10. * cos(short_frag * pi / float(short_dim))));					 //+ int(long_frag); // + int(TIME);
	// n = halton_num + pixel_id % (long_dim - 21);																															 //+ int(long_frag); // + int(TIME);
	// n = 21 + pixel_id % (long_dim - halton_shift);
	int n = pixel_id % (long_dim - 5 + int(2. * (cos(short_frag_time * pi / 4.) - 1.))); // 6, 10,14,18

	// int halton_pixel = 21 + (pixel_id) % (long_dim - halton_shift);

	float r = 0.0;
	float f = 1.0;
	while (n > 0)
	{
		f = f / 2.;
		r = r + f * float(n % 2);
		n = int(floor(float(n) / 2.));
	}
	return r;
}

vec3 filter(vec2 uv, vec2 frag)
{
	vec3 vp = texture(velocity_buffer, uv).xyz;
	//	float vp_mag = length(vp.xy);
	vec3 color_p = texture(color_buffer, uv).xyz;

	float j = halton(frag);

	// vec2 tile_frag = floor(FRAGCOORD.xy / tile_size);
	vec2 j_tile_falloff = abs(0.5 - fract(frag.xy / tile_size));
	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;
	// if (j_tile_falloff.x == j_tile_falloff.y)

	vec2 j_frag = frag.xy + mix(vec2(-1.), vec2(1.), j * j_tile_falloff + 0.5);

	// vec3 v_max = tap(j_frag / reso);
	vec3 v_max = texture(neighbor_buffer, j_frag / reso).xyz;
	//	v_max.xy -= 0.5;
	// v_max.xy -= buffer_correction;
	//	v_max.xy *= reso;
	// v_max.z = length(v_max.xy);
	//	if (v_max.z < 0.0625)
	//		return color_p;

	vec2 wn = normalize(v_max.xy);
	//	vec2 vc = 0.5 + 0.5 * vp.xy * clamp(vp_mag * big_e, 0.5, lil_r) / (lil_r * (vp_mag + epsilon));
	vec3 vc = vp; //- 0.5;
	float vc_mag = length(vc);
	vec2 wp = vec2(-wn.y, wn.x);
	//	vec2 vc = mix(wp, normalize(vp.xy), (length(vp.xy) - 0.5) / gamma);
	// float vc_mag = length(vc);
	// vc = 0.5 + 0.5 * vc * clamp(vc_mag, 0.5, lil_r) / lil_r;

	if (dot(wp, vc.xy) < 0.)
		wp = -wp;
	vec2 wc = normalize(mix(wp, normalize(vc.xy), (vc_mag - 0.5) / gamma));

	float total_weight = big_n / (kk * vc_mag);

	vec3 result = color_p * total_weight;

	float j_prime = j * j_prime_term;

	for (float i = 0.; i < big_n; i++)
	{
		float tee = mix(-1., 1., (i + j_prime + 1.) / (big_n + 1.));
		vec2 d;
		if (int(i) % 2 == 0)
			d = vc.xy;
		else
			d = v_max.xy;

		float big_tee = abs(tee * v_max.z);

		vec2 big_s = floor(tee * d) + frag;

		vec3 vps = texture(velocity_buffer, big_s / reso).xyz;
		//	vps.xy -= 0.5;
		//	vps.xy *= reso;
		//		float vps_mag = length(vps.xy);
		//	vec2 vs = 0.5 + 0.5 * vps.xy * clamp(vps_mag * big_e, 0.5, lil_r) / (lil_r * (vps_mag + epsilon));
		vec3 vs = vps; // - 0.5;
		float vs_mag = length(vs);

		vec3 color_s = texture(color_buffer, big_s / reso).xyz;

		//	float vs_mag = length(vs.xy);
		vec2 z_cmp = vec2(float(vp.z > vps.z), float(vps.z > vp.z));

		float weight = 0.;
		float wA = dot(wc, d);
		float wB = dot(normalize(vs.xy), d);

		float cone_s = max(1. - big_tee / vs_mag, 0.);
		float cone_c = max(1. - big_tee / vc_mag, 0.);
		float v_min = min(vs_mag, vc_mag);
		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee);

		weight += z_cmp.x * cone_s * wB;
		weight += z_cmp.y * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;
		total_weight += weight;
		result += color_s * weight;
	}

	// return result / total_weight;
	return vec3(j);
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

	COLOR = vec4(texture(neighbor_buffer, SCREEN_UV).xy, 0., 1.);
	//	COLOR = vec4(vec3((output.x + 1.) * 0.5), 1.);
	COLOR = vec4(output, 1.);
	//	COLOR = vec4(fract(TIME));
	//	 COLOR = vec4(v_max, 1.);
}