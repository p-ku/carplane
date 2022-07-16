// Original version of this shader found here: https://github.com/Dimev/Realistic-Atmosphere-Godot-and-UE4

shader_type spatial;
render_mode blend_mix, depth_test_disable, unshaded;
// render_mode blend_mix, shadows_disabled, unshaded;
// render_mode blend_add, ambient_light_disabled, shadows_disabled, unshaded;
// render_mode blend_add, depth_draw_always, unshaded;
uniform float intensity = 1.;	 // how bright the light is, affects the brightness of the atmosphere
uniform float plan_rad = 26.0; // the radius of the planet
uniform float cloud_rad = 31.0;
uniform float height_c = 5.;
uniform float atmo_rad = 32.0;									// the radius of the atmosphere
uniform vec3 beta_ray = vec3(.038, .135, .331); // the amount rayleigh scattering scatters the colors (for earth: causes the blue atmosphere)
uniform vec3 beta_mie = vec3(.21);							// the amount mie scattering scatters colors
uniform vec3 beta_ambient = vec3(0.0);					// the amount of scattering that always occurs, can help make the back side of the atmosphere a bit brighter
uniform float g = 0.76;													// the direction mie scatters the light in (like a cone). closer to -1 means more towards a single direction
uniform float height_ray = 0.5;									// how high do you have to go before there is no rayleigh scattering?
uniform float height_mie = 0.25;								// the same, but for mie
uniform float density_multiplier = 1.0;					// how much extra the atmosphere blocks light
uniform mat3 m = mat3(vec3(0.00, 0.80, 0.60),
											vec3(-0.80, 0.36, -0.48),
											vec3(-0.60, -0.48, 0.64));
uniform float thickness = 2.;
uniform sampler3D noise_vol;
uniform sampler2D noise_tex2;

uniform float pi = 3.14159;

uniform sampler2D cloud_noise;

varying float Rz;
varying vec3 cam_position;
varying vec3 light_direction;
float hash(float n)
{
	return fract(sin(n) * 43758.5453);
}
float noise(in vec3 x)
{

	vec3 p = floor(x);
	vec3 f = x - p;

	f = smoothstep(0.0, 1.0, f);

	x = p + f;
	return textureLod(noise_vol, (0.5 * x / (atmo_rad) + 0.5), 0.0).x * 2.0 - 1.0;

	//	float n = p.x + p.y * 57.0 + 113.0 * p.z;
	//	float res = mix(mix(mix(hash(n + 0.0), hash(n + 1.0), f.x),
	//											mix(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
	//									mix(mix(hash(n + 113.0), hash(n + 114.0), f.x),
	//											mix(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
	//									f.z);
	//	return res;
}
float fbm(vec3 p)
{
	float f;
	f = 0.5000 * noise(p);
	p = m * p * 2.02;
	f += 0.2500 * noise(p);
	p = m * p * 2.03;
	f += 0.1250 * noise(p);
	p = m * p * 2.01;
	f += 0.0625 * noise(p);
	return f;
}
float map(in vec3 p, int oct)
{
	vec3 q = p;
	float g_map = 0.5 + 0.5 * noise(q * 0.3);

	float f;
	f = 0.50000 * noise(q);
	q = q * 2.02;
	if (oct >= 2)
		f += 0.25000 * noise(q);
	q = q * 2.23;
	if (oct >= 3)
		f += 0.12500 * noise(q);
	q = q * 2.41;
	if (oct >= 4)
		f += 0.06250 * noise(q);
	q = q * 2.62;
	if (oct >= 5)
		f += 0.03125 * noise(q);
	f = mix(f * 0.1 - 0.75, f, g_map * g_map) + 0.1;

	return f + 0.5;
}

vec3 fbm3(vec3 p)
{
	p += TIME;
	//	if (ANIM)
	//		p += iTime;
	float fx = map(p, 5);
	float fy = map(p + vec3(1345.67, 0, 45.67), 5);
	float fz = map(p + vec3(0, 4567.8, -123.4), 5);
	return vec3(fx, fy, fz);
}
vec3 perturb3(vec3 p, float scaleX, float scaleI)
{
	scaleX *= 2.;
	return scaleI * scaleX * fbm3(p / scaleX); // usually, to be added to p
																						 // return 1. * 1. * fbm3(p / 1.); // usually, to be added to p
}
vec4 intersectSphere(vec3 rpos, vec3 rdir)
{
	vec3 op = vec3(0.0, 0.0, 0.0) - rpos;
	// float rad = 0.3;

	float eps = 1e-5;
	float b = dot(op, rdir);
	float det = b * b - dot(op, op) + cloud_rad * cloud_rad;

	if (det > 0.0)
	{
		det = sqrt(det);
		float t = b - det;
		if (t > eps)
		{
			vec4 P = vec4(normalize(rpos + rdir * t), t);
			//			Rz = cloud_rad * P.z; // 1/2 ray length inside object
			//														//#if LINEAR_DENSITY
			//			// skin layer counts less
			//			float dH = 1. + H * (H - 2. * cloud_rad) / (Rz * Rz);
			//			if (dH > 0.) // core region
			//				Rz *= .5 * (1. + sqrt(dH));
			//			else
			//				Rz *= .5 * rad * (1. - sqrt(1. - Rz * Rz / (cloud_rad * cloud_rad))) / H;
			//#endif
			return P;
		}
	}

	return vec4(0.0);
}
vec4 cloud_march(float den, vec3 pos, vec3 sun_dir, vec3 bgcol, float step, float cloud_left, float light_den)
{
	// float dif = clamp((den - noise(pos + 0.1 * sun_dir)) / 0.1, 0.0, 1.0);
	//	vec3 lin = vec3(0.65, 0.65, 0.75) * 1.1 + 0.8 * vec3(1.0, 0.6, 0.3) * dif;
	// vec3 lin = vec3(light_den);

	// vec4 col = vec4(mix(vec3(1.0, 0.95, 0.8), vec3(0.25, 0.3, 0.35), den), den);
	// vec4 col = vec4(mix(vec3(1.0), vec3(0.), den), den);
	vec4 col = vec4(vec3(1.0), den);

	col.xyz *= light_den;
	// fog
	// col.xyz = mix(col.xyz, bgcol, 1.0 - exp2(-0.075 * cloud_left));
	// composite front to back
	// col.w = min(col.w, 1.0);
	// col.w = 0.01;
	col.rgb *= col.a;
	return col;
	// sum += col * (1.0 - sum.a);
}
vec4 intersectCloud(vec3 pos, float cloud_length, vec3 start, vec3 dir, vec3 light_dir)
{
	vec3 outer_pos = pos + normalize(pos) * thickness;
	float eps = 1e-5;
	float cloud_den = 2. * map(outer_pos, 5);

	// outer_pos = outer_pos + perturb3(outer_pos, 1.5, 1.5) + textureLod(noise_vol, (0.5 * (start + dir * cloud_length) / (atmo_rad) + 0.5), 0.0).x * 2.0 - 1.0;
	// outer_pos *= cloud_den;
	//		float t = b - det; length?
	// if (cloud_den > 0.001)
	float again = length(outer_pos) - cloud_rad;

	if (cloud_den > 0.7)
	{
		// vec3 P = normalize(dir * cloud_lengthdot * (dir * cloud_length, light_dir));

		float NDotL = dot(normalize(outer_pos), light_dir);
		float NDotO = dot(-normalize(outer_pos), normalize(start));
		float LDotO = dot(light_dir, start);

		//#if LINEAR_DENSITY
		// float transmittance = linearDensityTransmittance(NDotL, NDotO, LDotO, FragCoord, iResolution) * .5;
		float transmittance = 0.075 * cloud_den * NDotL; // / (NDotL + NDotO);
		//#else
		//       float transmittance = constantDensityTransmittance(NDotL,NDotO);
		//#endif
		float cloud_alpha = 1. - exp(-8. * cloud_den);
		//	cloud_alpha *= -NDotL;
		return vec4(vec3(transmittance), cloud_alpha);
		//					 ALBEDO = (cloud_frag + (1. - cloud_alpha) * skyColor.xyz);
		//  ALPHA = (alpha + (1. - cloud_alpha) * skyColor[3]);
	}
	return vec4(0.);
}

float cloud(float h, vec3 pos, float top, float bot)
{
	float first = smoothstep(bot, top, h + bot) * sin(pi * min(h, top - bot) / (top - bot));
	return first;
}
vec2 sphere_int(vec3 pos, vec3 dir, float radius, int choose)
{
	float b = 2.0 * dot(dir, pos);
	float c = dot(pos, pos) - (radius * radius);
	float d = (b * b) - 4.0 * c;
	if (choose == 0)
		return vec2((-b - sqrt(d)) * 0.5, 0.);
	if (choose == 1)
		return vec2(0., (-b + sqrt(d)) * 0.5);
	if (choose == 2)
		return vec2((-b - sqrt(d)) * 0.5, (-b + sqrt(d)) * 0.5);
}

vec4 light_step(vec3 pos, vec3 light_dir, int steps, vec2 scale_height, float radius, float b, float d)
{

	float ray_length_l = 0.;
	float step_size_l = 0.;

	ray_length_l = (-b + sqrt(d)) * 0.5 / float(steps);
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
	opt_l.z = height_l;
	opt_l.w = step_size_l;

	// MARCH THE CLOUD using POS_L
	return opt_l;
}

vec3 ray_mie(float od1, float od2, float dens)
{
	return exp(-(beta_ray * od1 + 1.1 * beta_mie * od2)) * dens;
}

// vec4 cloud_color(vec3 start, vec3 dir, vec2 opt_c, float cloud_ray_length, vec2 density, vec3 ray_c, vec3 mie_c)
//{
//	float cloud_den = min(10. * map(start + dir * cloud_ray_length, 5), 1.);
//	if (cloud_den > 0.5)
//	{
//		vec3 cloud_shade = exp(-(1.1 * beta_mie * opt_c.y + beta_ray * opt_c.x));
//		vec3 colo = cloud_shade * (ray_c + mie_c * cloud_den);
//
//		return vec4(colo, cloud_den);
//	}
//	else
//		return vec4(0.);
// }

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
	float c = startMagSq - atmo_rad * atmo_rad;
	float bb = b * b;
	float d = bb - 4.0 * c;
	//
	//	// stop early if there is no intersect
	if (d < 0.0)
		return vec4(0.); // vec4(0.0);

	//	 calculate the ray length
	vec2 ray_length = vec2(
			max((-b - sqrt(d)) * 0.5, 0.0),				// + atmo_rad			  * 0.00001,
			min((-b + sqrt(d)) * 0.5, max_dist)); // - atmo_rad  * 0.00001);

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
	vec3 colo;
	vec4 sky_color = vec4(0.);
	vec4 opacity;
	vec4 col_one;
	vec4 col_two;
	vec4 col_thr;
	vec4 cloud_o;
	vec4 cloud_t;
	vec4 cloud_sum = vec4(0.);

	vec4 cloud_color;

	// initialize the optical depth. This is used to calculate how much air was in the ray

	mat4 opt_i = mat4(0.);

	// also init the scale height, avoids some vec2's later on
	vec2 scale_height = vec2(height_ray, height_mie);
	int cloud_stage = 0;

	// loat cloud_rad = plan_rad + height_c;
	vec3 pos_c1;
	vec3 pos_c2;
	float cCloud;
	float dCloud;
	cCloud = startMagSq - cloud_rad * cloud_rad;
	dCloud = bb - 4.0 * cCloud;

	// vec3 outer_pos_c1;
	// vec3 outer_pos_c2;
	// float outer_cCloud;
	// float outer_dCloud;
	// float outer_cloud_rad = cloud_rad - thickness;
	// outer_cCloud = startMagSq - outer_cloud_rad * outer_cloud_rad;
	// outer_dCloud = bb - 4.0 * cCloud;
	vec2 cloud_ray_length;
	if (dCloud > 0.) // clouds possible
	{
		cloud_ray_length = vec2(
				max((-b - sqrt(dCloud)) * 0.5, 0.0),
				min((-b + sqrt(dCloud)) * 0.5, max_dist));
		if (cloud_ray_length.x < cloud_ray_length.y) // clouds intercept definite (barring planet in the way)
		{
			if (cloud_ray_length.x > 0.) // above clouds
			{
				cloud_stage = 1;
				if (!planet_facing)
				{
					float outer_rad = cloud_rad + thickness;
					cCloud = startMagSq - outer_rad * outer_rad;
					dCloud = bb - 4.0 * cCloud;
					//	outer_ray_length = vec2(
					//			max((-b - sqrt(dCloud)) * 0.5, 0.0),
					//			min((-b + sqrt(dCloud)) * 0.5, max_dist));

					pos_c1 = start + dir * cloud_ray_length.x;
					pos_c2 = start + dir * cloud_ray_length.y;
					// outer_pos_c1 = pos_c1 + normalize(pos_c1) * thickness;
					// outer_pos_c2 = pos_c2 + normalize(pos_c2) * thickness;
					cloud_color = intersectCloud(pos_c1, cloud_ray_length.x, start, dir, light_dir);
				}
				else
				{
					pos_c1 = start + dir * cloud_ray_length.x;
					pos_c2 = start + dir * cloud_ray_length.y;

					cloud_color = intersectCloud(pos_c1, cloud_ray_length.x, start, dir, light_dir);
					//		cloud_color += (1. - cloud_color.a) * intersectCloud(cloud_ray_length.y, start, dir, light_dir);

					//	float cloud_alpha = 1. - exp(-8. * DENS * transmittance.y);
					// fragColor = vec4(alpha); return; // for tests
					//		vec3 cloud_frag = vec3(transmittance.x);
					// ALBEDO = (cloud_frag + (1. - cloud_alpha) * skyColor.xyz);
					// ALPHA = (alpha + (1. - cloud_alpha) * skyColor[3]);
					//	+= (1. - sky_color.a) * phase_mie;
				}
			}
			else // below clouds
			{
				if (!planet_facing)
				{
					pos_c1 = start + dir * cloud_ray_length.x;
					pos_c2 = start + dir * cloud_ray_length.y;
					cloud_color = intersectCloud(pos_c2, cloud_ray_length.y, start, dir, light_dir);
					cloud_stage = 1;
				}
			} // planet not in the way and below clouds
			cloud_color = min(cloud_color, 1.);
		}
	}
	// Calculate the Rayleigh and Mie phases.
	// This is the color that will be scattered for this ray
	// mu, mumu and gg are used quite a lot in the calculation, so to speed it up, precalculate them
	float mu = dot(dir, light_dir);
	float mumu = mu * mu;
	float gg = g * g;
	// float phase_ray = 3.0 / (50.2654824574 /* (16 * pi) */) * (1.0 + mumu);
	// float phase_mie = !planet_facing ? 3.0 / (25.1327412287 /* (8 * pi) */) * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg)) : 0.0;
	float phase_ray = 3.0 / 50.2654824574 * (1.0 + mumu);
	float phase_mie = !planet_facing ? 3.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg)) : 0.0;
	// float cloud_phase_mie_back = 3.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg + 2.0 * mu * g, 1.5) * (2.0 + gg));
	// cloud_phase_mie_back = 0.;
	float cloud_phase_mie_one = 1.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg));
	float cloud_phase_mie_two = 1.0 / 25.1327412287 * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * g, 1.5) * (2.0 + gg));

	vec3 cloud_mie_constant_one = cloud_phase_mie_one * beta_mie;
	vec3 cloud_mie_constant_two = cloud_phase_mie_two * beta_mie;

	vec3 ray_constant = phase_ray * beta_ray;
	vec3 mie_constant = phase_mie * beta_mie;

	bool into_sun = mu > 0.;

	// tangent_dist = (plan_rad+height_c)*tan(acos((plan_rad+height_c)/atmo_rad));

	// steps_i = 6;
	vec3 opt_i_tot = vec3(0.);

	// vec2 cloud_den = vec2(0.);
	vec3 cloud_shade_ray_one = vec3(0.);
	vec3 cloud_shade_ray_two = vec3(0.);
	vec3 cloud_shade_mie_one = vec3(0.);
	vec3 cloud_shade_mie_two = vec3(0.);

	vec3 final_light;

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
	vec3 l_attn = vec3(0.);
	final_light = vec3(0.);
	float tangent_dist = plan_rad * tan(acos(plan_rad / atmo_rad)); // distance from tangent of planet to atmo

	float full_path = ray_length.y - ray_length.x;
	float steps_raw; // = max(16. * full_path / tangent_dist, 16.0) - 4. * ray_length.x / ray_length.y;
	if (planet_facing)
		steps_raw = 16.0 - 4. * ray_length.x / ray_length.y;
	else
		steps_raw = (16. * full_path / tangent_dist) - 8. * ray_length.x / ray_length.y;

	int steps_i = int(round(steps_raw));
	float step_size_i = full_path / float(steps_i);
	float half_step_i = 0.5 * step_size_i;
	// ray_length.y -= half_step_i;
	vec3 pos_i;
	float height_i;
	if (cloud_stage < 0)
	{

		b = 2.0 * dot(light_dir, pos_c1);
		d = b * b;
		vec4 opt_c_one = light_step(pos_c1, light_dir, 4, scale_height, cloud_rad, b, d);
		b = 2.0 * dot(light_dir, pos_c2);
		d = b * b;
		vec4 opt_c_two = light_step(pos_c2, light_dir, 4, scale_height, cloud_rad, b, d);
		// opt_c_two = opt_c_one;
		vec3 cloud_raw = vec3(0.);
		ivec3 cloud_steps;

		vec3 cloud_step_size;
		vec3 cloud_half_step;
		vec3 cloud_path;
		vec3 cloud_steps_raw;

		// float step_size_i = (ray_length.y - ray_length.x) / float(steps_i);
		if (cloud_ray_length.x > 0.) // above clouds
		{
			cloud_path.x = cloud_ray_length.x - ray_length.x;
			cloud_path.y = ray_length.y - cloud_ray_length.x;
			cloud_steps_raw.x = max(steps_raw * cloud_path.x / full_path, 0.);
			cloud_steps_raw.y = max(steps_raw * cloud_path.y / full_path, 0.);
		}
		else
		{
			cloud_path.x = cloud_ray_length.y;
			cloud_path.y = ray_length.y - cloud_ray_length.y;
			cloud_steps_raw.x = max(steps_raw * cloud_path.x / full_path, step_size_i);
			cloud_steps_raw.y = max(steps_raw * cloud_path.y / full_path, step_size_i);
		}

		cloud_steps.x = int(round(cloud_steps_raw.x));
		cloud_steps.y = steps_i - cloud_steps.x;

		cloud_step_size = cloud_path / vec3(cloud_steps);
		cloud_step_size = vec3(step_size_i);

		cloud_half_step = cloud_step_size * 0.5;

		for (int i = 0; i < steps_i; ++i)
		{

			if (cloud_stage == 1)
			{
				pos_i = start + dir * (ray_pos_i + cloud_half_step.x);
				height_i = length(pos_i) - plan_rad;
				vec2 density = exp(-height_i / scale_height) * cloud_step_size.x;
				b = 2.0 * dot(dir, pos_i);
				c = dot(pos_i, pos_i) - atmo_rad * atmo_rad;
				d = b * b - 4.0 * c;
				opt_l = light_step(pos_i, light_dir, 4, scale_height, atmo_rad, b, d);
				opt_i[1].xy += density;
				opt_i_tot.xy += density;
				attn = exp(-(1.1 * beta_mie * (opt_i_tot.y + opt_l.y) + beta_ray * (opt_i_tot.x + opt_l.x)));
				cloud_ray[1].xyz += attn * density.x;
				cloud_mie[1].xyz += attn * density.y;
				if (i == cloud_steps.x - 1)
				{
					cloud_stage = 2;
					opacity[1] = 1. - length(exp(-(1.1 * beta_mie * opt_i[1].y + beta_ray * opt_i[1].x) * density_multiplier));
					//  next, add cloud color if it's dense enough
					// cloud_color += (1. - sky_color.a) * phase_mie * cloud_color(start, dir, opt_c_one, cloud_ray_length.x, density, ray_constant, cloud_mie_constant_one);
					//	cloud_color += (1. - sky_color.a) * phase_mie;
				}
				ray_pos_i += cloud_step_size.x;
			}
			else // if (cloud_stage == 2)
			{
				pos_i = start + dir * (ray_pos_i + cloud_half_step.y);
				height_i = length(pos_i) - plan_rad;
				vec2 density = exp(-height_i / scale_height) * cloud_step_size.y;
				b = 2.0 * dot(dir, pos_i);
				c = dot(pos_i, pos_i) - atmo_rad * atmo_rad;
				d = b * b - 4.0 * c;
				opt_l = light_step(pos_i, light_dir, 4, scale_height, atmo_rad, b, d);
				opt_i[2].xy += density;
				opt_i_tot.xy += density;
				attn = exp(-(1.1 * beta_mie * (opt_i_tot.y + opt_l.y) + beta_ray * (opt_i_tot.x + opt_l.x)));
				cloud_ray[2].xyz += attn * density.x;
				cloud_mie[2].xyz += attn * density.y;
				if (i == cloud_steps.y + cloud_steps.x - 1)
				{
					opacity[2] = 1. - length(exp(-(1.1 * beta_mie * opt_i[2].y + beta_ray * opt_i[2].x) * density_multiplier));

					//		if (!planet_facing)
					//	cloud_color += (1. - sky_color.a) * cloud_color(start, dir, opt_c_two, cloud_ray_length.y, density, ray_constant, cloud_mie_constant_two);
				}
				ray_pos_i += cloud_step_size.y;
			}
		}

		opacity[0] = 1. - length(exp(-(beta_ray * opt_i_tot.x + 1.1 * beta_mie * opt_i_tot.y) * density_multiplier));
		cloud_ray[0].xyz = cloud_ray[1].xyz + cloud_ray[2].xyz + cloud_ray[3].xyz;
		cloud_mie[0].xyz = cloud_mie[1].xyz + cloud_mie[2].xyz + cloud_mie[3].xyz;

		sky_color += vec4(ray_constant * cloud_ray[0].xyz + mie_constant * cloud_mie[0].xyz, opacity[0]);

		// sky_color.xyz *= intensity;
		// if (cloud_steps.y + cloud_steps.x == steps_i)
		//	return sky_color;
		//	sky_color.xyz *= intensity;
		//
		//	return vec4(cloud_color.xyz, cloud_color.a) + (1. - cloud_color.a) * sky_color.a;
	}
	else
	{
		for (int i = 0; i < steps_i; ++i)
		{
			pos_i = start + dir * (ray_pos_i + half_step_i);
			height_i = length(pos_i) - plan_rad;
			vec2 density = exp(-height_i / scale_height) * step_size_i;
			b = 2.0 * dot(dir, pos_i);
			c = dot(pos_i, pos_i) - atmo_rad * atmo_rad;
			d = b * b - 4.0 * c;
			opt_l = light_step(pos_i, light_dir, 4, scale_height, atmo_rad, b, d);

			// float dt = max(0.05, 0.04 * t);

			if (height_i < height_c) //&& cloud_sum.a < 0.99)
			{
				// float cloud_den = smoothstep(0., height_c, height_i) * sin(pi * height_i / height_c) * map(pos_i, 5);
				// float light_den = smoothstep(0., height_c, opt_l.z) * sin(pi * opt_l.z / height_c) * map(pos_i + 0.01 * opt_l.w * light_dir, 5);
				float cloud_den = min(4. * map(pos_i, 5), 1.);
				float light_den = min(4. * map(pos_i + opt_l.w * light_dir, 5), 1.);
				light_den = clamp((cloud_den - light_den) / (opt_l.w), 0., 1.);
				//	float dif = clamp((den - noise(pos + 0.1 * sun_dir)) / 0.1, 0.0, 1.0);

				vec4 col = cloud_march(cloud_den, pos_i, light_dir, vec3(0.0), step_size_i, cloud_ray_length.y - ray_pos_i, light_den);
				cloud_sum += col * (1.0 - cloud_sum.a);
			}

			opt_i[0].xy += density;
			opt_i_tot.xy += density;
			attn = exp(-(1.1 * beta_mie * (opt_i_tot.y + opt_l.y) + beta_ray * (opt_i_tot.x + opt_l.x)));

			cloud_ray[0].xyz += attn * density.x;
			cloud_mie[0].xyz += attn * density.y;

			ray_pos_i += step_size_i;
		}
		opacity[0] = 1. - length(exp(-(beta_ray * opt_i_tot.x + 1.1 * beta_mie * opt_i_tot.y) * density_multiplier));
		sky_color = vec4(ray_constant * cloud_ray[0].xyz + mie_constant * cloud_mie[0].xyz, opacity[0]);
		//	sky_color.xyz *= intensity;
		//	return sky_color;
	}
	// ALBEDO = (cloud_frag + (1. - cloud_alpha) * skyColor.xyz);
	// ALPHA = (cloud_alpha + (1. - cloud_alpha) * skyColor[3]);
	sky_color.xyz *= intensity;
	// return sky_color;
	// if (cloud_color.x != 0.)
	//	return vec4(sky_color.xyz * intensity, cloud_color.a) + (1. - cloud_color.a) * sky_color.a;
	//// return vec4(sky_color.xyz * intensity, cloud_color.a) + (1. - cloud_color.a) * sky_color.a;
	// else
	return cloud_sum + sky_color;
}

vec3 rayDirection(float fieldOfView, vec2 size, vec2 fragCoord)
{
	vec2 xy = fragCoord - size * 0.5;
	float z = 0.5 * size.y / tan(radians(fieldOfView));

	return vec3(xy, -z);
}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
	UV = UV + 1.;
}

void fragment()
{
	Rz = 0.;
	float y = sin(UV.y * pi - pi / 2.);
	float x = cos(UV.x * 2. * pi) * cos(UV.y * pi - pi / 2.);
	float z = sin(UV.x * 2. * pi) * cos(UV.y * pi - pi / 2.);

	y = y * 0.5 + 0.5;
	x = x * 0.5 + 0.5;
	z = z * 0.5 + 0.5;
	vec4 cloud_tex = texture(noise_vol, vec3(x, y, z));

	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	cam_position = CAMERA_MATRIX[3].xyz;
	vec4 world = CAMERA_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	light_direction = vec3(0., 0., 1.);
	vec3 dir = rayDirection(35.0, VIEWPORT_SIZE, FRAGCOORD.xy);

	dir = normalize(dir.x * CAMERA_MATRIX[0].xyz + dir.y * CAMERA_MATRIX[1].xyz + dir.z * CAMERA_MATRIX[2].xyz);

	float max_distance = length(view.xyz);

	vec4 atm = calculate_scattering(CAMERA_MATRIX[3].xyz, dir, max_distance, light_direction);

	atm.w = clamp(atm.w, 0.000001, 1.0);

	ALBEDO = atm.xyz / atm.w;
	ALPHA = atm.w;
}