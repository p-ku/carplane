using Godot;
using System;
public class DebugCam : Camera
{
  Globals vars;
  public override void _Ready()
  {
	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
  }
  public override void _Process(float delta)
  {
	Far = GlobalTransform.origin.Length() + vars.PlanetRadius * 0.5f;
	Near = Mathf.Tan(vars.FovHalfRad.y) * (GlobalTransform.origin.Length() - vars.PlanetRadius - 4);
	if (Input.IsKeyPressed(87))
	  Translation -= new Vector3(0, 0, 0.1f);
	if (Input.IsKeyPressed(83))
	  Translation += new Vector3(0, 0, 0.1f);
	if (Input.IsKeyPressed(65))
	  Translation += new Vector3(0.1f, 0, 0);
	if (Input.IsKeyPressed(68))
	  Translation -= new Vector3(0.1f, 0, 0);
  }
}
