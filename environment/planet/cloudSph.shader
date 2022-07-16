// NOTE: Shader automatically converted from Godot Engine 3.4.2.stable.mono's SpatialMaterial.

shader_type spatial;
//render_mode blend_mix,depth_draw_opaque,cull_disabled,diffuse_burley,specular_schlick_ggx;
//render_mode cull_disabled,depth_draw_always;
render_mode depth_draw_alpha_prepass, cull_disabled;
//uniform vec4 albedo : hint_color;
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0,1);
uniform float point_size : hint_range(0,128);
uniform sampler2D texture_metallic : hint_white;
uniform vec4 metallic_texture_channel;
uniform sampler2D texture_normal : hint_normal;
uniform float normal_scale : hint_range(-16,16);

uniform sampler2D bayer;
uniform sampler3D noise_vol;
uniform float pi = 3.14159;

void vertex() {
	UV=UV+1.;
}

void fragment() {
float y = sin( UV.y * pi - pi/2. );
float x = cos( UV.x * 2.* pi ) * cos( UV.y* pi - pi/2. );
float z = sin( UV.x * 2.* pi ) * cos( UV.y *pi - pi/2. );

y = y * 0.5 + 0.5;
x = x * 0.5 + 0.5;
z = z * 0.5 + 0.5;

	vec2 base_uv = UV;
vec4 albedo_tex = texture(noise_vol,vec3(x,y,z));
	//vec4 albedo_tex = textureLod(texture_albedo,base_uv);
	ALBEDO = albedo_tex.rgb;
	ALPHA = albedo_tex.r;
//		if (texture(bayer, vec2(UV.x*4095.+0.5,UV.y*4095.+0.5)).r > ALPHA) {
				if (texture(bayer, UV*4095.+0.5).r > ALPHA) {
	//if (texture(bayer, UV*2047.+0.5).r > ALPHA) {
		//			if (texture(bayer, vec2(0.+TIME/4.,0.+TIME/4.)).r > ALPHA) {

		discard;
	}
	//float metallic_tex = dot(texture(texture_metallic,base_uv),metallic_texture_channel);
	//METALLIC = metallic_tex * metallic;
	//ROUGHNESS = roughness;
	//SPECULAR = specular;
	NORMALMAP = texture(texture_albedo,base_uv).rgb;
	NORMALMAP_DEPTH = normal_scale;
}
