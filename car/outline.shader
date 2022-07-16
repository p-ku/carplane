// NOTE: Shader automatically converted from Godot Engine 3.4.2.stable.mono's SpatialMaterial.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx, unshaded;
uniform vec4 albedo : hint_color;
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0, 1);
uniform float point_size : hint_range(0, 128);
// varying vec3 lightDir;
uniform vec3 sun;
uniform float lati;

// varying vec3 p;
uniform float LINEAR_DENSITY = 1.0; // 0: constant
uniform float DENS = 2.0;           // tau.rho
uniform float rad = 0.3;            // sphere radius
uniform float H = 0.05;             // skin layer thickness (for linear density)
uniform bool ANIM = false;          // true/false
uniform float PI = 3.14159;

uniform vec4 skyColor = vec4(.7, .8, 1., 1.);
uniform vec3 sunColor = vec3(10.0, 7.0, 1.0); // NB: is Energy
// vec2 FragCoord;

// --- noise functions from https://www.shadertoy.com/view/XslGRr
// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

uniform mat3 m = mat3(vec3(0.00, 0.80, 0.60),
                      vec3(-0.80, 0.36, -0.48),
                      vec3(-0.60, -0.48, 0.64));

float hash(float n)
{
    return fract(sin(n) * 43758.5453);
}

float noise(in vec3 x)
{
    vec3 p = floor(x);
    vec3 f = fract(x);

    f = f * f * (3.0 - 2.0 * f);

    float n = p.x + p.y * 57.0 + 113.0 * p.z;

    float res = mix(mix(mix(hash(n + 0.0), hash(n + 1.0), f.x),
                        mix(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                    mix(mix(hash(n + 113.0), hash(n + 114.0), f.x),
                        mix(hash(n + 170.0), hash(n + 171.0), f.x), f.y),
                    f.z);
    return res;
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
// --- End of: Created by inigo quilez --------------------

vec3 noise3(vec3 p)
{
    if (ANIM)
        p += TIME;
    float fx = noise(p);
    float fy = noise(p + vec3(1345.67, 0, 45.67));
    float fz = noise(p + vec3(0, 4567.8, -123.4));
    return vec3(fx, fy, fz);
}
vec3 fbm3(vec3 p)
{
    if (ANIM)
        p += TIME;
    float fx = fbm(p);
    float fy = fbm(p + vec3(1345.67, 0, 45.67));
    float fz = fbm(p + vec3(0, 4567.8, -123.4));
    return vec3(fx, fy, fz);
}
vec3 perturb3(vec3 p, float scaleX, float scaleI)
{
    scaleX *= 2.;
    return scaleI * scaleX * fbm3(p / scaleX); // usually, to be added to p
}

float constantDensityTransmittance(float NDotL, float NDotO)
{
    return NDotL / (DENS * (NDotL + NDotO));
}

float linearDensityTransmittance(float NDotL, float NDotO, float LDotO, vec2 FragCoord, vec2 iResolution)
{
    // if (FragCoord.y/iResolution.y>.42)
    //	return sqrt(PI/2.) / sqrt(DENS/H* NDotO/NDotL*(NDotL+NDotO) ) ; // test1
    // else
    //  return .15*DENS*NDotL/(NDotL+NDotO)*sqrt(1.-LDotO*LDotO);       // test2
    return .15 * DENS * NDotL / (NDotL + NDotO); // test3
}

// 1/2 ray length inside object
vec4 intersectSphere(vec3 rpos, vec3 rdir, vec3 opos)
{

    vec3 op = opos - rpos;

    float eps = 1e-5;
    float b = dot(op, rdir);
    float det = b * b - dot(op, op) + rad * rad;

    if (det > 0.0)
    {
        det = sqrt(det);
        float t = b - det;
        if (t > eps)
        {
            vec4 P = vec4(normalize(-op + rdir * t), t);
            P[3] = P.z * rad;
            // Rz = rad*P.z;   // 1/2 ray length inside object
            //#if LINEAR_DENSITY
            //            // skin layer counts less
            float dH = 1. + H * (H - 2. * rad) / (P[3] * P[3]);
            if (dH > 0.) // core region
                P[3] *= .5 * (1. + sqrt(dH));
            else
                P[3] *= .5 * rad * (1. - sqrt(1. - P[3] * P[3] / (rad * rad))) / H;
            //#endif
            return P;
        }
    }

    return vec4(0.0);
}

float computeNormal(in vec3 cameraPos, in vec3 cameraDir, in vec3 opos, out vec3 normal)
{
    cameraPos = cameraPos + perturb3(cameraDir, .06, 1.5);
    vec4 intersect = intersectSphere(cameraPos, cameraDir, opos);
    if (intersect.w > 0.)
    {
        normal = intersect.xyz;
        // normal = normalize(normal+perturb3(normal,.3,30.));
        return rad * intersect.z;
    }
    return 0.0;
}
vec2 computeTransmittance(in vec3 cameraPos, in vec3 cameraDir, vec3 lightDir, vec2 FragCoord, vec2 iResolution, vec3 opos)
{
    vec3 normal;
    float Rz = computeNormal(cameraPos, cameraDir, opos, normal);
    if (Rz > 0.0)
    {
        float NDotL = clamp(dot(normal, lightDir), 0., 1.);
        float NDotO = clamp(dot(normal, cameraPos), 0., 1.);
        float LDotO = clamp(dot(lightDir, cameraPos), 0., 1.);

        //#if LINEAR_DENSITY
        float transmittance = linearDensityTransmittance(NDotL, NDotO, LDotO, FragCoord, iResolution) * .5;
        //#else
        //       float transmittance = constantDensityTransmittance(NDotL,NDotO);
        //#endif
        return vec2(transmittance, Rz);
    }

    return vec2(-1., 0.);
}

float test()
{
    return 2.0;
}

void vertex()
{

    MODELVIEW_MATRIX = INV_CAMERA_MATRIX * mat4(CAMERA_MATRIX[0], CAMERA_MATRIX[1], CAMERA_MATRIX[2], WORLD_MATRIX[3]);
    MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(vec4(length(WORLD_MATRIX[0].xyz), 0.0, 0.0, 0.0), vec4(0.0, length(WORLD_MATRIX[1].xyz), 0.0, 0.0), vec4(0.0, 0.0, length(WORLD_MATRIX[2].xyz), 0.0), vec4(0.0, 0.0, 0.0, 1.0));
    //	vec4 world_pos = INV_PROJECTION_MATRIX * vec4(UV*2.0-1.0,DEPTH_TEXTURE*2.0-1.0,1.0);
    // lightDir = vec3(sin(10.*INSTANCE_CUSTOM.z),0.,cos(10.*INSTANCE_CUSTOM.z));
}

void fragment()
{
    // vec4 albedo_tex = texture(texture_albedo,UV);
    //		vec4 albedo_tex = vec4(1.0);
    //		albedo_tex *= COLOR[3];
    // float circleAlpha = 1.0-smoothstep(0.2, 0.25, dot(UV-0.5, UV-0.5));
    // ALBEDO = albedo_tex.rgb;

    //		ALPHA =  albedo_tex.a;
    // ALPHA = COLOR[3];
    float depth_tex = textureLod(DEPTH_TEXTURE, SCREEN_UV, 0.0).r;
    vec4 world_pos = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth_tex * 2.0 - 1.0, 1.0);
    world_pos.xyz /= world_pos.w;

    // vec2 iResolution =  VIEWPORT_SIZE;
    // vec2 iResolution =  UV;
    vec2 iResolution = vec2(1);
    // vec2 iResolution = UV*256.;
    vec2 fragc = UV;

    vec3 cameraPos = vec3(0.0, 0.0, 1.0);
    // vec3 cameraPos =CAMERA_MATRIX[3].xyz  ;
    vec3 cameraTarget = vec3(0.0, 0.0, 0.0);
    // vec3 cameraTarget = -CAMERA_MATRIX[2].xyz;

    vec3 ww = normalize(cameraPos - cameraTarget);
    vec3 uu = normalize(cross(vec3(0.0, 1.0, 0.0), ww));
    // vec3 uu = normalize(cross( world_pos.xyz, ww ));

    vec3 vv = normalize(cross(ww, uu));
    vec2 q = (fragc) / iResolution.xy;
    // vec2 q = vec2(0.,0.);
    vec2 p = -1.0 + 2.0 * q;
    p.x *= iResolution.x / iResolution.y;
    vec3 cameraDir = normalize(p.x * uu + p.y * vv - 1.5 * ww);
    // vec3 cameraDir = normalize( -CAMERA_MATRIX[2].xyz);

    mat4 camXform = mat4(vec4(uu, 0.),
                         vec4(vv, 0.),
                         vec4(ww, 0.),
                         vec4(0., 0., 0., 1.)) *
                    CAMERA_MATRIX;
    // cameraPos += camXform[3].xyz;
    // cameraTarget = normalize( p.x*camXform[0].xyz + p.y*camXform[1].xyz + camXform[2].xyz );

    // vec3 opos = vec3(0.,0.,1.)+camXform[3].xyz;
    vec3 opos = vec3(0., 0., 0.);
    //	vec3 lightDir = vec3(sin(10.*sun),0.,cos(10.*sun));
    vec3 lightDir = sun;
    //	 lightDir = vec3(sin(10.*lati),0.,cos(10.*lati));

    // light
    // float theta = (iMouse.x / iResolution.x *2. - 1.)*PI;
    // float phi = (iMouse.y / iResolution.y - .5)*PI;
    // vec3 lightDir =vec3(sin(theta)*cos(phi),sin(phi),cos(theta)*cos(phi));
    // vec3 lightDir = vec3(sin(30.*sun),0.,-cos(30.*sun));
    // lightDir = normalize( lightDir.x*camXform[0].xyz + lightDir.y*camXform[1].xyz + lightDir.z*camXform[2].xyz );
    // lightDir = normalize( lightDir.x*CAMERA_MATRIX[0].xyz + lightDir.y*CAMERA_MATRIX[1].xyz + lightDir.z*CAMERA_MATRIX[2].xyz );
    // lightDir = normalize( lightDir );

    // shade object
    // vec2 transmittance = computeTransmittance( cameraPos, cameraDir, lightDir,FRAGCOORD.xy,iResolution,NORMAL );
    vec2 transmittance = computeTransmittance(cameraPos, cameraDir, lightDir, fragc, iResolution, opos);

    // display: special cases
    // if (abs(q.y-0.42)*iResolution.y<.5)  ALBEDO = vec3(.75);
    if (transmittance.x < 0.)
    {
        // ALBEDO = skyColor.xyz;
        ALPHA = 0.0;
    }
    else if (transmittance.x > 1.)
    {
        ALBEDO = vec3(1., 0., 0.);
        ALPHA = 1.0;
    }
    else
    {   // display: object
        // float Rz = 1.-exp(-8.*DENS*rad*intersect.z);

        float alpha = 1. - exp(-8. * DENS * transmittance.y);
        // fragColor = vec4(alpha); return; // for tests
        vec3 frag = vec3(transmittance.x, transmittance.x, transmittance.x);
        // vec4 tex = texture(frag,UV);
        // vec4 color1 = texture(texture1, vec2(UV*texture1_scale + texture1_offset + additional_offset + normalize(texture1_pan_direction) * texture1_pan_speed * lifetime));

        vec4 albedo_tex = vec4(0.0);
        //	albedo_tex *= COLOR[3];
        ALBEDO = (frag * sunColor + (1. - alpha) * skyColor.xyz);
        ALPHA = (alpha + (1. - alpha) * skyColor[3]);
    }

    // display: light
    // float d = length(vec2(lightDir)-p)*iResolution.x;
    // float Z; if (lightDir.z>0.) Z=1.; else Z=0.;
    // if (d<10.) ALBEDO = vec4(1.,Z,.5,1.);

    // vec3 normal;
    // computeNormal(cameraPos,cameraDir, normal);
    // fragColor = vec4(normal,1.);

    ALPHA *= clamp(1.0 - smoothstep(world_pos.z + 1.0, world_pos.z, VERTEX.z), 0.0, 1.0);
    ALPHA_SCISSOR = 0.01f;
}
