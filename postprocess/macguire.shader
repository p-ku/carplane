// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D neighbor_buffer; // Plane image of 3D world as viewed by player, used for color
uniform sampler2D color_buffer;		 // Plane image of 3D world as viewed by player, used for color
uniform sampler2D velocity_buffer; // Velocity and depth information

uniform vec2 reso;
uniform vec2 inv_reso;
uniform vec2 dim_check;
uniform int short_dim;
uniform int long_dim;
uniform float tile_size;
uniform vec2 tile_uv;

const float steps = 7., nn = 0.95, gamma = 1.5, phi = 27.;
const float kk = 40. * steps / 35.;
const float j_prime_term = nn * phi / steps;

vec4 correct(vec3 pixel_vel)
{
	vec2 corrected = (pixel_vel.xy - 0.5) * tile_size;
	return vec4(corrected, pixel_vel.z, length(corrected));
	// vec2 corrected = (pixel_vel.xy - 0.5) * tile_uv;
	// return vec4(corrected, pixel_vel.z, length(corrected));
}

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
	vec3 color_p = texture(color_buffer, uv).xyz;

	float j = halton3(frag);
	//	float j = halton3(uv);

	vec2 j_tile_falloff = 2. * abs(0.5 - fract(frag.xy / tile_size));
	// vec2 j_tile_falloff = 2. * abs(0.5 - fract(uv / tile_uv));

	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;

	vec2 j_frag = frag + mix(vec2(-1.), vec2(1.), j * j_tile_falloff);
	vec2 j_uv = uv + mix(vec2(-1.), vec2(1.), j * j_tile_falloff) * inv_reso;

	vec4 v_max = correct(texture(neighbor_buffer, j_frag / reso).xyz);
	// vec4 v_max = correct(texture(neighbor_buffer, j_uv).xyz);

	//	if (v_max.w < 0.5)
	//		return color_p;

	vec2 wn = normalize(v_max.xy);

	vec4 vc = correct(texture(velocity_buffer, uv).xyz);

	vec2 wp = vec2(-wn.y, wn.x);
	if (dot(wp, vc.xy) < 0.)
		wp = -wp;

	vec2 wc = normalize(mix(wp, normalize(vc.xy), (vc.w - 0.5) / gamma));

	float total_weight = steps / (kk * vc.w);

	vec3 result = color_p * total_weight;

	float j_prime = j * j_prime_term;
	for (float i = 0.; i < steps; i++)
	{
		float tee = mix(-1., 1., (i + j_prime + 1.) / (steps + 1.));
		vec2 d;
		if (int(i) % 2 == 0)
			d = vc.xy;
		else
			d = v_max.xy;

		float big_tee = abs(tee * v_max.w);

		vec2 big_s = floor(tee * d) + frag;
		vec2 big_suv = floor(tee * d) + uv;

		vec4 vs = correct(texture(velocity_buffer, big_s / reso).xyz);
		vec3 color_s = texture(color_buffer, big_s / reso).xyz;
		// vec4 vs = correct(texture(velocity_buffer, big_suv).xyz);
		// vec3 color_s = texture(color_buffer, big_suv).xyz;
		//	vec2 z_cmp = vec2(float(vc.z > vs.z), float(vs.z > vc.z));
		vec2 z_cmp;
		z_cmp.x = clamp(1. - (vc.z - vs.z) / min(vc.z, vs.z), 0., 1.);
		z_cmp.y = clamp(1. - (vs.z - vc.z) / min(vs.z, vc.z), 0., 1.);

		float weight = 0.;

		float wA = dot(wc, d);
		float wB = dot(normalize(vs.xy), d);

		float v_min = min(vs.w, vc.w);

		float cone_s = max(1. - big_tee / vs.w, 0.);
		float cone_c = max(1. - big_tee / vc.w, 0.);

		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, big_tee);

		weight += z_cmp.x * cone_s * wB;
		weight += z_cmp.y * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;

		total_weight += weight;
		result += color_s * weight;
	}
	//	return vec3(v_max.xy / tile_size + 0.5, 0.);
	// return vec3(v_max.xy + 0.5, 0.);
	//	return texture(neighbor_buffer, uv).xyz;
	return result / total_weight;
}

void fragment()
{
	COLOR = vec4(filter(SCREEN_UV, FRAGCOORD.xy), 1.);
}