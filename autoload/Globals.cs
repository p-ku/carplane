using Godot;
using System;

public class Globals : Node
{
  internal float lVert, lHori, debugFloat, fps, Clift, liftAng;
  internal float cam_alt, MaxBlurAngleRad, MaxBlurAngleDeg;
  internal float AtmoRadius = 100f, PlanetRadius = 26f, CloudRadius = 31f, atmoHeight;
  internal float sun_ang, DragMag, AngDamp, aspectRatio;
  internal Vector2 renderRes = new Vector2(1280, 720), displayRes, FovRad, FovDeg, FovHalfDeg, FovHalfRad;
  internal Vector3 LinVel, cam_pos, LocLinVel, Lift;

  public override void _Ready()
  {
    Camera cam = (Camera)GetTree().Root.FindNode("CarCam", true, false);
    // Control disp = (Control)GetTree().Root.FindNode("Display", true, false);
    // disp.RectSize = renderRes;
    // Convert field of view to radians and calculate horizontal FOV.
    displayRes = GetViewport().Size;
    //RectSize = vars.renderRes;
    FovDeg.y = 70f;
    FovRad.y = Mathf.Deg2Rad(FovDeg.y);
    FovRad.x = 2f * Mathf.Atan(renderRes.x * Mathf.Tan(FovRad.y * 0.5f) / renderRes.y);
    FovDeg.x = Mathf.Rad2Deg(FovRad.x);
    FovHalfDeg = FovDeg * 0.5f;
    FovHalfRad = FovRad * 0.5f;
    MaxBlurAngleRad = FovHalfRad.x;
    MaxBlurAngleDeg = FovHalfDeg.x;

    aspectRatio = renderRes.x / renderRes.y;
    atmoHeight = AtmoRadius - PlanetRadius;
    GD.Randomize();
  }
  /*   float lerp(float firstFloat, float secondFloat, float by)
	{ return firstFloat * (1F - by) + secondFloat * by; }
	Vector3 Lerp(Vector3 firstVector, Vector3 secondVector, float by)
	{
	  float retX = lerp(firstVector.x, secondVector.x, by);
	  float retY = lerp(firstVector.y, secondVector.y, by);
	  float retZ = lerp(firstVector.z, secondVector.z, by);
	  return new Vector3(retX, retY, retZ);
	} */
  public override void _Input(InputEvent @event)
  {
    if ((Input.IsPhysicalKeyPressed(65)) & (Input.IsPhysicalKeyPressed(68)))
    {
      lHori = 0;
    }
    else if (Input.IsPhysicalKeyPressed(65))
    {
      lHori = -1;
    }
    else if (Input.IsPhysicalKeyPressed(68))
    {
      lHori = 1;
    }
    else
    {
      lHori = Input.GetAxis("turn_left", "turn_right");
    }
    if ((Input.IsPhysicalKeyPressed(83)) & (Input.IsPhysicalKeyPressed(87)))
    {
      lVert = 0;
    }
    else if (Input.IsPhysicalKeyPressed(83))
    {
      lVert = -1;
    }
    else if (Input.IsPhysicalKeyPressed(87))
    {
      lVert = 1;
    }
    else
    {
      lVert = Input.GetAxis("pitch_up", "pitch_down");

    }

  }
}
