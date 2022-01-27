using Godot;
using System;

public class CarAnchor : RigidBody
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";
  Vars vars;
  Transform previousTransform;
  Transform target;
  Vector3 upDir;
  Vector3 curDir;
  Vector3 targetDir;
  Vector3 targetPosition;
  RayCast tester;
  float rotationAngle;
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");

  }

  /*   public override void _IntegrateForces(PhysicsDirectBodyState state)
	{

	vars = (Vars)GetNode("/root/Vars");
	tester = (RayCast)GetNode("test");

	upDir = vars.car_pos;
	targetPosition = vars.car_pos;

	curDir = GlobalTransform.basis.Xform(Basis.Identity.z);
	targetDir = (targetPosition - GlobalTransform.origin).Normalized();
	rotationAngle = Mathf.Acos(curDir.x) - Mathf.Acos(targetDir.x);
	//state.AngularVelocity = upDir * (rotationAngle / state.Step);
	//LinearVelocity = (vars.car_pos - Transform.origin) / state.Step;
	//LinearVelocity = vars.LinVel / state.Step;


	GlobalTransform = new Transform(GlobalTransform.basis, vars.car_pos);
	//AddTorque(upDir * rotationAngle * 10F);
	//AddCentralForce((targetPosition - GlobalTransform.origin) * 100F);
	tester.CastTo = GlobalTransform.basis.z;

	} */
  public override void _PhysicsProcess(float delta)
  {
	/* 
	  if (vars.car_alt > (3f + vars.planet_radius))
	  {
	  target = new Transform(GlobalTransform.basis, vars.car_pos);
	  }
	  else
	  {
	  target = new Transform(GlobalTransform.basis, vars.car_pos.Normalized() * (3f + vars.planet_radius));
	  } */

	target.basis = GlobalTransform.basis;
	target.origin = vars.car_pos * 1.1f;

	LookAt(-GlobalTransform.origin, GlobalTransform.basis.x);

	//GlobalTransform = target.InterpolateWith(GlobalTransform, 0.5f);
	GlobalTransform = target;

  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }
}
