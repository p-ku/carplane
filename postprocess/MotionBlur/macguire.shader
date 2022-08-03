// This is a blur shader. It's meant for blur on stationary objects.
// Helpful starting point:
// https://github.com/Bauxitedev/godot-motion-blur
// but it's completely different now.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D neighbor_buffer; // Plane image of 3D world as viewed by player, used for color
uniform sampler2D color_buffer;		 // Plane image of 3D world as viewed by player, used for color
uniform sampler2D velocity_buffer; // Velocity and depth information
uniform sampler2D noise;					 // Velocity and depth information

uniform vec2 reso;
uniform float tile_pixel_size;
uniform float steps;
uniform float j_prime_term;
const float eps = 0.00001;
const float kk = 40.;
const float gamma = 1.5;

vec4 correct(vec3 pixel_vel)
{
	vec2 corrected = (pixel_vel.xy - 0.5) * tile_pixel_size;
	return vec4(corrected, pixel_vel.z, length(corrected));
}

vec3 filter(vec2 uv, float j)
{
	vec3 color_p = texture(color_buffer, uv).xyz;

	vec2 uv_frag = reso * uv;

	vec2 j_tile_falloff = 2. * abs(0.5 - fract(uv_frag / tile_pixel_size));

	vec2 j_tile_cmp = vec2(float(j_tile_falloff.x >= j_tile_falloff.y), float(j_tile_falloff.x < j_tile_falloff.y));

	j_tile_falloff = j_tile_cmp * j_tile_falloff;

	vec2 j_frag = uv_frag + mix(vec2(-1.), vec2(1.), j * j_tile_falloff);

	vec4 v_max = correct(texture(neighbor_buffer, j_frag / reso).xyz);

	if (v_max.w <= 0.5)
		return color_p;
	vec4 vc = correct(texture(velocity_buffer, uv).xyz);
	if (vc.w == 0.)
		return color_p;
	vec2 norm_vc = normalize(vc.xy);

	vec2 wn = normalize(v_max.xy);

	vec2 wp = vec2(-wn.y, wn.x);
	if (dot(wp, vc.xy) < 0.)
		wp = -wp;

	vec2 wc = normalize(mix(wp, norm_vc, (vc.w - 0.5) / gamma));

	float total_weight = steps / (kk * vc.w);

	vec3 result = color_p * total_weight;

	float j_prime = j * j_prime_term;

	for (float i = 0.; i < steps; i++)
	{
		float sample_fract = mix(-1., 1., (i + j_prime + 1.) / (steps + 1.));

		vec2 sample_dir = int(i) % 2 == 0 ? vc.xy : v_max.xy;

		float sample_mag = abs(sample_fract * v_max.w);

		vec2 sample_frag = floor(sample_fract * sample_dir) + uv_frag;
		vec2 sample_uv = sample_frag / reso;

		vec4 vs = correct(texture(velocity_buffer, sample_uv).xyz);
		vec3 color_s = texture(color_buffer, sample_uv).xyz;

		vec2 vs_norm = normalize(vs.xy * max(vs.w, 0.5) / vs.w);

		float wA = dot(wc, sample_dir);
		float wB = dot(vs_norm, sample_dir);

		float v_min = min(vs.w, vc.w);

		float cone_s = max(1. - sample_mag / vs.w, 0.);
		float cone_c = max(1. - sample_mag / vc.w, 0.);

		float cylinder = 1. - smoothstep(0.95 * v_min, 1.05 * v_min, sample_mag);

		float fore = min(max(0., 1. - (vc.z - vs.z) / min(vc.z, vs.z)), 1.);
		float back = min(max(0., 1. - (vs.z - vc.z) / min(vs.z, vc.z)), 1.);
		float weight = 0.;

		weight += fore * cone_s * wB;
		weight += back * cone_c * wA;
		weight += cylinder * max(wA, wB) * 2.;

		weight = max(weight, 0.);

		total_weight += weight;
		result += color_s * weight;
	}

	return result / total_weight;
}
void vertex()
{
	UV.y = 1. - UV.y;
}
void fragment()
{
	COLOR = vec4(filter(UV, texture(noise, UV).r), 1.);
	// COLOR = vec4(texture(velocity_buffer, UV).xy, 0., 1.);
}