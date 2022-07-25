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

uniform vec2 reso;
uniform vec2 inv_reso;
uniform vec2 dim_check;
uniform int short_dim;
uniform int long_dim;
uniform float tile_size;
uniform vec2 tile_uv;

uniform float steps;
uniform float j_prime_term;
uniform float gamma = 1.5;
uniform float kk;
const float eps = 0.00001;

uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;

vec4 correct(vec3 pixel_vel)
{

	vec2 corrected = (pixel_vel.xy - 0.5) * tile_size;
	// corrected = (pixel_vel.xy - 0.5) ;
	return vec4(corrected, pixel_vel.z, length(corrected));
	// vec2 corrected = (pixel_vel.xy - 0.5) * tile_uv;
	// return vec4(corrected, pixel_vel.z, length(corrected));
}

float halton(float base, vec2 frag) // https://www.gsn-lib.org/apps/raytracing/index.php?name=example_halton
{
	// float long_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
	// float short_frag = ceil(dot(dim_check, frag));

	// float long_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
	// float short_frag = ceil(dot(dim_check, frag));
	//	bool even_odd = int(short_frag) % 2 == 0;
	//	vec2 eo_vec = vec2(float(even_odd), float(!even_odd));
	//	vec2 long_vec = vec2(long_frag, float(long_dim) - long_frag);
	//	int n = int(dot(long_vec, eo_vec) + float(long_dim) * short_frag);

	int n = int(frag.x + reso.x * frag.y);
	// float base = 3.;
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
// float haltonUV(float base, vec2 uv) // https://www.gsn-lib.org/apps/raytracing/index.php?name=example_halton
//{
//	float long_frag = floor(dot(vec2(dim_check.y, dim_check.x), uv) * reso.x);
//	float short_frag = ceil(dot(dim_check, uv) * reso.y);
//
//	// float long_frag = floor(dot(vec2(dim_check.y, dim_check.x), frag));
//	// float short_frag = ceil(dot(dim_check, frag));
//	//	bool even_odd = int(short_frag) % 2 == 0;
//	//	vec2 eo_vec = vec2(float(even_odd), float(!even_odd));
//	//	vec2 long_vec = vec2(long_frag, float(long_dim) - long_frag);
//	//	int n = int(dot(long_vec, eo_vec) + float(long_dim) * short_frag);
//
//	int n = int(long_frag + float(long_dim) * short_frag);
//	// float base = 3.;
//	float r = 0.0;
//	float f = 1.0;
//	while (n > 0)
//	{
//		f = f / base;
//		r = r + f * float(n % int(base));
//		n = int(floor(float(n) / base));
//	}
//	return r;
// }
float dither(vec2 frag)
{
	vec2 dither = fract(frag / 2.);
	float dither_amt = 0.;

	if (dither.x < 0.5 && dither.y < 0.5)
		dither_amt += 0.25;
	if (dither.y < 0.5)
		dither_amt += 0.25;
	if (dither.x > 0.5 && dither.y > 0.5)
		dither_amt += 0.75;
	return dither_amt;
}

vec3 filter(vec2 uv, float j)
{
	vec3 color_p = texture(color_buffer, uv).xyz;

	//	float j = 0.5 * (halton(4, frag) + halton(3, frag));
	//	j = halton(3, frag) * halton(6, frag);
	// j = halton(3, frag); // + halton(9, frag));
	vec2 uv_frag = reso * uv;
	// float j = halton3(uv);

	vec2 j_tile_falloff = 2. * abs(0.5 - fract(uv_frag / tile_size));
	// vec2 j_tile_falloff = 2. * abs(0.5 - fract(uv / tile_uv));

	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;

	vec2 j_frag = uv_frag + mix(vec2(-1.), vec2(1.), j * j_tile_falloff);
	// vec2 j_uv = uv + mix(vec2(-1.), vec2(1.), j * j_tile_falloff) * inv_reso;
	//	j_frag = uv_frag;
	vec4 v_max = correct(texture(neighbor_buffer, j_frag / reso).xyz);
	//	v_max = correct(texture(neighbor_buffer, uv).xyz);

	// v_max = correct(texture(neighbor_buffer, j_uv).xyz);

	if (v_max.w <= 0.5)
		return color_p;
	vec4 vc = correct(texture(velocity_buffer, uv).xyz);
	if (vc.w == 0.)
		return color_p;
	vec2 norm_vc = normalize(vc.xy);

	vec2 wn = normalize(v_max.xy);

	vec2 wp = vec2(-wn.y, wn.x);
	if (dot(wp, vc.xy) < 0.) // || vc.w == 0.)
		wp = -wp;

	vec2 wc = normalize(mix(wp, norm_vc, (vc.w - 0.5) / gamma));

	float total_weight = steps / (kk * vc.w);

	//		float total_weight = steps / (kk * vc.w / (4. * tile_size));

	// total_weight = vc.w;
	// total_weight = 1. / vc.w;
	// float total_start = total_weight;

	// total_weight = 1.;

	//	if (vc.w == 0.)
	//		total_weight = 1.;
	vec3 result = color_p * total_weight;
	float j_prime = j * j_prime_term;
	for (float i = 0.; i < steps; i++)
	{
		float tee = mix(-1., 1., (i + j_prime + 1.) / (steps + 1.));
		//	tee = (i + 1.) / (steps + 1.);
		vec2 d = int(i) % 2 == 0 ? vc.xy : v_max.xy;
		// d = v_max.xy;
		//	if (int(i) % 2 == 0) // && vc.w != 0.)
		//		d = vc.xy;
		//	else
		//		d = v_max.xy;
		// float d_len = length(d);
		float big_tee = abs(tee * v_max.w);

		vec2 big_s = floor(tee * d) + uv_frag;
		//	vec2 big_suv = floor(tee * d) + uv;
		vec2 big_suv = big_s / reso;

		vec4 vs = correct(texture(velocity_buffer, big_suv).xyz);
		vec3 color_s = texture(color_buffer, big_suv).xyz;

		// vec4 vs = correct(texture(velocity_buffer, big_suv).xyz);
		// vec3 color_s = texture(color_buffer, big_suv).xyz;
		//	vec2 z_cmp = vec2(float(vc.z > vs.z), float(vs.z > vc.z));
		//	float fore = clamp(1. - (vc.z - vs.z) / min(vc.z, vs.z), 0., 1.);
		//	float back = clamp(1. - (vs.z - vc.z) / min(vs.z, vc.z), 0., 1.);
		float fore = min(max(0., 1. - (vc.z - vs.z) / min(vc.z, vs.z)), 1.);
		float back = min(max(0., 1. - (vs.z - vc.z) / min(vs.z, vc.z)), 1.);
		float weight = 0.;

		float wA = dot(wc, d);
		// wA = clamp(wA, -d_len, d_len);
		//	if (vc.w == 0. || d_len == 0.)
		//		wA = 0.;
		//	float wB = dot(normalize(vs.xy), d);
		// wA = vc.w == 0. ? 0. : dot(wc, d);
		//	float wB = (vs.w < 100.) ? 1. : dot(normalize(vs.xy), d);
		vec2 vs_norm = normalize(vs.xy * max(vs.w, 0.5) / vs.w);
		float wB = dot(vs_norm, v_max.xy);
		// wB = clamp(wB, -d_len, d_len);
		//	wB = max(wB, -1.);

		//	if (d_len == 0.)
		//		wB = 0.;
		// wB = max(wB, -0.76);
		//	if (vs.w < 0.5 && length(d) > 0.1)
		//	{
		//	}
		//	wB = dot(normalize(vec2(clamp(vs.x, -0.25, 0.25), clamp(vs.y, -0.25, 0.25))), d);
		//	wB = dot(normalize(vec2(clamp(vs.xy, vec2(-1000.), vec2(1000.)))), d);
		//	wB = dot(normalize(vs.xy), d);

		//	// vs_norm = vs_norm / length(vs_norm);
		//	if (vs.w <= 0.5)
		//	{ // if (length(vs.xy) < 0.5)
		//		bool greater_norm = vs_norm.x > vs_norm.y;
		//		vs_norm = normalize(vs.xy * 0.5 / vs.w);
		//		//	vs_norm *= 0.5 * vec2(float(greater_norm), float(!greater_norm)) * vs_norm;
		//	}
		//

		//
		//	wB = dot(vs_norm, d);
		//  wB = vs.w == 0.0 ? length(d) : dot(normalize(vs.xy), d);

		//  if (vs.w == 0.)
		//  	wB = 0.;
		//	if (vc.w == 0.)
		//		return color_p;
		float v_min = min(vs.w, vc.w);
		//	if (vc.w == 0.)
		//		v_min = vs.w;
		float cone_s = max(1. - big_tee / vs.w, 0.);
		float cone_c = max(1. - big_tee / vc.w, 0.);
		//	cone_s = clamp(1. - big_tee / vs.w, 0., 1.);
		//	cone_c = clamp(1. - big_tee / vc.w, 0., 1.);
		// float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee);
		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee);

		//	if (v_min == 0.)
		//		weight += z_cmp.x * cone_s * 0.;
		//	else
		weight += fore * cone_s * wB;
		weight += back * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;

		weight = max(weight, 0.);
		//  weight = max(weight, -2. / v_max.w);

		total_weight += weight;
		result += color_s * weight;
	}
	//	float start_weight += steps / (kk * vc.w);
	//	result += color_p * start_weight;

	// return vec3(v_max.xy / tile_size + 0.5, 0.);
	// return vec3(v_max.xy + 0.5, 0.);
	//	return vec3(j);
	//	return length(final) < 1. ? final : vec3(0.);
	return result / total_weight;
}
void vertex()
{
	UV = 1. - UV;
}
void fragment()
{

	//	return texture(neighbor_buffer, uv).xyz;
	COLOR = vec4(filter(UV, halton(3, FRAGCOORD.xy)), 1.);
	//	COLOR = vec4(vec3(dither_amt), 1.);
	//	COLOR = vec4(texture(neighbor_buffer, UV, 1.));
}