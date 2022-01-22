using Godot;
using System;

public class Vars : Node
{
  public bool flying;
  public Vector3 sun_pos = new Vector3(1F, 0F, 0F);
  public float planet_radius = 26F;
  public float atmo_radius = 100F;
  public Vector3 cam_pos;
  public float cam_alt;
  public float cam_dist;
  public Basis cam_basis;
  public float car_alt;
  public Vector3 car_pos;
  public Vector3 car_norm;
  public Basis car_basis;
  public float roll_val;
  public float pitch_val;
  public float sun_ang;
  public float spin = 0.0001F;
  public float RVert;
  public float RHori;
  public float LVert;
  public float LHori;
  public override void _Input(InputEvent @event)
  {
    if ((Input.IsPhysicalKeyPressed(65)) & (Input.IsPhysicalKeyPressed(68)))
    {
      LHori = 0;
    }
    else if (Input.IsPhysicalKeyPressed(65))
    {
      LHori = -1;
    }
    else if (Input.IsPhysicalKeyPressed(68))
    {
      LHori = 1;
    }
    else
    {
      LHori = Input.GetAxis("turn_left", "turn_right");
    }
    if ((Input.IsPhysicalKeyPressed(83)) & (Input.IsPhysicalKeyPressed(87)))
    {
      LVert = 0;
    }
    else if (Input.IsPhysicalKeyPressed(83))
    {
      LVert = -1;
    }
    else if (Input.IsPhysicalKeyPressed(87))
    {
      LVert = 1;
    }
    else
    {
      LVert = Input.GetAxis("pitch_up", "pitch_down");
    }
  }
}



