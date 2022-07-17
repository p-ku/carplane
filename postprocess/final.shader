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

const float shutter_angle = 0.5; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
// Shutter angles > 1 are unrealistic; blur length exceeds frame time, or max "shutter speed".
const float max_steps = 16.; // Number of blur samples.
const float threshold = 1.;	 // Minimum pixel movement required to blur. Using one pixel.
uniform vec2 fov;						 // Field of view, horizontal and vertical.
uniform float uv_depth;			 // Made up term, but derived with real math. Used for variable edge blur.
uniform vec2 reso;
const float dither_scale = 0.25;

void fragment()
{
	vec3 col = texture(color_buffer, SCREEN_UV).xyz;
	vec3 vel = texture(neighbor_buffer, SCREEN_UV).xyz;

	// vel.xy *= 5000.;
	vel.xy -= 0.5;
	vel.xyz *= 5.;
	//	vec3 tile = texture(tile_buffer, SCREEN_UV).xyz;

	// Shutter speed effectively reduces pixel velocity (if less than one).
	//	vec2 w_shutter = shutter_angle * vel.xy;

	// This part is to increase blur at the edges of the screen.
	//	vec2 cos_sq = cos((SCREEN_UV - 0.5) * fov - w_shutter); // Last term puts blur "point" at mid-blur

	//	vec2 cos_sq = cos((SCREEN_UV - 0.5) * fov - vel.xy * 0.5); // Last term puts blur "point" at mid-blur
	//	vec2 edge_factor = uv_depth / (cos_sq * cos_sq);

	// Determine number of samples.
	//	float pixel_distance = length(w_shutter * edge_factor); // Screen distance in pixels.
	//	float pixel_distance = length(vel.xy * edge_factor); // Screen distance in pixels.
	float pixel_distance = vel.z; // Screen distance in pixels.
																// Only blur if it's worthwhile, but not too big either.
																// if (pixel_distance > threshold)
	//{
	//  Divide delta_rad by field of view to put it in terms of SCREEN_UV,
	//  e.g. if delta_rad = 1, then the object traveled across the entire screen.
	//  Apply shutter angle and divide by steps as well, before looping.
	float steps = min(max_steps, pixel_distance);
	steps = max_steps;
	//	vec2 delta_rad = w_shutter / (steps * fov);
	vec2 delta_rad = vel.xy / (steps * fov);

	//  Counter for how many blur layers are applied.
	float count = 1.;
	for (float i = 0.; i < steps; i++)
	{
		// Apply offset multiplied by number of steps.
		vec2 offset = (i + 1.) * delta_rad;
		// vec2 offset = (i + 0.5) * delta_rad;

		vec2 newUV = SCREEN_UV - offset;
		// If blur is occurring offscreen, no need to continue looping.
		if (newUV.x < 0. || newUV.y < 0. || newUV.x > 1. || newUV.y > 1.)
			break;
		col += texture(color_buffer, newUV).rgb;
		count++;
	}
	// Average the blur layers into final result.
	col /= count;
	//	}
	// COLOR = vec4(vel.xy * 100., 0., 1.);
	COLOR = vec4(col, 1.);

	// float Vee = float(floor(FRAGCOORD.x / 20.) == floor(FRAGCOORD.y / 20.));

	//	float posMod = float((-dither_scale + 2. * dither_scale * FRAGCOORD.x) * (-1. + 2. * FRAGCOORD.y));

	// vec2 Vee = mod(FRAGCOORD.xy, 20.);
	// vec4 O = vec4(float(Vee.x == .5 || Vee.y == .5));
	// if (length(Vee - 10.) < 5.)
	// 	O.x++;
	//	COLOR = O;
}