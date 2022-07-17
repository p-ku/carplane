// A buffer that stores velocity and depth information for each pixel.
// Camera velocity only at the moment.

shader_type spatial;
render_mode depth_test_disable, depth_draw_never, unshaded;

uniform vec3 cam_prev_pos;		// Previous position of the camera.
uniform mat3 cam_xform;				// Used to transform between current and previous camera rotation
uniform float max_blur_angle; // Horizontal field of view in radians, halved in this case.
uniform bool snap = false;

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
	vec3 prev_pixel_pos = cam_xform * pixel_pos.xyz;

	if (depth < 1.)

		//		//   Add translational motion if pixel isn't part of the background,
		//		//   i.e. assume infinite distance to the background.
		prev_pixel_pos += (pixel_pos.xyz - cam_prev_pos);

	// Ignore if rotation puts pixel behind camera.
	//	if (prev_pixel_rot.z < 0.)
	//	if (cam_x_angle < 1.57 && cam_y_angle < 1.57 && cam_z_angle < 1.57)
	vec2 vel = vec2(0.5);
	// float real_depth = length(pixel_pos.xyz);
	if (!snap)
	{
		// Angle between current pixel and center camera view.
		vec2 th = atan(pixel_pos.xy / pixel_pos.z);
		// Angle between current pixel as seen in previous frame.
		vec2 th_prev = atan(prev_pixel_pos.xy / prev_pixel_pos.z);

		// Angle between object's initial position and final position, relative to the camera.
		vec2 delta_rad = th - th_prev;
		// Cap angles at which velocity is considered. Prevents glitchy stuff.
		//	if (abs(th.x - th_rot.x) < max_blur_angle)
		//	ALBEDO = vec3(delta_rad + 0.5, depth);

		// ALBEDO = vec3(delta_rad + 0.5, depth);
		vel = delta_rad + 0.5;
		//	else
		//		ALBEDO = vec3(0.5, 0.5, depth);
		// Add 0.5 for positive values, storable as colors. My thinking is these will always be small angles,
		// So no worrying about values becoming greater than 1. Absolute value is the obvious path,
		// but then you need away to store sign (+/-) e.g. in the blue channel (I need that for depth).
		// By adding 0.5, I need only subtract 0.5 on the other side.
	}
	// else
	//	vel
	//  ALBEDO = vec3(0.5, 0.5, depth);
	//  ALBEDO = vec3(0.5, 0.5, depth);

	//	ALBEDO = vec3(0., 0., depth);
	ALBEDO = vec3(vel, depth);
}