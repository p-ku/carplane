using Godot;
using System;


public class ChaseCamera : Camera
{
  [Export]
  float LerpSpeed = 20F;
  public Vars vars;
  Position3D target = null;


  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
  }
  public override void _PhysicsProcess(float delta)
  {
	if (target == null)
	{
	  return;
	}
	GlobalTransform = GlobalTransform.InterpolateWith(target.GlobalTransform, LerpSpeed * delta);
	vars.cam_pos = GlobalTransform.origin;
	vars.cam_alt = vars.cam_pos.Length() - vars.planet_radius;
	vars.cam_dist = vars.car_pos.DistanceTo(GlobalTransform.origin);
	vars.cam_basis = GlobalTransform.basis;
	//Fov = 120F + vars.cam_dist / 10F;

  }
  private void _on_CameraPositions_ChangeCamera(Position3D t)
  {
	target = t;
  }
}






