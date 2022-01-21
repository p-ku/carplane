using Godot;
using System;

public class Lighting : WorldEnvironment
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	Vector3 rot;
	float ang;
	float foggy;
	float fog_curve;
	//float sun_curve;

	Vars vars;
	MeshInstance SunSurface;
	DirectionalLight Sun;
	MeshInstance Atmosphere;
	//Material mat;
	//ShaderMaterial shade;
	//float param;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		vars = (Vars)GetNode("/root/Vars");
		SunSurface = (MeshInstance)GetNode("SunSurface");
		Sun = (DirectionalLight)GetNode("Sun");
		Atmosphere = (MeshInstance)GetNode("/root/Space/Planet/Surface/Atmosphere");
		//mat = SunSurface.GetSurfaceMaterial(0);
		//ShaderMaterial shade = mat as ShaderMaterial;
		//ShaderMaterial shade = (ShaderMaterial)mat;
		//param = (float)shade.GetShaderParam("sunSet");
		//DebugOverlay.stats.add_property(self, "foggy", "")
	}
	public override void _PhysicsProcess(float delta)
	{
		//sun_curve = 256F/(1F+Mathf.Pow(2.7F,20F*(vars.sun_ang-0.2F-Mathf.Asin(vars.planet_radius/(vars.planet_radius+vars.cam_alt)))));
		fog_curve = 0.1F*(Mathf.Cos((Mathf.Max(vars.sun_ang,Mathf.Pi/2F)-Mathf.Pi)*2F)+1F);
		rot = Environment.BackgroundSkyRotation;
		Environment.BackgroundSkyRotation = rot + new Vector3(0,vars.spin,0);
		Sun.RotateY(vars.spin);
		
	   //environment.fog_depth_begin = Vars.cam_dist
		//environment.fog_depth_end = max(environment.fog_depth_begin,Vars.cam_dist+Vars.planet_radius/3)
		//environment.fog_color.a = fog_curve
		SunSurface.GlobalTransform = new Transform(SunSurface.GlobalTransform[0],
							SunSurface.GlobalTransform[1],
							SunSurface.GlobalTransform[2],
							vars.cam_pos + Atmosphere.Transform.basis.z*1000);
		


		//shade.SetShaderParam("sun_set", sun_curve);
		//(mat as ShaderMaterial).SetShaderParam("sunSet", sun_curve);
		//SunSurface.MaterialOverride = shade;


		foggy = Environment.FogColor.a;
	}
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
