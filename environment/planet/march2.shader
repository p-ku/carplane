shader_type spatial;
render_mode unshaded;
// const int NOISE_METHOD = 1;
// const bool USE_LOD = true;

uniform sampler3D noise_vol;
uniform sampler2D noise_tex;
uniform sampler2D noise_tex2;
// uniform int maxMarch = 1 - 1;
//  uniform float minD = 0.0;
uniform float eps = 0.01;
uniform float cloud_rad = 30.0;
uniform float planet_rad = 26.0;
uniform float pi = 3.14159;

uniform float buffBuff = 13.0;
uniform vec3 sundir = vec3(0.0, 0.0, 0.0);
uniform bool ANIM = false; // true/false

float noise(in vec3 x)
{
  // float mask = texture(noise_mask, UV).r;

  vec3 p = floor(x);
  vec3 f = x - p;
  // f = f*f*(3.0-2.0*f);
  f = smoothstep(0.0, 1.0, f);

  // if (NOISE_METHOD==0)
  //{
  x = p + f;
  // return textureLod(noise_vol,(x+0.5)/256.0,0.0).x*2.0-1.0;
  return textureLod(noise_vol, (0.5 * x / cloud_rad + 0.5), 0.0).x * 2.0 - 1.0;
}
float map(in vec3 p, float t, int oct, vec3 rd, vec3 ro)
{
  // vec3 q = p - vec3(0.0,0.1,1.0)*iTime;
  vec3 q = p;
  float g = 0.5 + 0.5 * noise(q * 0.3);

  float f;
  f = 0.50000 * noise(q);
  q = q * 2.02;
  // #if USE_LOD==1
  if (oct >= 2)
    //  #endif
    f += 0.25000 * noise(q);
  q = q * 2.23;
  //  #if USE_LOD==1
  if (oct >= 3)
    //  #endif
    f += 0.12500 * noise(q);
  q = q * 2.41;
  //  #if USE_LOD==1
  if (oct >= 4)
    //  #endif
    f += 0.06250 * noise(q);
  q = q * 2.62;
  //  #if USE_LOD==1
  if (oct >= 5)
    //  #endif
    f += 0.03125 * noise(q);

  f = mix(f * 0.1 - 0.75, f, g * g) + 0.1;
  // return 1.5*f - 0.5-(length(p)-cloud_rad);
  //   return 1.5*f - 0.5-length(p);
  return f + 0.5;
  // return 1.5*f - 0.5 + (length(ro)-length(rd+ro));
  // return 1.5*f - 0.5 - (dot(p-ro,normalize(ro)));
  // return  1.5*f - 0.5 + dot(p,ro)/(length(p)*length(ro));
}

uniform int kDiv = 2; // make bigger for higher quality

vec4 raymarch(in vec3 ro, in vec3 rd, in vec3 bgcol, in ivec2 px, vec3 sunDir, float depth, float depth_buffer)
{
  // bounding planes
  // float yb = -0.0;
  // float yt =  0.0;
  // float tb = (yb-ro.y)/rd.y;
  // float tt = (yt-ro.y)/rd.y;
  // float yb = cloud_rad-2.;
  float yb = planet_rad;

  float yt = cloud_rad;
  float tb = (yb - length(ro)) / dot(rd, ro);
  float tt = (yt - length(ro)) / dot(rd, ro);
  // find tigthest possible raymarching segment
  float tmin, tmax;
  // if( ro.y>yt )
  //{
  //     // above top plane
  //     if( tt<0.0 ) return vec4(0.0); // early exit
  //     tmin = tt;
  //     tmax = tb;
  // }
  //      if( length(ro)>yt )
  //  {
  //      // above top plane
  //      if( tt<0.0 ) return vec4(0.0); // early exit
  //      tmin = tt;
  //      tmax = tb;
  //  }
  //  else
  //{
  //  inside clouds slabs
  tmin = depth;
  // tmax = cloud_rad+ length(pos);
  // if( tt>0.0 ) tmax = min( tmax, tt );
  // if( tb>0.0 ) tmax = min( tmax, tb );
  //}

  // dithered near distance
  float t;
  if (tmin < 0.1)
  {
    t = tmin + 0.1 * texelFetch(noise_tex2, px, 0).x;
  }
  else
  {
    t = tmin;
  }

  // raymarch loop
  vec4 sum = vec4(0.0);
  for (int i = 0; i < 95 * kDiv; i++)
  {
    // step size
    float dt = max(0.05, 0.04 * t / float(kDiv));
    dt = 0.2;
    // sample cloud
    vec3 pos = ro + t * rd;
    tmax = cloud_rad + length(ro);
    if (length(pos) < yt && length(pos) > yb)
    {
      vec3 p = floor(pos);
      vec3 f = pos - p;
      // f = f*f*(3.0-2.0*f);
      f = smoothstep(0.0, 1.0, f);

      // if (NOISE_METHOD==0)
      //{
      //  vec3 x = p + f;
      // return textureLod(noise_vol,(x+0.5)/256.0,0.0).x*2.0-1.0;
      //  float den = smoothstep(yt,yb,length(pos))*textureLod(noise_vol,(x+127.5)/256.0,0.0).x*2.0-1.0;
      int oct = 5 - int(log2(1.0 + t * 0.5));
      oct = 1;
      //   float den = smoothstep(yt,yb,length(pos))*map(pos,rd,ro,t,oct);
      // float den = map(pos,rd,ro,t,oct);

      // float den = smoothstep(yt,yb,length(pos))*map(pos,t,oct);
      float den = smoothstep(yb, yt, length(pos)) * sin(pi * (length(pos) - yb) / (yt - yb)) * map(pos, t, oct, rd, ro);

      //    float den = noise( pos );
      if (den > 0.01) // if inside
      {
        // do lighting
        float dif = clamp((den - noise(pos + 0.3 * sundir)) / 0.3, 0.0, 1.0);
        vec3 lin = vec3(0.65, 0.65, 0.75) * 1.1 + 0.8 * vec3(1.0, 0.6, 0.3) * dif;
        vec4 col = vec4(mix(vec3(1.0, 0.95, 0.8), vec3(0.25, 0.3, 0.35), den), den);
        col.xyz *= lin;
        // fog
        col.xyz = mix(col.xyz, bgcol, 1.0 - exp2(-0.075 * t));
        // composite front to back
        col.w = min(col.w * 8.0 * dt, 1.0);
        col.rgb *= col.a;
        sum += col * (1.0 - sum.a);
        // back to front?
        //   col.w    += den;

        // col.rgb *= col.a;
        // sum += col;
      }
    }

    // advance ray
    t += dt;
    // until far clip or full opacity
    if (t > tmax || sum.a > 0.99 || length(pos) < yb - eps || length(pos) > yt + eps || t >= depth_buffer)
      break;
  }

  return clamp(sum, 0.0, 1.0);
}

vec3 rayDirection(float fieldOfView, vec2 size, vec2 fragCoord)
{
  vec2 xy = fragCoord - size * 0.5;
  float z = 0.5 * size.y / tan(radians(fieldOfView));
  return vec3(xy, -z);
}

vec4 render(vec3 eye, vec3 marchDir, float start, float end, float depth_buffer, ivec2 px)
{
  float depth = start;
  // for (int i = 0; i < maxMarch; i++)
  //{
  vec3 march = depth * marchDir;
  float dist = length(eye + march) - cloud_rad;

  // if (dist < eps || length(eye) < cloud_rad)
  // {
  // return render( eye, marchingDirection, ivec2(fragC-0.5), sundir );
  float sun = clamp(dot(sundir, marchDir), 0.0, 1.0);

  // background sky
  // vec3 col = vec3(0.76,0.75,0.86);
  // col -= 0.6*vec3(0.90,0.75,0.95)*marchDir.y;
  // col += 0.2*vec3(1.00,0.60,0.10)*pow( sun, 8.0 );
  vec3 col = vec3(0.0);

  // clouds
  vec4 res = raymarch(eye, marchDir, col, px, sundir, depth, depth_buffer);
  col = col * (1.0 - res.w) + res.xyz;

  // sun glare
  col += 0.2 * vec3(1.0, 0.4, 0.2) * pow(sun, 3.0);

  // tonemap
  col = smoothstep(0.15, 1.1, col);
  // col = vec3(0.9,0.1,0.8);
  //   return vec4( col, 1.0 );
  return res;
  // return vec4(1.,0.,0.,0.1);
  // }
  if (depth >= end)
  {
    return vec4(0.0, 1., 0.0, 0.0);
  }
  if (depth > depth_buffer + buffBuff)
  {
    return vec4(0.0);
  }
  depth += dist;
  //}
  // return vec4(0.0, 0.0, 1.,0.1);
}

void vertex()
{
  POSITION = vec4(VERTEX, 1.0);
}

void fragment()
{
  float depth = texture(DEPTH_TEXTURE, SCREEN_UV).x;
  vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
  vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
  view.xyz /= view.w;
  // float linear_depth = -view.z;
  float max_distance = length(view.xyz);

  vec4 world = CAMERA_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);

  vec3 world_position = world.xyz / world.w;
  vec3 world_camera = (CAMERA_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
  vec3 eye = CAMERA_MATRIX[3].xyz;
  float lEye = length(eye);
  vec3 dir = rayDirection(35.0, VIEWPORT_SIZE, FRAGCOORD.xy);
  dir = normalize(dir.x * CAMERA_MATRIX[0].xyz + dir.y * CAMERA_MATRIX[1].xyz + dir.z * CAMERA_MATRIX[2].xyz);
  float startMagSq = dot(eye, eye);
  float b = 2.0 * dot(dir, eye);
  float c = startMagSq - cloud_rad * cloud_rad;
  float bb = b * b;
  float d = bb - 4.0 * c;
  // if (lEye - cloud_rad < linear_depth + buffBuff)
  if (d > 0.)
  {
    vec2 ray_length = vec2(
        max((-b - sqrt(d)) * 0.5, 0.0), // + atmo_rad			  * 0.00001,
        min((-b + sqrt(d)) * 0.5, max_distance));
    if (ray_length.x < ray_length.y)
    {
      // vec3 sundir = vec3(sin(TIME),0.0,-cos(TIME));
      float minD = ray_length.x;
      // float maxD = lEye;
      ivec2 px = ivec2((FRAGCOORD.xy - 0.5) * 256. / VIEWPORT_SIZE);

      vec4 fragColor = render(eye, dir, minD, lEye, max_distance, px);
      // ALBEDO = shortestDistanceToSurface(eye, dir, minD, maxD,linear_depth);
      // ALPHA = 0.1;

      // vec4 fragColor = render( eye, dir, ivec2(FRAGCOORD.xy-0.5), sundir );
      ALBEDO = fragColor.rgb;
      ALPHA = fragColor.a;
    }
  }
  else
  {
    ALPHA = 0.0;
  }

  // ALPHA*=clamp(1.0-smoothstep(world_position.z+1.0,world_position.z,VERTEX.z),0.0,1.0);
  ALPHA_SCISSOR = 0.01f;
}