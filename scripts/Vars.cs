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
  public float car_alt = 0F;
  public Vector3 car_pos;
  public Vector3 car_norm;
  public Basis car_basis;
  public float roll_val;
  public float pitch_val;
  public float sun_ang = 0F;
  public float spin = 0.0001F;
}