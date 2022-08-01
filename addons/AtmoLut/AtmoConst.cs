using Godot;
using System;

internal static class AtmoConst
{
  //public static readonly Vector3 betaRay0 = new Vector3(.0519673f, .121427f, .296453f);
  public static readonly double[] betaRay0 = { .0519673d, .121427d, .296453d };

  // public static readonly Vector3 betaMie0 = .21f * Vector3.One;
  // public static readonly double[] betaMie0 = { .21m, .21m, .21m };
  public static readonly double betaMie0 = .21d;

  public static readonly double g = .76d;
  public static readonly double phaseRayConst = 3d / (16d * Math.PI);
  public static readonly double phaseMieConst = phaseRayConst * 2d;
  public static readonly double intensity = 2d;
  // public static readonly double[] scaleHeight = { 8.5m, 1.2m };
  public static readonly double scaleRay = 8.5d;
  public static readonly double scaleMie = 1.2d;

}