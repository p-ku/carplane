shader_type particles;

render_mode disable_force, disable_velocity;

// this defines how far out the particles will spawn
uniform float particle_diameter = 5f;
uniform float angDamp = 0.0;
uniform sampler3D noise_vol;

// input a value that changes over time and can also be a coordinate
// use vec(TIME,0) if you got no coordinates
float rand(vec2 co)
{
    // this will give a pseudorandom value between 0-1
    return fract(sin(dot(co.xy, vec2(23.21, 101.83))) * 34759.214);
}

void vertex()
{

    CUSTOM[3] = angDamp;
}