// NOTE: Shader automatically converted from Godot Engine 3.4.2.stable.mono's SpatialMaterial.

shader_type spatial;
render_mode blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx;
uniform vec4 albedo : hint_color;
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0,1);
uniform float point_size : hint_range(0,128);
uniform vec3 sun;
//float sphintersect() {

//float sphintersect(vec3 ro, vec3 rd, vec4 sph) {
//		vec3 oc = ro - sph.xyz;
//		float b = dot( oc, rd );
//		float c = dot( oc, oc ) - sph.w*sph.w;
//		float h = b*b - c;
//		if( h<0.0 ) return -1.0;
//		h = sqrt( h );
//		return -b - h;
//}
float sphIntersect( in vec3 ro, in vec3 rd, in vec4 sph )
{
	vec3 oc = ro - sph.xyz;
	float b = dot( oc, rd );
	float c = dot( oc, oc ) - sph.w*sph.w;
	float h = b*b - c;
	if( h<0.0 ) return -1.0;
	return -b - sqrt( h );
}


float sphSoftShadow( in vec3 ro, in vec3 rd, in vec4 sph, in float k )
{
    vec3 oc = ro - sph.xyz;
    float b = dot( oc, rd );
    float c = dot( oc, oc ) - sph.w*sph.w;
    float h = b*b - c;
    

    // physically plausible shadow
    float d = sqrt( max(0.0,sph.w*sph.w-h)) - sph.w;
    float t = -b - sqrt( max(h,0.0) );
    return (t<0.0) ? 1.0 : smoothstep(0.0, 1.0, 2.5*k*d/t );

 
}    
            
float sphOcclusion( in vec3 pos, in vec3 nor, in vec4 sph )
{
    vec3  r = sph.xyz - pos;
    float l = length(r);
    return dot(nor,r)*(sph.w*sph.w)/(l*l*l);
}

vec3 sphNormal( in vec3 pos, in vec4 sph )
{
    return normalize(pos-sph.xyz);
}
float test()
{return 2.0;}

void vertex() {

	MODELVIEW_MATRIX = INV_CAMERA_MATRIX * mat4(CAMERA_MATRIX[0],CAMERA_MATRIX[1],CAMERA_MATRIX[2],WORLD_MATRIX[3]);
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(vec4(length(WORLD_MATRIX[0].xyz), 0.0, 0.0, 0.0),vec4(0.0, length(WORLD_MATRIX[1].xyz), 0.0, 0.0),vec4(0.0, 0.0, length(WORLD_MATRIX[2].xyz), 0.0),vec4(0.0, 0.0, 0.0, 1.0));
	//	vec4 world_pos = INV_PROJECTION_MATRIX * vec4(UV*2.0-1.0,DEPTH_TEXTURE*2.0-1.0,1.0);
		

}

void fragment() {

			float depth_tex = textureLod(DEPTH_TEXTURE,SCREEN_UV,0.0).r;
		vec4 world_pos = INV_PROJECTION_MATRIX * vec4(SCREEN_UV*2.0-1.0,depth_tex*2.0-1.0,1.0);
	world_pos.xyz/=world_pos.w;
float testy = test();
vec3 boom = CAMERA_MATRIX[3].xyz;
vec3 boomy = WORLD_MATRIX[3].xyz;


		float rayHit = sphIntersect(CAMERA_MATRIX[3].xyz,normalize(CAMERA_MATRIX[3].xyz-world_pos.xyz),vec4(VERTEX,0.5));

vec3 worldNormal = normalize(world_pos.xyz*rayHit - VERTEX);
lowp vec3 worldLightDir = -normalize(sun);
lowp float ndotl = clamp(0.0,1.0,dot(worldNormal, worldLightDir));


	//vec4 albedo_tex = texture(texture_albedo,UV);
		vec4 albedo_tex = vec4(1.0);
		albedo_tex *= COLOR[3];

float circleAlpha = 1.0-smoothstep(0.2, 0.25, dot(UV-0.5, UV-0.5));
	ALBEDO = albedo_tex.rgb*ndotl;

	METALLIC = metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
		ALPHA = circleAlpha * albedo_tex.a;
	//ALPHA = COLOR[3];

		ALPHA*=clamp(1.0-smoothstep(world_pos.z+1.0,world_pos.z,VERTEX.z),0.0,1.0);
		ALPHA_SCISSOR=0.01f;


}
