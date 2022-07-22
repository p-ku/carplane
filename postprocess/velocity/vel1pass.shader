// A buffer that stores velocity and depth information for each pixel.
// Camera velocity only at the moment.

shader_type spatial;
render_mode depth_test_disable, depth_draw_never, unshaded;

uniform vec3 cam_prev_pos;	 // Previous position of the camera.
uniform mat3 cam_prev_xform; // Used to transform between current and previous camera rotation
uniform bool snap = false;
uniform vec2 res_depth_vec;
uniform vec2 uv_depth_vec;
uniform float tile_uv;
uniform vec2 reso;
const float tile_size = 40.;
const float eps = 10e-5;
const float shutter_angle = 0.5;

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
	UV = UV + 1.;
}

void fragment()
{
	float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;

	vec2 pix_half_blur = vec2(0.5);
	vec2 uv_half_blur = vec2(0.5);

	if (!snap)
	{
		// Get pixel position relative to the camera.
		vec3 pixel_pos_ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
		vec4 pixel_pos = INV_PROJECTION_MATRIX * vec4(pixel_pos_ndc, 1.0);
		pixel_pos.xyz /= pixel_pos.w;

		vec3 prev_pixel_pos = cam_prev_xform * pixel_pos.xyz;

		if (depth < 1.)
			prev_pixel_pos += (pixel_pos.xyz - cam_prev_pos);

		vec2 frag_prev = reso * 0.5 - prev_pixel_pos.xy * res_depth_vec / prev_pixel_pos.z;
		vec2 frag_vel = (FRAGCOORD.xy - frag_prev);
		float vel_mag = length(frag_vel);
		pix_half_blur = 0.5 + 0.5 * frag_vel * clamp(vel_mag * shutter_angle, 0.5, tile_size) / (tile_size * (vel_mag + eps));

		//	vec2 uv_prev = 0.5 - prev_pixel_pos.xy * uv_depth_vec / prev_pixel_pos.z;
		//	vec2 uv_vel = (SCREEN_UV - uv_prev);
		//	float vel_mag = length(uv_vel);
		//	uv_half_blur = 0.5 + 0.5 * uv_vel * clamp(vel_mag * shutter_angle, 0.5, tile_uv) / (tile_uv * (vel_mag + eps));
	}

	ALBEDO = vec3(pix_half_blur, depth);
}