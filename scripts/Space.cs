using Godot;
using System;

public class Space : Spatial
{
  Vars vars;
  Vector3 rot;
  float fps;
  MeshInstance Atmosphere;
  float foggy = 0;
  Vector3 rotInc = new Vector3(0F, 0.0001F, 0F);
  //static WorldEnvironment worldEnvironment = GetNode<WorldEnvironment>("WorldEnvironment");
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";
  CanvasLayer canv;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	vars = (Vars)GetNode("/root/Vars");
	//var overlay = GD.Load("res://DebugOverlay.tscn");
	//canv = GetNode();
	//add_stat("boo", 2F);

	//AddChild(overlay);
	//overlay.add_stat("FPS")
	//Node car = GetNode<Node>("Car");
	VehicleBody carBody = (VehicleBody)GetNode("Car/CarBody");
	RigidBody leftWing = (RigidBody)GetNode("Car/LeftWing");
	RigidBody rightWing = (RigidBody)GetNode("Car/RightWing");
	Atmosphere = (MeshInstance)GetNode("/root/Space/Planet/Surface/Atmosphere");
	//DebugOverlay.stats.add_property(self, "fps", "")
	//DebugOverlay.stats.add_property(self, "sun_ang", "")
	//carBody.Transform.basis = myVars.car_basis;
	//carBody.Transform.basis = Basis.Identity * carBody.Transform.basis;
	//Basis carbasis = carBody.Transform.basis;
	//carbasis = Transform.basis.Rotated(carBody.Transform.basis.x,Mathf.Pi/4).Orthonormalized();
	carBody.Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
	leftWing.Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
	rightWing.Transform.basis.Rotated(Transform.basis.x, Mathf.Pi / 4).Orthonormalized();
  }
  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)
  {
	fps = Engine.GetFramesPerSecond();
  }
  public override void _PhysicsProcess(float delta)
  {
	WorldEnvironment worldEnvironment = (WorldEnvironment)GetNode("WorldEnvironment");
	DirectionalLight sun = (DirectionalLight)GetNode("WorldEnvironment/Sun");
	//StaticBody planet = (StaticBody)GetNode("Planet");

	rot = worldEnvironment.Environment.BackgroundSkyRotation;
	worldEnvironment.Environment.BackgroundSkyRotation = rot + rotInc;
	sun.RotateY(0.0001F);

	worldEnvironment.Environment.FogDepthBegin = Mathf.Min(vars.cam_alt, 26F);
	vars.sun_ang = vars.cam_pos.AngleTo(-Atmosphere.Transform.basis.z);
	//sun_ang = myVars.sun_ang;
  }
  // Debug overlay by Gonkee - full tutorial https://youtu.be/8Us2cteHbbo






}
