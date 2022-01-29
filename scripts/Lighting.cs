using Godot;
using System;

public class Lighting : WorldEnvironment
{
  float fog_curve;

  Vars vars;
  MeshInstance SunSurface;
  DirectionalLight Sun;
  MeshInstance Atmosphere;

  Transform sunTransform;


  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	SunSurface = (MeshInstance)GetNode("SunSurface");
	Sun = (DirectionalLight)GetNode("Sun");
	Atmosphere = (MeshInstance)GetNode("/root/Space/Planet/Surface/Atmosphere");

  }
  public override void _PhysicsProcess(float delta)
  {
	Environment.FogDepthBegin = Mathf.Min(vars.cam_alt, vars.planet_radius);
	vars.sun_ang = vars.cam_pos.AngleTo(-Atmosphere.Transform.basis.z);

	fog_curve = 0.1F * (Mathf.Cos((Mathf.Max(vars.sun_ang, Mathf.Pi / 2F) - Mathf.Pi) * 2F) + 1F);

	Sun.RotateY(0.01f * delta);


	Environment.BackgroundSkyRotation = Sun.Rotation;
	Atmosphere.Rotation = Sun.Rotation;

	sunTransform.basis = SunSurface.GlobalTransform.basis;
	sunTransform.origin = vars.cam_pos + Atmosphere.Transform.basis.z * 1000f;
	SunSurface.GlobalTransform = sunTransform;
  }
}
