// TRANSLATED & MODIFIED FROM: https://www.shadertoy.com/view/4sX3Rs
// https://godotshaders.com/shader/lens-flare-shader/
shader_type canvas_item;
render_mode unshaded;

uniform float sun_size;
uniform vec2 reso = vec2(1280, 720);
uniform vec2 sun_frag = vec2(1.0);
uniform vec3 tint = vec3(1.4, 1.2, 1.0);
uniform sampler2D noise_texture;
uniform sampler2D velocity_buffer;

float noise_float(float t, vec2 texResolution)
{
  return texture(noise_texture, vec2(t, 0.0) / texResolution).x;
}
float noise_vec2(vec2 t, vec2 texResolution)
{
  return texture(noise_texture, t / texResolution).x;
}

vec3 lensflare(vec2 uv, vec2 frag, vec2 sun_uv)
{
  vec2 uv_o = uv + 0.5;
  vec2 sun_uv_o = sun_uv + 0.5;

  uv.x = uv.x * reso.x / reso.y;
  sun_uv.x = sun_uv.x * reso.x / reso.y;

  vec2 uv_diff = uv - sun_uv;

  vec2 frag_diff = frag - sun_frag;
  vec2 center_frag = reso * 0.5;

  vec2 uvd = uv * (length(uv));

  // float n = noise_vec2(vec2(ang * 16.0, dist_pow * 32.0), texResolution);

  float f1 = max(0.01 - pow(length(uv + 1.2 * sun_uv), 1.9), .0) * 7.0;

  float f2 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.8 * sun_uv), 2.0)), .0) * 00.25;
  float f22 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.85 * sun_uv), 2.0)), .0) * 00.23;
  float f23 = max(1.0 / (1.0 + 32.0 * pow(length(uvd + 0.9 * sun_uv), 2.0)), .0) * 00.21;

  vec2 uvx = mix(uv, uvd, -0.5);

  float f4 = max(0.01 - pow(length(uvx + 0.4 * sun_uv), 2.4), .0) * 6.0;
  float f42 = max(0.01 - pow(length(uvx + 0.45 * sun_uv), 2.4), .0) * 5.0;
  float f43 = max(0.01 - pow(length(uvx + 0.5 * sun_uv), 2.4), .0) * 3.0;

  //  uvx = mix(uv, uvd, -0.5);

  float f6 = max(0.01 - pow(length(uvx - 0.3 * sun_uv), 1.6), .0) * 6.0;
  float f62 = max(0.01 - pow(length(uvx - 0.325 * sun_uv), 1.6), .0) * 3.0;
  float f63 = max(0.01 - pow(length(uvx - 0.35 * sun_uv), 1.6), .0) * 5.0;

  uvx = mix(uv, uvd, -.4);

  float f5 = max(0.01 - pow(length(uvx + 0.2 * sun_uv), 5.5), .0) * 2.0;
  float f52 = max(0.01 - pow(length(uvx + 0.4 * sun_uv), 5.5), .0) * 2.0;
  float f53 = max(0.01 - pow(length(uvx + 0.6 * sun_uv), 5.5), .0) * 2.0;

  vec3 c = vec3(.0);

  c.r += f2 + f4 + f5 + f6;
  c.g += f22 + f42 + f52 + f62;
  c.b += f23 + f43 + f53 + f63;
  c = c * 1.3 - vec3(length(uvd) * .05);

  // Do not need an artificial sun
  if (texture(velocity_buffer, uv_o).z == 1.)
  {
    float ang = atan(frag_diff.x, frag_diff.y);

    float dist = length(uv_diff);
    //  vec2 dist_vec = dist * reso;
    // float dist = length(dist_vec);
    // // float dist = length(dist_vec);
    // // dist = length(uv_diff);
    // //  vec2 dist_pow;
    // dist = pow(dist, 0.1);
    // float f0 = length(10. * sun_size / (dist * 16.0 + 1.0));
    float f0 = sun_size / (dist * 16.0 + sun_size);
    //  f0 = f0 + f0 * (sin(noise_float(sin(ang * 2. + sun_uv.x) * 4.0 - cos(ang * 3. + sun_uv.y), reso) * 16.) * .1 + dist_pow * .1 + .8);
    c += vec3(f0);
  }
  return c;
}

vec3 cc(vec3 color, float factor, float factor2) // color modifier
{
  float w = color.x + color.y + color.z;
  return mix(color, vec3(w) * factor, w * factor2);
}
void vertex()
{
  UV.y = 1. - UV.y;
}
void fragment()
{
  //  vec2 texResolution = 1.0 / TEXTURE_PIXEL_SIZE;
  //  vec2 resolution = 1.0 / SCREEN_PIXEL_SIZE;

  // vec2 uv = FRAGCOORD.xy / resolution.xy - 0.5;
  // uv.x *= resolution.x / resolution.y; // fix aspect ratio
  vec2 sun_uv = sun_frag / reso;
  sun_uv.y = 1. - sun_uv.y;
  // mouse.x *= resolution.x / resolution.y; // fix aspect ratio
  // sun_uv.x *= reso.x / reso.y;
  vec4 previousColor = texture(SCREEN_TEXTURE, UV);

  vec3 color = previousColor.rgb;
  vec2 uuv = UV - 0.5;
  // color += tint * lensflare(UV, sun_uv, texResolution);
  color += lensflare(UV - 0.5, UV * reso, sun_uv - 0.5);

  // color -= noise_vec2(FRAGCOORD.xy, texResolution) * .015;
  color = cc(color, .5, .1);
  COLOR = vec4(color, 1.0);
}