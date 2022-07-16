// NOTE: Shader automatically converted from Godot Engine 3.4.2.stable.mono's ParticlesMaterial.

shader_type particles;

uniform float lift;
uniform float lati;
//uniform float sun;

uniform sampler2D scale_texture;
uniform sampler2D color_ramp;
//uniform float scale;


void vertex() {
	bool restart = false;
	float pi = 3.14159;


	if (CUSTOM.y > 1.0) {
		restart = true;
	}
	if (RESTART || restart) {
//COLOR[3] = 1.0/(1.0+1.0/angDamp)/10.0;
float temperature;
float altitude = length(EMISSION_TRANSFORM[3].xyz)-27.0;

//COLOR[3] = -smoothstep(27.0,52.0,length(EMISSION_TRANSFORM[3].xyz))*cos(lati)*angDamp/4.0;
//COLOR[3] = -cos(lati)*sin(0.120830487*clamp(0.0,26.0,altitude))*angDamp/4.0;
//COLOR[3] = -cos(lati)*sin(0.120830487*clamp(0.0,26.0,altitude))*lift*2.0;

COLOR[3] = sin(0.120830487*clamp(0.0,13.0,altitude))*lift*2.0;

//float lati = dot(EMISSION_TRANSFORM[3].xyz,vec3(0.0,1.0,0.0))/length(EMISSION_TRANSFORM[3].xyz);
//float lati = length(cross(EMISSION_TRANSFORM[3].xyz,vec3(0.0,1.0,0.0)))/length(EMISSION_TRANSFORM[3].xyz);
//float lati = abs(sin(acos(dot(EMISSION_TRANSFORM[3].xyz,vec3(0.0,1.0,0.0))/length(EMISSION_TRANSFORM[3].xyz))));
//float lati = abs(normalize(EMISSION_TRANSFORM[3].xyz).z);
		//{
			

			vec2 wind_normal = normalize(vec2(EMISSION_TRANSFORM[3].z,EMISSION_TRANSFORM[3].x));
			vec2 wind = sin(lati)*0.26*(vec2(-wind_normal.y,wind_normal.x));
			VELOCITY = vec3(wind.y,0.0,wind.x);

			
			
	//	}
		CUSTOM.w = lati;
		CUSTOM.x = COLOR[3];
		CUSTOM.y = 0.0;

		VELOCITY = (EMISSION_TRANSFORM * vec4(VELOCITY, 0.0)).xyz;
		TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
	}
	else
	{
				CUSTOM.y += DELTA / LIFETIME;
			//tv = CUSTOM.y / CUSTOM.w;

	//	vec3 org = EMISSION_TRANSFORM[3].xyz;
	//	vec3 diff = pos - org;
	//	// apply tangential acceleration;
	//	vec3 crossDiff = cross(normalize(diff), normalize(gravity));
	//	force += length(crossDiff) > 0.0 ? normalize(crossDiff) * ((tangent_accel + tex_tangent_accel) * mix(1.0, rand_from_seed(alt_seed), tangent_accel_random)) : vec3(0.0);
	//	// apply attractor forces
	//	VELOCITY += force * DELTA;
		//	vec3 pos = TRANSFORM[3].xyz;
			vec2 wind_normal = normalize(vec2(TRANSFORM[3].z,TRANSFORM[3].x));
			vec2 wind = sin(CUSTOM.w)*0.26*(vec2(-wind_normal.y,wind_normal.x));
		VELOCITY = vec3(wind.y,0.0,wind.x);


//				float color_scale = mix(angColor,0.0,tv);
//
//	mat4 hue_rot_mat = mat4(vec4(0.299, 0.587, 0.114, 0.0),
//			vec4(0.299, 0.587, 0.114, 0.0),
//			vec4(0.299, 0.587, 0.114, 0.0),
//			vec4(0.000, 0.000, 0.000, 1.0)) +
//		mat4(vec4(0.701, -0.587, -0.114, 0.0),
//			vec4(-0.299, 0.413, -0.114, 0.0),
//			vec4(-0.300, -0.588, 0.886, 0.0),
//			vec4(0.000, 0.000, 0.000, 0.0));

	//COLOR = hue_rot_mat * textureLod(color_ramp, vec2(tv, 0.0), 0.0) * vec4(1.0);
		
		
//		COLOR = hue_rot_mat * vec4(1.0,1.0,1.0,1.0);
		
		//COLOR[3] += -base_scale;
		//COLOR[3] = max(0.0, COLOR[3]);
//COLOR[3] -= 1.0/((LIFETIME/DELTA)*(1.0-CUSTOM.y));


	//CUSTOM.x = 0.0;
	//	CUSTOM.z = 0.0;
	}
		float tex_scale = textureLod(scale_texture, vec2(CUSTOM.y, 0.0), 0.0).r;

	TRANSFORM[0].xyz = normalize(TRANSFORM[0].xyz);
	TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
	TRANSFORM[2].xyz = normalize(TRANSFORM[2].xyz);
	//float base_scale = tex_scale * mix(scale, 1.0,0.0);
	//	float base_scale = 0.5*mix(0.0,1.0,tv)+0.2;
	//			float base_scale = 0.3*smoothstep(0.0,1.0,tv)+0.2;

				//float scaler = mix(1.0,2.0,CUSTOM.y);
								//float base_scale = 0.4*scaler+0.2;
								float base_scale = mix(0.2,2.0,CUSTOM.y);
base_scale = 3.;
		//			float base_scale = 0.4*tex_scale+0.2;
//COLOR[3] -= DELTA/(LIFETIME-LIFETIME*CUSTOM.y);
//COLOR[3] -= 1.0/((LIFETIME/DELTA)*(1.0-CUSTOM.y));
//COLOR[3] *= textureLod(color_ramp, vec2(CUSTOM.y, 0.0), 0.0);
	//COLOR[3] *= 1.0 - tex_scale;
	//	COLOR[3] = COLOR[3] - COLOR[3]*scaler;
	COLOR[3] = mix(1.0,0.0,CUSTOM.y)*CUSTOM.x;
//COLOR[3] = 1.0;
	if (base_scale < 0.000001) {
		base_scale = 0.000001;
	}
	TRANSFORM[0].xyz *= base_scale;
	TRANSFORM[1].xyz *= base_scale;
	TRANSFORM[2].xyz *= base_scale;
	//	CUSTOM.z = sun;


	if (CUSTOM.y > CUSTOM.w) { ACTIVE = false; }
}

