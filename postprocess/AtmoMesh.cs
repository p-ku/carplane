using Godot;
using System;
// https://www.reddit.com/r/godot/comments/tzx19b/wip_godot_3x_perobject_motion_blur_solution/

public class AtmoMesh : MeshInstance
{
  // CarCam cam;
  CarCam leadCam;
  float tanDistAtmo;
  Camera velCam, colorCam, debugCam;
  ColorRect staticBlur;
  internal ShaderMaterial velMat;
  ShaderMaterial atmoMat, atmoRectMat;
  bool check = false;
  Vector3 camBlurAngle;
  Globals vars;
  // hello.
  [Export(PropertyHint.Range, "0.013,1")]
  float blurAmount = 0.5f;
  [Export(PropertyHint.Range, "3,35")]
  uint blurSteps = 7;
  [Export(PropertyHint.Range, "1,80")]
  float maxBlurRadius = 7f;
  [Export(PropertyHint.Range, "0.1,1")]
  float atmoQuality = 0.2f;
  static float nn = 0.95f, gamma = 1.5f, phi = 27;
  float j_prime_term;
  float kk;
  uint shortDim;
  uint longDim;
  Vector2 dimCheck;
  public override void _Ready()
  {

	vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
	//   cam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);
	velCam = (Camera)GetTree().Root.FindNode("VelocityCam", true, false);
	leadCam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);

	colorCam = (Camera)GetTree().Root.FindNode("ColorCam", true, false);
	debugCam = (Camera)GetTree().Root.FindNode("DebugCam", true, false);

	if (vars.renderRes.x >= vars.renderRes.y)
	{
	  longDim = (uint)vars.renderRes.x;
	  shortDim = (uint)vars.renderRes.y;
	  dimCheck = new Vector2(1f, 0f);
	}
	else
	{
	  longDim = (uint)vars.renderRes.y;
	  shortDim = (uint)vars.renderRes.x;
	  dimCheck = new Vector2(0f, 1f);
	}


	MeshInstance atmoMesh = (MeshInstance)GetTree().Root.FindNode("AtmoMesh", true, false);
	ViewportContainer atmoRect = (ViewportContainer)GetTree().Root.FindNode("TestContainer", true, false);
	atmoMat = atmoMesh.GetActiveMaterial(0) as ShaderMaterial;
	// atmoMat = atmoRect.Material as ShaderMaterial;

	ColorRect tiledVel = (ColorRect)GetTree().Root.FindNode("TiledVelocity", true, false);
	ColorRect neighborVel = (ColorRect)GetTree().Root.FindNode("NeighborVelocity", true, false);

	Viewport velView = (Viewport)GetTree().Root.FindNode("VelocityBuffer", true, false);
	Viewport colView = (Viewport)GetTree().Root.FindNode("ColorBuffer", true, false);
	Viewport noBlurView = (Viewport)GetTree().Root.FindNode("NoBlurBuffer", true, false);


	Vector2 halfReso = vars.renderRes * 0.5f;

	Vector2 halfUvDepthVec = new Vector2(Mathf.Tan(vars.FovHalfRad.x), Mathf.Tan(vars.FovHalfRad.y));

	float resDepthVec = 0.5f * velView.Size.y / halfUvDepthVec.y;
	Vector2 uvDepthVec = 0.5f * Vector2.One / halfUvDepthVec;

	// atmoMesh.Visible = true;

	// Initiate atmosphere.
	atmoMat.SetShaderParam("velocity_buffer", velView.GetTexture());
	atmoMat.SetShaderParam("color_buffer", colView.GetTexture());
	atmoMat.SetShaderParam("plan_rad", vars.PlanetRadius);
	atmoMat.SetShaderParam("plan_rad_sq", vars.PlanetRadius * vars.PlanetRadius);

	atmoMat.SetShaderParam("atmo_height", vars.atmoHeight);
	atmoMat.SetShaderParam("atmo_rad", vars.AtmoRadius);
	atmoMat.SetShaderParam("atmo_rad_sq", vars.AtmoRadius * vars.AtmoRadius);
	tanDistAtmo = vars.PlanetRadius * Mathf.Tan(Mathf.Acos(vars.PlanetRadius / vars.AtmoRadius));
	//  atmoMat.SetShaderParam("tangent_dist", tanDist);
	// float cloudTanDist = vars.CloudRadius * Mathf.Tan(Mathf.Acos(vars.CloudRadius / vars.AtmoRadius));
	// atmoMat.SetShaderParam("cloud_tangent_dist", vars.AtmoRadius * vars.AtmoRadius);
	atmoMat.SetShaderParam("dist_factor", vars.PlanetRadius * vars.AtmoRadius);
	atmoMat.SetShaderParam("plan_res_depth_vec", resDepthVec * vars.PlanetRadius);
	atmoMat.SetShaderParam("half_vert_reso", halfReso.y);
	atmoMat.SetShaderParam("atmo_height_sq", vars.atmoHeight * vars.atmoHeight);
	atmoMat.SetShaderParam("no_blur_mask", noBlurView.GetTexture());

	atmoMat.SetShaderParam("quality", atmoQuality);

	// Lens flare.
	atmoMat.SetShaderParam("velocity_buffer", velView.GetTexture());
	atmoMat.SetShaderParam("uv_depth_vec", 0.5f / halfUvDepthVec.y);
	atmoMat.SetShaderParam("reso", vars.renderRes);

	//	Vector2 sunSize; sunSize.x = 2f * Mathf.Pi / FovDeg.x; sunSize.y = 2f * Mathf.Pi / FovDeg.y;
	float sunSize = (2f * Mathf.Pi * Vector2.One / vars.FovDeg).Length();
	atmoMat.SetShaderParam("sun_size", sunSize);
  }

  public override void _Process(float delta)
  {
	//   float top = Mathf.Tan(vars.FovHalfRad.y) * velCam.Near;
	//
	//   float factor1 = (top * vars.aspectRatio) / velCam.Near;
	//   float f_denom = 2 * velCam.Far * velCam.Near;
	//   float factor2 = top / velCam.Near;
	//   float factor3 = (velCam.Near - velCam.Far) / f_denom;
	//   float factor4 = (velCam.Near + velCam.Far) / f_denom;
	//
	//   atmoMat.SetShaderParam("f1", factor1);
	//   atmoMat.SetShaderParam("f2", factor2);
	//   atmoMat.SetShaderParam("f3", factor3);
	//   atmoMat.SetShaderParam("f4", factor4);
	Vector3 sunDir = Vector3.Back * 500f + leadCam.GlobalTransform.origin;
	//  lensFlare.Visible = !leadCam.IsPositionBehind(sunDir);
	//  if (lensFlare.Visible)
	//  {
	Vector2 unprojSunDir = leadCam.UnprojectPosition(sunDir);
	atmoMat.SetShaderParam("sun_frag", unprojSunDir);
	// GD.Print(unprojSunDir);
	//   }


	float tanDist = vars.PlanetRadius * Mathf.Tan(Mathf.Acos(vars.PlanetRadius / leadCam.Altitude));
	tanDist = Mathf.Max(tanDistAtmo, tanDist);
	atmoMat.SetShaderParam("tangent_dist", tanDist);
	atmoMat.SetShaderParam("cam_dist", leadCam.OriginLength);
	atmoMat.SetShaderParam("cam_alt", leadCam.OriginLength - vars.PlanetRadius);
  }
}
