shader_type particles;

uniform float angular_spacing = 0.5;
uniform float outward_amt = 3;
uniform float min_cloud_size = 0.03;
uniform float max_cloud_size = 1.0;
uniform sampler2D noise;
uniform int count = 100;
uniform float radius = 26;
float rand_from_seed(in uint seed)
{
  int k;
  int s = int(seed);
  if (s == 0)
    s = 305420679;
  k = s / 127773;
  s = 16807 * (s - k * 127773) - 2836 * k;
  if (s < 0)
    s += 2147483647;
  seed = uint(s);
  return float(seed % uint(65536)) / 65535.0;
}

uint hash(uint x)
{
  x = ((x >> uint(16)) ^ x) * uint(73244475);
  x = ((x >> uint(16)) ^ x) * uint(73244475);
  x = (x >> uint(16)) ^ x;
  return x;
}

void vertex()
{
  if (RESTART)
  {
    float between = (2.0 * radius) / float(count);

    // create some random seeds
    float init_seed_x = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float init_seed_y = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float init_seed_z = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float alt_seed_1 = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float alt_seed_2 = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float alt_seed_3 = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float alt_seed_4 = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));
    float alt_seed_5 = rand_from_seed(hash(uint(INDEX) + RANDOM_SEED));

    float x = init_seed_x / sqrt(init_seed_x * init_seed_x + init_seed_y * init_seed_y + init_seed_z * init_seed_z);
    float y = (float(INDEX) - float(count / 2)) * between;
    float z = init_seed_z / sqrt(init_seed_x * init_seed_x + init_seed_y * init_seed_y + init_seed_z * init_seed_z);
    // float spie =
    // position clouds in spiral
    float pt_index = float(INDEX) + alt_seed_1;
    pt_index = sqrt(pt_index);

    float scale = mix(min_cloud_size, max_cloud_size, 1.0 - pow(alt_seed_3, 2)) * COLOR[1];

    x += (outward_amt + alt_seed_4 / (1.0 + 3.0 * scale)) * cos(angular_spacing * pt_index);
    // y += alt_seed_2;
    z += (outward_amt + alt_seed_5 / (1.0 + 3.0 * scale)) * sin(angular_spacing * pt_index);

    TRANSFORM[3].xyz = vec3(x, y, z);
    TRANSFORM[0].x *= scale;
    TRANSFORM[1].y *= scale;
    TRANSFORM[2].z *= scale;
  }
}