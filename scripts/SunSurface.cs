using Godot;
using System;

public class SunSurface : MeshInstance
{
  float sun_curve;
  Vars vars;
  ShaderMaterial shade = new ShaderMaterial();
  Shader shader;

  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");

	shader = GD.Load<Shader>("res://sun.gdshader");
	shade.Shader = shader;

	shade.SetShaderParam("texture_mask", GD.Load("res://.import/FireMask.png-1e1c127a29681f50e85d666456fd7e72.s3tc.stex"));
	shade.SetShaderParam("noise_texture", GD.Load("res://noisetexture.tres"));
	shade.SetShaderParam("texture_scale", 1.0F);
	shade.SetShaderParam("emission_intensity", 2.0F);
	shade.SetShaderParam("time_scale", 3.0F);
	shade.SetShaderParam("edge_softness", 0.1F);
	shade.SetShaderParam("sunSet", 0.0F);

  }
  public override void _PhysicsProcess(float delta)
  {

	sun_curve = 256F / (1F + Mathf.Pow(2.7F, 20F * (vars.sun_ang - 0.2F - Mathf.Asin(vars.planet_radius / (vars.planet_radius + vars.cam_alt)))));

	shade.SetShaderParam("sunSet", sun_curve);

	MaterialOverride = shade;
  }
}
