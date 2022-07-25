// Original version of this shader found here: https://github.com/Dimev/Realistic-Atmosphere-Godot-and-UE4
// Atmo and cloud combined shader, "twin" because it ray marches two steps

shader_type spatial;
render_mode depth_test_disable, depth_draw_never, unshaded;

const vec3 beta_ray0 = vec3(.0519673, .121427, .296453); // the amount rayleigh scattering scatters the co
const vec3 beta_mie0 = vec3(.21);												 // the amount mie scattering scatters colors
const float g = 0.76;																		 // the direction mie scatters the light in (like a cone). closer to -1 means more towards a single direction
const float gg = g * g;
const float pi = 3.14159;
const float phase_ray_const = 3. / (16. * pi);
const float phase_mie_const = phase_ray_const * 2.;
const float intensity = 2.;								// how bright the light is, affects the brightness of the atmosphere
const vec2 scale_height = vec2(8.5, 1.2); // how high do you have to go before there is no rayleigh scattering?
// the same, but for mie
const mat4 inv_mat0 = mat4(vec4(0.), vec4(0.), vec4(0.), vec4(0., 0., -1., 0.));

uniform float quality;
uniform float half_vert_reso;
uniform float tangent_dist;				// distance from tangent of planet to atmo
uniform float plan_rad = 26.0;		// the radius of the planet
uniform float plan_rad_sq = 676.; // the radius of the planet
uniform float cam_dist;						// distance from tangent of planet to atmo
uniform float cam_alt;						// distance from tangent of planet to atmo
uniform float plan_res_depth_vec;
uniform sampler2D velocity_buffer; // Velocity and depth information
uniform sampler2D car_mask;				 // Velocity and depth information
uniform float atmo_height;
uniform float atmo_height_sq;
uniform float atmo_rad; // the radius of the atmosphere
uniform float dist_factor;
uniform float atmo_rad_sq; // the radius of the atmosphere
uniform float f1;
uniform float f2;
uniform float f3;
uniform float f4;
uniform vec3 light_direction = vec3(0., 0., 1.);

// varying flat float cam_alt;
varying flat mat4 ipm;
varying flat float startMagSq;
varying flat float startMag;

vec4 light_step(vec3 pos, vec3 light_dir, int steps, float radius, float b, float d)
{

	float ray_length_l = (-b + sqrt(d)) * 0.5;
	float step_size_l = ray_length_l / float(steps);

	float ray_pos_l = 0.0;
	vec4 opt_l = vec4(0.0);

	for (int l = 0; l < steps; ++l)
	{
		vec3 pos_l = pos + (ray_pos_l + step_size_l * 0.5) * light_dir;
		float height_l = length(pos_l) - plan_rad;

		if (height_l < 0.)
		{
			opt_l = vec4(1. / 0.);
			break;
		}
		opt_l.xy += exp(-height_l / scale_height) * step_size_l;
		ray_pos_l += step_size_l;
	}

	return opt_l;
}

vec4 calculate_scattering(
		vec3 start,			// the start of the ray (the camera position)
		vec3 dir,				// the pixel direction
		float max_dist, // the maximum distance the ray can travel (because something is in the way, like an object)
		vec3 light_dir, // the direction of the light
		float depth,
		vec2 uv)
{
	// calculate the start and end position of the ray, as a distance along the ray
	// we do this with a ray sphere intersect
	float b = 2.0 * dot(dir, start);
	float c = startMagSq - atmo_rad_sq;
	float bb = b * b;
	float d = bb - 4.0 * c;

	//	// stop early if there is no intersect
	if (d < 0.0)
		return vec4(0.);

	//	 calculate the ray length
	vec2 thru = vec2(
			(-b - sqrt(d)) * 0.5,
			(-b + sqrt(d)) * 0.5);

	bool hits_object = depth < 1.;

	vec2 ray_length = vec2(max(thru.x, 0.0), hits_object ? max_dist : thru.y);
	//	if (dist_to_surf < max_dist + 2. && dist_to_surf > max_dist - 2.)
	//		ray_length.y = dist_to_surf;
	//  if the ray did not hit the atmosphere, return a black color
	if (ray_length.x > ray_length.y)
		return vec4(0.);

	float ray_pos_i = ray_length.x;

	// these are the values we use to gather all the scattered light
	//	float full_path = ray_length.y - ray_length.x;
	// float thru_path = thru.y - thru.x;
	float full_path = ray_length.y - ray_length.x;
	float steps_raw = full_path;
	if (hits_object)
	{
		float cp = startMagSq - plan_rad_sq;
		float dp = bb - 4.0 * cp;
		//	full_path = to_planet_surf < max_dist + 2. ? to_planet_surf : max_dist;
		//
		//	full_path = cam_alt > atmo_height * 0.4 && to_planet_surf < max_dist + 2. ? to_planet_surf : max_dist;
		//
		//	if (cam_alt > atmo_height * 0.4 && to_planet_surf < max_dist + 2.)
		//		full_path = mix(to_planet_surf, max_dist, (cam_alt - atmo_height * 0.4) / (atmo_height * 0.1));
		//	else
		//		full_path = max_dist;
		if (dp > 0.)
		{
			float to_planet_surf = (-b - sqrt(dp)) * 0.5;
			//	if (to_planet_surf > 0.)
			//	if (to_planet_surf < max_dist + 2.)
			//		full_path = mix(max_dist, to_planet_surf, clamp((cam_alt - atmo_height * 0.4) / (atmo_height * 0.1), 0., 1.));
			//		if (to_planet_surf < max_dist + 2. && cam_alt > atmo_height * 0.5)
			if (to_planet_surf < max_dist + 3. && texture(car_mask, uv).a == 0.)
			{
				full_path = to_planet_surf - ray_length.x;
				steps_raw = startMag;
			}
		}
		//	full_path = to_planet_surf < max_dist + 2. ? to_planet_surf : max_dist;
		//
		//	full_path = mix(max_dist, to_planet_surf, 2. * (cam_alt - 0.5) / atmo_height);
	}

	// initialize the optical depth. This is used to calculate how much air was in the ray
	vec2 opt_i = vec2(0.);

	// Calculate the Rayleigh and Mie phases.
	// This is the color that will be scattered for this ray
	// mu, mumu and gg are used quite a lot in the calculation, so to speed it up, precalculate them
	float mu = dot(dir, light_dir);
	float mumu = mu * mu;

	float phase_mie_numer = phase_mie_const * ((1.0 - gg) * (mumu + 1.0));
	float phase_mie_denom = pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg);
	float phase_ray = phase_ray_const + phase_ray_const * mumu;

	float phase_mie = depth == 1. ? phase_mie_numer / phase_mie_denom : 0.0;
	phase_mie = phase_mie_numer / phase_mie_denom;

	vec3 ray_constant = phase_ray * beta_ray0;
	vec3 mie_constant = phase_mie * beta_mie0;

	mat4 cloud_ray = mat4(0.);
	mat4 cloud_mie = mat4(0.);

	//	float steps_raw = 0.3 * atmo_height * full_path / tangent_dist;
	//
	//	if (hits_object)
	//		// if (max_dist == ray_length.y)
	//		steps_raw = max(steps_raw, 0.15 * atmo_height);
	//
	//	steps_raw /= (1. + ray_length.x / full_path);

	//	if (hits_object)
	//	{
	//		steps_raw = 0.3 * atmo_height;
	//		steps_raw /= (1. + (max(length(start) - atmo_rad, 0.)) / tangent_dist);
	//		steps_i = int(round(steps_raw));
	//		step_size_i = tangent_dist / float(steps_i);
	//		half_step_i = 0.5 * step_size_i;
	//	}

	//	float steps_raw = 0.3 * atmo_height * full_path / atmo_rad;
	//
	//	// if (hits_object)
	//	if (max_dist == ray_length.y)
	//		steps_raw = max(steps_raw, 0.15 * atmo_height);
	//
	//	steps_raw /= (1. + ray_length.x / full_path);
	//
	//	int steps_i = int(round(steps_raw));
	//	float step_size_i = full_path / float(steps_i);
	//	float half_step_i = 0.5 * step_size_i;

	//	float steps_raw = hits_object ? dist_to_surf : full_path;
	// float steps_raw = full_path;
	// if (ray_length.x > 0.)
	// steps_raw -= ray_length.x / full_path;

	steps_raw *= quality;
	// steps_raw -= min(ray_length.x, atmo_rad * 0.1); /// atmo_rad;

	// steps_raw = full_path * 0.3 - ray_length.x;

	// float steps_raw = full_path * 0.3 - 1. / (ray_length.x + 1.);
	// steps_raw /= (1. + ray_length.x / steps_raw);
	// int steps_i = hits_object ? int(steps_raw + 2.) : int(floor(steps_raw));
	//	int steps_i = int(round(steps_raw));
	int steps_i = hits_object ? int(max(steps_raw, 1.)) : int(round(max(steps_raw, 0.)));
	// int steps_i = int(ceil(steps_raw));

	//	if (steps_raw > float(steps_i) + 1.)
	//		steps_raw -= (ray_length.x - 1.) / full_path;
	float step_size_i = full_path / float(steps_i);
	float half_step_i = 0.5 * step_size_i;

	for (int i = 0; i < steps_i; ++i)
	{

		vec3 pos_i = start + dir * (ray_pos_i + half_step_i);
		float height_i = length(pos_i) - plan_rad;
		vec2 density = exp(-height_i / scale_height) * step_size_i;
		b = 2.0 * dot(dir, pos_i);
		c = dot(pos_i, pos_i) - atmo_rad_sq;
		d = b * b - 4.0 * c;
		vec4 opt_l = light_step(pos_i, light_dir, 4, atmo_rad, b, d);

		opt_i += density;
		vec3 attn = exp(-(1.1 * beta_mie0 * (opt_i.y + opt_l.y) + beta_ray0 * (opt_i.x + opt_l.x)));

		cloud_ray[0].xyz += attn * density.x;
		cloud_mie[0].xyz += attn * density.y;

		ray_pos_i += step_size_i;
	}
	float opacity = 1. - length(exp(-(beta_ray0 * opt_i.x + 1.1 * beta_mie0 * opt_i.y)));

	//	opacity *= plan_rad * plan_rad / ((cam_alt - plan_rad) * (cam_alt - plan_rad));
	//	opacity -= (max_dist * max_dist - plan_rad) / dist_factor;
	//  opacity = min(opacity / max_dist, 0.3);
	//	opacity = hits_object && max_dist > plan_rad * 2. ? min(opacity, 0.25) : opacity;
	vec4 sky_color = vec4(ray_constant * cloud_ray[0].xyz + mie_constant * cloud_mie[0].xyz, opacity);

	sky_color.xyz *= intensity;

	return sky_color;
}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
	//	UV = UV + 1.;

	ipm = inv_mat0;
	ipm[0][0] = f1;
	ipm[1][1] = f2;
	ipm[2][3] = f3;
	ipm[3][3] = f4;
	startMagSq = dot(CAMERA_MATRIX[3].xyz, CAMERA_MATRIX[3].xyz);
	startMag = length(CAMERA_MATRIX[3].xyz);
}

void fragment()
{
	float depth = texture(velocity_buffer, SCREEN_UV).z;
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	//	float visual_scale = half_vert_reso - plan_res_depth_vec / view.z;

	vec4 atm = calculate_scattering(CAMERA_MATRIX[3].xyz, normalize(mat3(CAMERA_MATRIX) * view.xyz), length(view.xyz), light_direction, depth, SCREEN_UV);

	atm.w = clamp(atm.w, 0.000000001, 1.);

	ALBEDO = atm.xyz / atm.w;

	ALPHA = atm.w;
}