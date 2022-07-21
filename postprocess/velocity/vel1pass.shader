// A buffer that stores velocity and depth information for each pixel.
// Camera velocity only at the moment.

shader_type spatial;
render_mode depth_test_disable, depth_draw_never, unshaded;

uniform vec3 cam_prev_pos;		// Previous position of the camera.
uniform mat3 cam_prev_xform;	// Used to transform between current and previous camera rotation
uniform float max_blur_angle; // Horizontal field of view in radians, halved in this case.
uniform bool snap = false;
uniform vec2 fov;
uniform vec2 uv_depth_vec;
uniform vec2 reso;
const float lil_r = 40.;
const float epsilon = 10e-5;
const float framerate_target = 60.;
const float shutter_angle = 0.5;
const float exposure_t = shutter_angle / framerate_target;

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
	UV = UV + 1.;
}

void fragment()
{

	// Get pixel position relative to the camera.
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;
	vec3 pixel_pos_ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	vec4 pixel_pos = INV_PROJECTION_MATRIX * vec4(pixel_pos_ndc, 1.0);
	pixel_pos.xyz /= pixel_pos.w;

	// Previous camera rotation.
	// vec3 prev_pixel_pos = pixel_pos.xyz;
	//	vec3 prev_pixel_pos = cam_prev_xform * pixel_pos.xyz;
	vec3 prev_pixel_pos = pixel_pos.xyz;
	prev_pixel_pos = cam_prev_xform * pixel_pos.xyz;

	if (depth < 1.)
	//		//   Add translational motion if pixel isn't part of the background,
	//		//   i.e. assume infinite distance to the background.
	{
		//	prev_pixel_pos = cam_prev_xform * pixel_pos.xyz;
		prev_pixel_pos += (pixel_pos.xyz - cam_prev_pos);
	}
	//	prev_pixel_pos = (pixel_pos.xyz - cam_prev_pos);

	// Ignore if rotation puts pixel behind camera.
	//	if (prev_pixel_rot.z < 0.)
	//	if (cam_x_angle < 1.57 && cam_y_angle < 1.57 && cam_z_angle < 1.57)
	//	vec2 vel = vec2(0.5);
	// float real_depth = length(pixel_pos.xyz);
	//	vec2 delta_rad = vec2(0.5, 0.5);
	// vec2 uv_prev = vec2(0.0);
	// vec2 uv_vel = vec2(0.0);
	vec2 pix_half_blur = vec2(0.);
	if (!snap)
	{
		// vec2 uv_prev = 0.5 - prev_pixel_pos.xy / (prev_pixel_pos.z * uv_depth_vec);
		vec2 frag_prev = reso * 0.5 - prev_pixel_pos.xy * uv_depth_vec / prev_pixel_pos.z;
		vec2 frag_vel = (FRAGCOORD.xy - frag_prev) * framerate_target;
		//	vec2 uv_vel = SCREEN_UV - uv_prev;
		//		vec2 uv2pix = (pixel_vel.xy - 0.5) * reso;
		//	float vel_mag = length(uv_prev);
		// vec2 frag_vel = uv_vel * reso * framerate_target;
		float vel_mag = length(frag_vel);
		pix_half_blur = 0.5 * frag_vel * clamp(vel_mag * exposure_t, 0.5, lil_r) / (lil_r * (vel_mag + epsilon));
		// uv_vel = 0.5 + 0.5 * uv_vel * clamp(vel_mag * exposure_time, 0.5, lil_r) / (lil_r * (vel_mag + epsilon));
		//	float final_mag = length(uv2pix);
		//	uv_vel = pix_half_blur / reso;
	}
	// else
	//	vel
	//  ALBEDO = vec3(0.5, 0.5, depth);
	//  ALBEDO = vec3(0.5, 0.5, depth);

	//	ALBEDO = vec3(0., 0., depth);

	// SCREEN_UV - uv_prev gives the UV velocity of the pixel.
	//	ALBEDO = vec3(SCREEN_UV - uv_prev + 0.5, depth);
	ALBEDO = vec3(pix_half_blur + 0.5, depth);

	//	ALBEDO = vec3(uv_prev, depth);

	// ALBEDO = vec3(uv_prev - SCREEN_UV, depth);

	//	ALBEDO = vec3(delta_rad, depth);

	//		ALBEDO = vec3(vel, depth);
}