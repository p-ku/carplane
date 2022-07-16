shader_type canvas_item;
render_mode blend_premul_alpha, unshaded;

uniform vec2 resolution;

// DO NOT use TEXTURE_PIXEL_SIZE
// because of bug https://github.com/godotengine/godot/issues/45428
// ALWAYS send resolution in your own uniform
uniform sampler2D buffer;
uniform vec3 linear;
uniform vec3 angular;

const float shutter_angle = 0.5; // 0.5 is like 180deg, i.e. shutter is open for half the frame time
const float max_steps = 4.;
uniform vec2 fov; // field of view, fov[0] is horizontal, fov[1] is vertical
// uniform vec2 rads_per_pixel; // field of view divided by screen resolution (x and y)
uniform vec2 threshold;
const float EPS = 1e-6;

void fragment()
{
  // transformations to get to camera space
  // float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r;
  // vec3 pixel_pos_ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
  // vec4 pixel_pos_cam = INV_PROJECTION_MATRIX * vec4(pixel_pos_ndc, 1.0);
  // pixel_pos_cam.xyz /= pixel_pos_cam.w;
  //
  // // pixel_pos_cam.xyz = min(pixel_pos_cam.xyz, 1000.);
  //
  // // linear displacement of current pixel relative to the camera during one frame
  // vec2 delta_xy = pixel_pos_cam.xy - linear.xy;
  // // angle between current pixel and center camera view
  // vec2 th = atan(pixel_pos_cam.xy / -pixel_pos_cam.z);
  // // angles (x and y) between final position and central camera view due to motion in the xy-plane
  // vec2 tht = atan(delta_xy / -pixel_pos_cam.z);
  // // angles (x and y) between final position and central camera view due to motion on the z-axis
  // vec2 thtz = atan(pixel_pos_cam.xy / (linear.z - pixel_pos_cam.z));
  // // angle between object's initial position and final position, relative to the camera
  // vec2 delta_rad = (2. * th - tht - thtz);
  //
  // // begin with unblurred image
  // vec3 col = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0).rgb;
  //
  // vec2 steps_vec = abs(delta_rad / threshold);
  // float steps = floor(min(max(steps_vec.x, steps_vec.y), max_steps));
  // if (steps > 0.)
  // {
  //   // divide delta_rad by field of view to put it in terms of SCREEN_UV
  //   // e.g. if delta_rad = 1, then the object traveled across the entire screen
  //   // apply shutter angle and divide by steps as well, before looping
  //   delta_rad = delta_rad * shutter_angle / (steps * fov);
  //   // counter for how many blur layers are applied
  //   float count = 1.;
  //   for (float i = 0.; i < steps; i++)
  //   {
  //     // apply offset multiplied by number of steps
  //     vec2 offset = (i + 1.) * delta_rad;
  //     vec2 newUV = SCREEN_UV - offset;
  //     // if blur is occurring offscreen, no need to continue looping
  //     if (newUV.x < 0. || newUV.y < 0. || newUV.x > 1. || newUV.y > 1.)
  //       break;
  //     col += textureLod(SCREEN_TEXTURE, newUV, 0.0).rgb;
  //     count++;
  //   }
  //   // average the blur layers into final result
  //   ALBEDO = col / count;
  // }
  // else
  // {
  //   // no blur when there's little to no motion
  //   ALBEDO = col;
  // }
  // COLOR.xyz = vec3(0.8, 0.5, 0.9);
  COLOR = vec4(texture(buffer, FRAGCOORD.xy / vec2(1920, 1080)).rgb, 1.);
  // vec4 mask = vec4(texture(buffer, UV).rgb, 1.);
}