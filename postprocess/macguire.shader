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
uniform vec2 fov;						// Field of view, horizontal and vertical.
uniform vec2 half_fov;			// Field of view, horizontal and vertical.
uniform float uv_depth;			// Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 uv_depth_vec;
uniform vec2 half_uv_depth_vec;
uniform vec2 reso;
uniform vec2 dim_check;
uniform int short_dim;
uniform int long_dim;
uniform float buffer_correction;
const mat4 inv_mat0 = mat4(vec4(0.), vec4(0.), vec4(0.), vec4(0., 0., -1., 0.));
uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;
uniform int halton_num;
uniform float tile_size;
const float epsilon = 10e-5;
const float length_in_samples = steps * 2. - 1.;
const float two_pi = 2. * 3.14159;
const float big_n = 35., lil_r = 40., kk = 40., nn = 0.95, gamma = 1.5, phi = 27.;

varying mat4 ipm;

vec3 tap(vec2 uv)
{
	//	vec2 uv = frag_coord;
	vec3 buffer_tap = texture(neighbor_buffer, uv).xyz;
	buffer_tap.xy -= buffer_correction;

	//	vec3 ndc = vec3(uv, buffer_tap.w) * 2.0 - 1.0;
	//	vec4 view = ipm * vec4(ndc, 1.0);
	//	view.xyz /= view.w;
	// buffer_tap.w = length(view.xyz);

	// Spread in pixels.
	buffer_tap.xy *= reso;
	// buffer_tap.xy = buffer_tap.xy * clamp(buffer_tap.z,0.5,) / (buffer_tap.z + epsilon);
	//	buffer_tap.w = abs(buffer_tap.x) * sqrt(half_uv_depth_vec.x + 1.) / half_uv_depth_vec.x;

	return buffer_tap;
}

float calc_weight(vec4 center, vec4 sample, vec3 direction, float pixel_sample_convert, float sample_unit_offset)
{
	vec2 spread_cmp = pixel_sample_convert * vec2(sample.z, center.z) - max(sample_unit_offset - 1., 0.);

	float depth_delta = (sample.w - center.w);
	// float depth_factor = 2. * depth_delta;
	float depth_adjust = pixel_sample_convert * depth_delta;

	//	vec2 depth_cmp = 0.5 + vec2(depth_adjust, -depth_adjust);
	//
	//	vec2 z_cmp_fore = clamp(center.w - sample.w / min(center.w, sample.w), 0., 1.);
	//	vec2 z_cmp_back = clamp(sample.w - center.w / min(sample.w, center.w), 0., 1.);
	vec2 depth_check = vec2(float(center.w > sample.w), float(sample.w > center.w));
	//	float depth_check_back = float(sample.w > center.w);

	return dot(depth_check, clamp(spread_cmp, 0., 1.));
	// return dot(depth_cmp, spread_cmp);
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
float halton(int n) // https://www.gsn-lib.org/apps/raytracing/index.php?name=example_halton
{
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
	vec3 color_p = texture(color_buffer, uv).xyz;
	//	int halton_pixel = halton_num + int(reso.x * floor(frag.y) + ceil(frag.x));
	// int halton_pixel = halton_num + int(floor(frag.y) * 4. + floor(frag.x) * 5. + uv.x);
	//	int halton_pixel = 1 + (halton_num + int(reso.x * floor(frag.y) + ceil(frag.x))) % 409;
	float short_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
	int halton_mod = int(float(long_dim) * short_frag + ceil(dot(dim_check, frag)));
	int halton_pixel = 21 + (halton_num + halton_mod) % (long_dim - 11);

	float j = halton(halton_pixel);

	// vec2 tile_frag = floor(FRAGCOORD.xy / tile_size);
	vec2 j_tile_falloff = abs(0.5 - fract(frag.xy / tile_size));
	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;
	// if (j_tile_falloff.x == j_tile_falloff.y)

	vec2 j_frag = frag.xy + mix(vec2(-1.), vec2(1.), j * j_tile_falloff + 0.5);

	vec3 v_max = tap(j_frag / reso);
	// if (v_max.z < 0.5)
	//	return color_p;

	vec2 wn = normalize(v_max.xy);
	vec2 wp = vec2(-wn.y, wn.x);
	vec2 vc = mix(wp, normalize(vp.xy), (length(vp.xy) - 0.0)); // / gamma);
	float vc_mag = length(vc);
	if (dot(wp, vp.xy) < 0.)
		wp = -wp;
	vec2 wc = normalize(vc);

	float total_weight = big_n / (kk * vc_mag);

	vec3 result = color_p * total_weight;

	float j_prime = j * nn * phi / big_n;

	for (float i = 0.; i < big_n; i++)
	{
		float tee = mix(-1., 1., (i + j_prime + 1.) / (big_n + 1.));
		vec2 d;
		if (int(i) % 2 == 0)
			d = vc;
		else
			d = v_max.xy;

		float big_tee = tee * v_max.z;
		float big_tee_mag = abs(big_tee);

		vec2 big_s = floor(tee * d) + frag;

		vec3 vs = texture(velocity_buffer, uv).xyz;
		vec3 color_s = texture(color_buffer, big_s / reso).xyz;

		float vs_mag = length(vs.xy);
		vec2 z_cmp = vec2(float(vp.z > vs.z), float(vs.z > vp.z));

		float weight = 0.;
		float wA = dot(wc, d);
		float wB = dot(normalize(vs.xy), d);
		float cone_c = max(1. - big_tee_mag / vc_mag, 0.);
		float cone_s = max(1. - big_tee_mag / vs_mag, 0.);
		float v_min = min(vs_mag, vc_mag);
		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee_mag);
		weight += z_cmp.x * cone_s * wB;
		weight += z_cmp.y * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;
		total_weight += weight;
		result += color_s * weight;
	}

	return result / total_weight;
	return vec3(j, 0., 0.);
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
	COLOR = vec4(vec3((output.x + 1.) * 0.5), 1.);
	COLOR = vec4(output, 1.);

	//	 COLOR = vec4(v_max, 1.);
}