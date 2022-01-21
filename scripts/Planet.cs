using Godot;
using System;

public class Planet : StaticBody
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public Vars vars;
	public override void _Ready()
	{
		vars = (Vars)GetNode("/root/Vars");

	//	var arad = vars.atmo_radius;
	//	var prad = vars.planet_radius;

		MeshInstance atmos = (MeshInstance)GetNode("Surface/Atmosphere");
		Material mat = atmos.GetSurfaceMaterial(0);
		ShaderMaterial shade = mat as ShaderMaterial;

		//shade.SetShaderParam("atmo_radius",arad);
		//shade.SetShaderParam("planet_radius",prad);
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _PhysicsProcess(float delta)
  {

		MeshInstance atmos = (MeshInstance)GetNode("Surface/Atmosphere");
		Material mat = atmos.GetSurfaceMaterial(0);
		ShaderMaterial shade = mat as ShaderMaterial;
	 	atmos.RotateY(vars.spin);
	//$Grass.material_override.set_shader_param("character_position", Vars.car_pos)
	//vars.sun_ang = vars.cam_pos.angle_to(-atmos.Transform.Basis.Z);
	//Vars.sun_ang = Vars.cam_basis.z.angle_to($Surface/Atmosphere.transform.basis.z) 
  }
}
