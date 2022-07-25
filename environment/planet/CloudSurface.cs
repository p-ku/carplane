using Godot;
using System;

public class CloudSurface : MeshInstance
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";
  Car car;
  Transform newTransform;
  DirectionalLight sun;
  Globals vars;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    // car = (Car)GetTree().Root.FindNode("Car");
    // sun = (DirectionalLight)GetNode("../WorldEnvironment/Sun");
    vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
    car = (Car)GetTree().Root.FindNode("Car", true, false);
    sun = (DirectionalLight)GetTree().Root.FindNode("Sun", true, false);
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _PhysicsProcess(float delta)
  {
    float ang = Mathf.Asin(vars.PlanetRadius / 20f);
    float d = vars.PlanetRadius / Mathf.Tan(ang);
    //float circ =
    //float cRad = Mathf.Sqrt(400.+);
    //newTransform.origin = new Vector3(0f, 0f, -car.GlobalTransform.origin.z);
    //GlobalTransform = newTransform;
    //Rotation = sun.Rotation;
    GlobalTransform = car.GlobalTransform;
  }
}
