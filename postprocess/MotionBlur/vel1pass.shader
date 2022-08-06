// A buffer that stores velocity and depth information for each pixel.
// Camera velocity only at the moment.

shader_type spatial;
render_mode depth_test_disable, depth_draw_never, unshaded;

uniform sampler2D no_blur_mask;
uniform vec3 cam_prev_pos;	 // Previous position of the camera.
uniform mat3 cam_prev_xform; // Used to transform between current and previous camera rotation
uniform bool snap = false;
uniform float res_depth_vec;
uniform vec2 half_reso;
uniform float tile_pixel_size;
uniform float shutter_angle = 0.5;
// uniform bool all_blur;

const float eps = 0.00001;

void vertex()
{
	// VERTEX *= 2.;
	POSITION = vec4(VERTEX, 1.0);
	// UV.y = 1. - UV.y + 0.5;
	// UV.x = UV.x + 0.5;
	//	UV.y = 2. * (1. - UV.y);
	//	UV.x = UV.x * 2.;
	//	UV.y = (1. - UV.y);
}

void fragment()
{
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;

	vec2 half_blur = vec2(0.5);
	bool blur_it = texture(no_blur_mask, SCREEN_UV).a == 0.; // || all_blur;

	if (!snap && blur_it)
	{
		// Get pixel position relative to the camera.
		vec3 pixel_pos_ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
		vec4 pixel_pos = INV_PROJECTION_MATRIX * vec4(pixel_pos_ndc, 1.0);
		pixel_pos.xyz /= pixel_pos.w;
		vec3 prev_pixel_pos = cam_prev_xform * pixel_pos.xyz;

		if (depth < 1.)
			prev_pixel_pos += (pixel_pos.xyz - cam_prev_pos);

		vec2 frag_prev = half_reso - prev_pixel_pos.xy * res_depth_vec / prev_pixel_pos.z;
		vec2 frag_vel = (FRAGCOORD.xy - frag_prev);
		float vel_mag = length(frag_vel);
		float clamped_vel = max(min(vel_mag * shutter_angle, tile_pixel_size), 0.5);
		half_blur = 0.5 + 0.5 * frag_vel * clamped_vel / (tile_pixel_size * (vel_mag + eps));
	}
	ALBEDO = vec3(half_blur, depth);
	//	ALBEDO = vec3(half_blur, 0.);
	// ALBEDO = vec3(UV, 0.);
}