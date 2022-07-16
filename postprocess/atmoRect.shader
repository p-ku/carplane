// Original version of this shader found here: https://github.com/Dimev/Realistic-Atmosphere-Godot-and-UE4
// Atmo and cloud combined shader, "twin" because it ray marches two steps

shader_type canvas_item;
render_mode unshaded;
// shader_type canvas_item;
// render_mode unshaded;
//  render_mode blend_mix, depth_test_disable, unshaded;
//   render_mode blend_mix, shadows_disabled, unshaded;
//   render_mode blend_add, ambient_light_disabled, shadows_disabled, unshaded;
//   render_mode blend_add, depth_draw_always, unshaded;
const float intensity = 2.;		 // how bright the light is, affects the brightness of the atmosphere
uniform float plan_rad = 26.0; // the radius of the planet
uniform vec2 fov;							 // the radius of the planet
uniform float reso;						 // the radius of the planet
uniform mat4 cam_xform;				 // the radius of the planet

const float cloud_rad = 31.0;
const float cloud_rad_sq = cloud_rad * cloud_rad;
const float height_c = 5.;
uniform sampler2D velocity_buffer; // Velocity and depth information
uniform sampler2D color_buffer;		 // Velocity and depth information

uniform sampler2D camera_buffer; // Velocity and depth information
uniform float atmo_height;
uniform float atmo_rad;		 // the radius of the atmosphere
uniform float atmo_rad_sq; // the radius of the atmosphere
// const vec3 beta_ray0 = vec3(.051, .135, .331); // the amount rayleigh scattering scatters the colors (for earth: causes the blue atmosphere)
// const vec3 beta_mie0 = vec3(.21);							 // the amount mie scattering scatters colors
// const vec3 beta_ray0 = vec3(.519673e-5, .121427e-4, .296453e-4); // the amount rayleigh scattering scatters the colors (for earth: causes the blue atmosphere)
// const vec3 beta_mie0 = vec3(21e-4);															 // the amount mie scattering scatters colors
const vec3 beta_ray0 = vec3(.0519673, .121427, .296453); // the amount rayleigh scattering scatters the co
const vec3 beta_mie0 = vec3(.21);												 // the amount mie scattering scatters colors
const float g = 0.76;																		 // the direction mie scatters the light in (like a cone). closer to -1 means more towards a single direction
const float gg = g * g;
const float thickness = 0.05;
const float pi = 3.14159;
uniform float tangent_dist;				// distance from tangent of planet to atmo
uniform float cloud_tangent_dist; // distance from tangent of planet to atmo
const float phase_ray_const = 3. / (16. * pi);
const float phase_mie_const = phase_ray_const * 2.;
const float beta_const = 8. * pi * pi * pi / 3.;
const float refract = 1.00029;
const float mole_n = 2.504e25;
const float mole_n_cloud = 4e8;
const float re_sq = 6e-6 * 6e-6;

const mat3 amat = mat3(vec3(-1., 1., 0.), vec3(-1., 1., 0.), vec3(0., 0., 1.));
const mat3 at = mat3(vec3(-1., -1., 0.), vec3(1., 0., 0.), vec3(0., 1., 1.));

const float extinc0 = mole_n * pi;
const vec2 scale_height = vec2(8.5, 1.2); // how high do you have to go before there is no rayleigh scattering?
// the same, but for mie
const mat4 inv_mat0 = mat4(vec4(0.), vec4(0.), vec4(0.), vec4(0., 0., -1., 0.));
uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;
// uniform sampler3D noise_vol;
// uniform sampler2D noise_tex2;

varying float Rz;
varying vec3 light_direction;

vec4 light_step(vec3 pos, vec3 light_dir, int steps, float radius, float b, float d)
{

	float ray_length_l = 0.;
	float step_size_l = 0.;

	ray_length_l = (-b + sqrt(d)) * 0.5; // / float(steps);
	step_size_l = ray_length_l / float(steps);

	float ray_pos_l = 0.0;
	vec4 opt_l = vec4(0.0);

	float height_l = 1.;
	vec3 pos_l;
	for (int l = 0; l < steps; ++l)
	{
		pos_l = pos + (ray_pos_l + step_size_l * 0.5) * light_dir;
		float pos_l_len = length(pos_l);
		height_l = pos_l_len - plan_rad;

		if (height_l < 0.)
		{
			opt_l = vec4(1. / 0.);
			break;
		}
		opt_l.xy += exp(-height_l / scale_height) * step_size_l;
		ray_pos_l += step_size_l;
	}
	// opt_l.z = height_l;
	// opt_l.w = step_size_l;

	// MARCH THE CLOUD using POS_L
	return opt_l;
}

vec4 calculate_scattering(
		vec3 start,			// the start of the ray (the camera position)
		vec3 dir,				// the direction of the ray (the camera vector)
		float max_dist, // the maximum distance the ray can travel (because something is in the way, like an object)
		vec3 light_dir, // the direction of the light
)
{
	// calculate the start and end position of the ray, as a distance along the ray
	// we do this with a ray sphere intersect

	float startMagSq = dot(start, start);
	float b = 2.0 * dot(dir, start);
	float c = startMagSq - atmo_rad_sq;
	float bb = b * b;
	float d = bb - 4.0 * c;

	//	// stop early if there is no intersect
	if (d < 0.0)
		return vec4(0.); // vec4(0.0);

	//	 calculate the ray length
	vec2 thru = vec2(
			(-b - sqrt(d)) * 0.5,
			(-b + sqrt(d)) * 0.5);

	vec2 ray_length = vec2(
			max(thru.x, 0.0),				// + atmo_rad			  * 0.00001,
			min(thru.y, max_dist)); // - atmo_rad  * 0.00001);

	// ray_length = sphere_int(start, dir, atmo_rad, 2);
	//  if the ray did not hit the atmosphere, return a black color
	if (ray_length.x > ray_length.y)
		return vec4(0.);

	// prevent the mie glow from appearing if there's an object in front of the camera
	bool planet_facing = max_dist == ray_length.y;
	// float step_size_i = (ray_length.y - ray_length.x) / float(steps_i);
	// float cloud_rad = plan_rad + height_c;

	float ray_pos_i = ray_length.x;

	// these are the values we use to gather all the scattered light
	vec4 sky_color = vec4(0.);
	vec4 opacity;

	vec4 col_thr;
	vec4 cloud_sum = vec4(0.);

	vec4 cloud_color;
	float full_path = ray_length.y - ray_length.x;
	float thru_path = thru.y - thru.x;

	// initialize the optical depth. This is used to calculate how much air was in the ray

	mat4 opt_i = mat4(0.);

	// also init the scale height, avoids some vec2's later on

	// loat cloud_rad = plan_rad + height_c;

	// float chord_d_sq = atmo_rad_sq - thru_path * thru_path * 0.25;
	// float chord_d = sqrt(chord_d_sq);

	//	if (chord_d < cloud_rad) // clouds possible
	//

	// Calculate the Rayleigh and Mie phases.
	// This is the color that will be scattered for this ray
	// mu, mumu and gg are used quite a lot in the calculation, so to speed it up, precalculate them
	float mu = dot(dir, light_dir);
	float mumu = mu * mu;
	// float phase_ray = 3.0 / (50.2654824574 /* (16 * pi) */) * (1.0 + mumu);
	// float phase_mie = !planet_facing ? 3.0 / (25.1327412287 /* (8 * pi) */) * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg)) : 0.0;
	// float phase_ray = 3.0 / 50.2654824574 * (1.0 + mumu);
	// float phase_mie = !planet_facing ? 3.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg)) : 0.0;
	float phase_mie_numer = phase_mie_const * ((1.0 - gg) * (mumu + 1.0));
	float phase_mie_denom = pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg);
	float phase_ray = phase_ray_const + phase_ray_const * mumu;
	//	float phase_mie = !planet_facing ? phase_mie_numer / phase_mie_denom : 0.0;
	float phase_mie = phase_mie_numer / phase_mie_denom;

	// float cloud_phase_mie_back = 3.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg + 2.0 * mu * g, 1.5) * (2.0 + gg));
	// cloud_phase_mie_back = 0.;
	// float cloud_phase_mie_one = phase_mie_numer / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg));

	// vec3 cloud_mie_constant_two = cloud_phase_mie_two * beta_mie0;

	vec3 ray_constant = phase_ray * beta_ray0;
	vec3 mie_constant = phase_mie * beta_mie0;

	vec3 opt_i_tot = vec3(0.);

	mat4 cloud_ray = mat4(0.);
	mat4 cloud_mie = mat4(0.);
	vec3 total_ray = vec3(0.);
	vec3 total_mie = vec3(0.);

	float ray_length_l;
	float height_l;
	float step_size_l;
	int steps_l = 4;
	float ray_pos_l = 0.0;
	vec4 opt_l = vec4(0.0);

	vec3 attn;

	//	float steps_raw;
	// if (planet_facing)
	//	steps_raw = 16.0 - 4. * ray_length.x / ray_length.y;
	// else
	//	steps_raw = (16. * full_path / tangent_dist) - 8. * ray_length.x / ray_length.y;
	// steps_raw = ((16. * full_path - 3. * ray_length.x) / tangent_dist); // - (ray_length.x / tangent_dist);

	// float steps_raw = ((16. * full_path) / tangent_dist) / (1. + ray_length.x / full_path); // - (ray_length.x / tangent_dist);
	float steps_raw = 0.3 * atmo_height * full_path / tangent_dist;

	if (planet_facing)
		steps_raw = max(steps_raw, 0.15 * atmo_height);

	steps_raw /= (1. + ray_length.x / full_path);

	//	if (planet_facing && ray_length.y < plan_rad * 2.)
	//		steps_raw = max(steps_raw, 8.);
	//	else
	//		steps_raw = (16. * full_path / tangent_dist) - 8. * ray_length.x / ray_length.y;

	int steps_i = int(round(steps_raw));
	float step_size_i = full_path / float(steps_i);
	float half_step_i = 0.5 * step_size_i;

	vec3 pos_i;
	float height_i;

	//	b = 2.0 * dot(light_dir, pos_c1);
	//	d = b * b;
	//	vec4 opt_c_one = light_step(pos_c1, light_dir, 4, cloud_rad, b, d);
	//	b = 2.0 * dot(light_dir, pos_c2);
	//	d = b * b;
	//	vec4 opt_c_two = light_step(pos_c2, light_dir, 4, cloud_rad, b, d);
	vec3 cloud_raw = vec3(0.);
	ivec3 cloud_steps;

	vec3 cloud_step_size;
	vec3 cloud_half_step;
	vec3 cloud_path;
	vec3 cloud_steps_raw;

	cloud_steps.x = int(round(cloud_steps_raw.x));
	cloud_steps.y = steps_i - cloud_steps.x;

	cloud_step_size = cloud_path / vec3(cloud_steps);
	cloud_step_size = vec3(step_size_i);

	cloud_half_step = cloud_step_size * 0.5;

	for (int i = 0; i < steps_i; ++i)
	{
		pos_i = start + dir * (ray_pos_i + half_step_i);
		height_i = length(pos_i) - plan_rad;
		vec2 density = exp(-height_i / scale_height) * step_size_i;
		b = 2.0 * dot(dir, pos_i);
		c = dot(pos_i, pos_i) - atmo_rad_sq;
		d = b * b - 4.0 * c;
		opt_l = light_step(pos_i, light_dir, 4, atmo_rad, b, d);

		// float dt = max(0.05, 0.04 * t);

		opt_i[0].xy += density;
		opt_i_tot.xy += density;
		attn = exp(-(1.1 * beta_mie0 * (opt_i_tot.y + opt_l.y) + beta_ray0 * (opt_i_tot.x + opt_l.x)));

		cloud_ray[0].xyz += attn * density.x;
		cloud_mie[0].xyz += attn * density.y;

		ray_pos_i += step_size_i;
	}
	opacity[0] = 1. - length(exp(-(beta_ray0 * opt_i_tot.x + 1.1 * beta_mie0 * opt_i_tot.y)));
	sky_color = vec4(ray_constant * cloud_ray[0].xyz + mie_constant * cloud_mie[0].xyz, opacity[0]);
	//	sky_color.xyz *= intensity;
	//	return sky_color;

	// ALBEDO = (cloud_frag + (1. - cloud_alpha) * skyColor.xyz);
	// ALPHA = (cloud_alpha + (1. - cloud_alpha) * skyColor[3]);
	sky_color.xyz *= intensity;
	// return sky_color;
	// if (cloud_color.x != 0.)
	//	return vec4(sky_color.xyz * intensity, cloud_color.a) + (1. - cloud_color.a) * sky_color.a;
	//// return vec4(sky_color.xyz * intensity, cloud_color.a) + (1. - cloud_color.a) * sky_color.a;
	// else
	return sky_color;
}

vec3 rayDirection(float fieldOfView, vec2 size, vec2 fragCoord)
{
	vec2 xy = fragCoord - size * 0.5;
	float z = 0.5 * size.y / tan(radians(fieldOfView));

	return vec3(xy, -z);
}

// void vertex()
//{
//	POSITION = vec4(VERTEX, 1.0);
//	UV = UV + 1.;
// }

void fragment()
{
	mat4 ipm = inv_mat0;
	ipm[0][0] = f1;
	ipm[1][1] = f2;
	ipm[2][3] = f3;
	ipm[3][3] = f4;
	//	Rz = 0.;
	//	float y = sin(UV.y * pi - pi / 2.);
	//	float x = cos(UV.x * 2. * pi) * cos(UV.y * pi - pi / 2.);
	//	float z = sin(UV.x * 2. * pi) * cos(UV.y * pi - pi / 2.);
	//
	//	y = y * 0.5 + 0.5;
	//	x = x * 0.5 + 0.5;
	//	z = z * 0.5 + 0.5;
	// vec4 cloud_tex = texture(noise_vol, vec3(x, y, z));

	//	float depth = texture(velocity_buffer, SCREEN_UV).z;
	//	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	//	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	//	view.xyz /= view.w;

	float depth = texture(velocity_buffer, SCREEN_UV).z;
	vec2 ndc = SCREEN_UV * 2.0 - 1.0;
	vec2 view_angle = ndc * fov;
	vec2 view2 = vec2(depth * 0.5 * sin(2. * view_angle));
	vec3 view = vec3(view2, -view2.x / tan(view_angle.x));

	// cam_position = CAMERA_MATRIX[3].xyz;
	// vec4 world = CAMERA_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	light_direction = vec3(0., 0., 1.);
	//	vec3 dir = rayDirection(35.0, VIEWPORT_SIZE, FRAGCOORD.xy);
	// vec3 dir = view.xyz;
	//	dir = normalize(dir.x * CAMERA_MATRIX[0].xyz + dir.y * CAMERA_MATRIX[1].xyz + dir.z * CAMERA_MATRIX[2].xyz);
	// dir = normalize(dir.x * cam_mat[0].xyz + dir.y * cam_mat[1].xyz + dir.z * cam_mat[2].xyz);
	// float max_distance = length(view.xyz);

	//			vec4 atm = calculate_scattering(CAMERA_MATRIX[3].xyz, dir, length(view.xyz), light_direction);
	//	vec4 atm = calculate_scattering(CAMERA_MATRIX[3].xyz, normalize(mat3(CAMERA_MATRIX) * view.xyz), length(view.xyz), light_direction);
	// vec4 atm = calculate_scattering(CAMERA_MATRIX[3].xyz, normalize(mat3(CAMERA_MATRIX) * view.xyz), depth, light_direction, depth);
	vec4 atm = calculate_scattering(cam_xform[3].xyz, normalize(mat3(cam_xform) * view.xyz), depth, light_direction);

	atm.w = clamp(atm.w, 0.000000001, 1.);
	//	atm.w = clamp(atm.w, 0.07, 1.);

	// float clamp_r = clamp(atm.w, beta_ray0.r, 1.);
	// float clamp_g = clamp(atm.w, beta_ray0.g, 1.);
	// float clamp_b = clamp(atm.w, beta_ray0.b, 1.);

	// atm.xyz = pow(max(atm.xyz, 0.), vec3(1. / 2.2));
	// atm.xyz / atm.w;
	// COLOR = vec4(atm.xyz / atm.w, atm.w);
	// ALBEDO = vec3(atm.xyz / atm.w);

	//	ALBEDO.r = atm.r / clamp_r;
	//	ALBEDO.g = atm.g / clamp_g;
	//	ALBEDO.b = atm.b / clamp_b;

	// ALPHA = atm.w;
	vec3 col = texture(color_buffer, SCREEN_UV).xyz;
	COLOR = vec4(clamp(col + atm.xyz, 0., 1.), 1.);
	//	ALPHA = clamp(atm.w, 0.09, 1.);
}