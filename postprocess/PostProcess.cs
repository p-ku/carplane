using Godot;
using System;
// https://www.reddit.com/r/godot/comments/tzx19b/wip_godot_3x_perobject_motion_blur_solution/

public class PostProcess : Node
{
  // CarCam cam;
  CarCam leadCam;

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

	MeshInstance velMesh = (MeshInstance)GetTree().Root.FindNode("VelocityMesh", true, false);
	staticBlur = (ColorRect)GetTree().Root.FindNode("StaticBlur", true, false);
	//staticBlur = (Sprite)GetTree().Root.FindNode("StaticBlur2", true, false);

	MeshInstance atmoMesh = (MeshInstance)GetTree().Root.FindNode("AtmoMesh", true, false);
	ViewportContainer atmoRect = (ViewportContainer)GetTree().Root.FindNode("TestContainer", true, false);
	atmoMat = atmoMesh.GetActiveMaterial(0) as ShaderMaterial;
	// atmoMat = atmoRect.Material as ShaderMaterial;

	ColorRect tiledVel = (ColorRect)GetTree().Root.FindNode("TiledVelocity", true, false);
	ColorRect neighborVel = (ColorRect)GetTree().Root.FindNode("NeighborVelocity", true, false);

	Viewport velView = (Viewport)GetTree().Root.FindNode("VelocityBuffer", true, false);
	Viewport tiledView = (Viewport)GetTree().Root.FindNode("TiledBuffer", true, false);
	Viewport neighborView = (Viewport)GetTree().Root.FindNode("NeighborBuffer", true, false);
	Viewport colView = (Viewport)GetTree().Root.FindNode("ColorBuffer", true, false);
	Viewport carView = (Viewport)GetTree().Root.FindNode("CarBuffer", true, false);
	ViewportContainer carContainer = (ViewportContainer)GetTree().Root.FindNode("CarContainer", true, false);

	//Image img = new Image();
	//img.Create((int)vars.renderRes.x, (int)vars.renderRes.y, false, Image.Format.Rgba8);
	//ImageTexture texture_n = new ImageTexture();
	//texture_n.CreateFromImage(img, 0);
	//staticBlur.Texture = texture_n;
	//staticBlur.Offset = Vector2.Zero;
	int blurTileSize = 40;
	carView.Size = vars.renderRes;
	colView.Size = vars.renderRes;
	velView.Size = vars.renderRes;
	tiledView.Size = velView.Size / blurTileSize;
	neighborView.Size = tiledView.Size;
	//carContainer.RectSize = vars.renderRes;

	j_prime_term = nn * phi / blurSteps;
	kk = 40 * blurSteps / 35;

	ViewportTexture carBuff = carView.GetTexture();
	ViewportTexture colBuff = colView.GetTexture();
	ViewportTexture velBuff = velView.GetTexture();
	ViewportTexture tiledBuff = tiledView.GetTexture();
	ViewportTexture variBuff = neighborView.GetTexture();
	ViewportTexture neighborBuff = neighborView.GetTexture();

	Vector2 halfResoSq = vars.renderRes * vars.renderRes * 0.25f;

	Vector2 halfUvDepthVec = new Vector2(Mathf.Tan(vars.FovHalfRad.x), Mathf.Tan(vars.FovHalfRad.y));

	Vector2 resDepthVec = 0.5f * velView.Size / halfUvDepthVec;
	Vector2 uvDepthVec = new Vector2(0.5f, 0.5f) / halfUvDepthVec;

	Vector2 invReso = Vector2.One / vars.renderRes;
	//Vector2 tileUV = new Vector2(blurTileSize, blurTileSize) / velView.Size;
	Vector2 tileUV = blurTileSize * invReso;
	// Initiate velocity pass.
	velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;
	velMat.SetShaderParam("res_depth_vec", resDepthVec);
	velMat.SetShaderParam("uv_depth_vec", uvDepthVec);
	velMat.SetShaderParam("tile_uv", tileUV);
	velMat.SetShaderParam("inv_reso", invReso);
	velMat.SetShaderParam("tile_size", blurTileSize);


	velMat.SetShaderParam("shutter_angle", blurAmount);

	velMat.SetShaderParam("reso", vars.renderRes);
	velMat.SetShaderParam("car_mask", carBuff);

	velCam.Visible = true;
	atmoMesh.Visible = true;

	// Initiate blur tile pass.
	Vector2 tileDimensions = velView.Size / blurTileSize;
	(tiledVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("inv_reso", invReso);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("tile_uv", tileUV);

	// Initiate blur neighbor pass.
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tiled_velocity", tiledBuff);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("dims", tileDimensions);
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tile_uv", tileUV);

	// Initiate atmosphere.
	atmoMat.SetShaderParam("velocity_buffer", velBuff);
	atmoMat.SetShaderParam("color_buffer", colBuff);
	atmoMat.SetShaderParam("plan_rad", vars.PlanetRadius);
	atmoMat.SetShaderParam("plan_rad_sq", vars.PlanetRadius * vars.PlanetRadius);

	atmoMat.SetShaderParam("atmo_height", vars.atmoHeight);
	atmoMat.SetShaderParam("atmo_rad", vars.AtmoRadius);
	atmoMat.SetShaderParam("atmo_rad_sq", vars.AtmoRadius * vars.AtmoRadius);
	float tanDist = vars.PlanetRadius * Mathf.Tan(Mathf.Acos(vars.PlanetRadius / vars.AtmoRadius));
	atmoMat.SetShaderParam("tangent_dist", tanDist);
	float cloudTanDist = vars.CloudRadius * Mathf.Tan(Mathf.Acos(vars.CloudRadius / vars.AtmoRadius));
	atmoMat.SetShaderParam("cloud_tangent_dist", vars.AtmoRadius * vars.AtmoRadius);
	atmoMat.SetShaderParam("dist_factor", vars.planet_radius * vars.AtmoRadius);

	// Create final image.
	(staticBlur.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("neighbor_buffer", neighborBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("color_buffer", colBuff);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tiled_buffer", tiledBuff);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("reso", vars.renderRes);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("tile_size", blurTileSize);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("long_dim", (int)longDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("short_dim", (int)shortDim);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("dim_check", dimCheck);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("inv_reso", invReso);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("tile_uv", tileUV);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("steps", blurSteps);

	(staticBlur.Material as ShaderMaterial).SetShaderParam("gamma", 1.5f);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("j_prime_term", j_prime_term);
	(staticBlur.Material as ShaderMaterial).SetShaderParam("kk", kk);


  }

  public override void _Process(float delta)
  {
	float top = Mathf.Tan(vars.FovHalfRad.y) * velCam.Near;

	float factor1 = (top * vars.aspectRatio) / velCam.Near;
	float f_denom = 2 * velCam.Far * velCam.Near;
	float factor2 = top / velCam.Near;
	float factor3 = (velCam.Near - velCam.Far) / f_denom;
	float factor4 = (velCam.Near + velCam.Far) / f_denom;

	atmoMat.SetShaderParam("f1", factor1);
	atmoMat.SetShaderParam("f2", factor2);
	atmoMat.SetShaderParam("f3", factor3);
	atmoMat.SetShaderParam("f4", factor4);
	// atmoMat.SetShaderParam("cam_pos", cam.GlobalTransform);
	processVelocity();

  }

  public override void _PhysicsProcess(float delta)
  {

	//   debugCam.Far = debugCam.GlobalTransform.origin.Length() + vars.planet_radius * 0.5f;
	//   debugCam.Near = Mathf.Tan(vars.FovHalfRad.y) * (debugCam.GlobalTransform.origin.Length() - vars.planet_radius - 4);

	velCam.GlobalTransform = leadCam.GlobalTransform;
	velCam.Far = leadCam.GlobalTransform.origin.Length() + vars.planet_radius * 0.5f;
	velCam.Near = Mathf.Tan(vars.FovHalfRad.y) * (leadCam.GlobalTransform.origin.Length() - vars.planet_radius - 4);

	colorCam.GlobalTransform = leadCam.GlobalTransform;
	colorCam.Far = velCam.Far;
	colorCam.Near = velCam.Near;

	//  debugCam.Near = 2;
	colorCam.Near = 2;
	velCam.Near = 2;
	//	atmoMat.SetShaderParam("cam_pos", leadCam.GlobalTransform);


	// debugCam.GlobalTransform = GlobalTransform;
  }

  internal void processVelocity()
  {

	//if (!check)
	//{
	camBlurAngle.x = leadCam.PrevGlobalTransform.basis.x.AngleTo(leadCam.GlobalTransform.basis.x);
	camBlurAngle.y = leadCam.PrevGlobalTransform.basis.y.AngleTo(leadCam.GlobalTransform.basis.y);
	camBlurAngle.z = leadCam.PrevGlobalTransform.basis.z.AngleTo(leadCam.GlobalTransform.basis.z);
	//}
	//  else { check = !check; }


	if (camBlurAngle.x > 1.57f | camBlurAngle.y > 1.57f | camBlurAngle.z > 1.57f)// | camBlurAngle.Length() > 1.57f)
	{
	  velMat.SetShaderParam("snap", true);
	  //  check = !check;
	}
	else velMat.SetShaderParam("snap", false);

	velMat.SetShaderParam("cam_prev_pos", -leadCam.ToLocal(leadCam.PrevGlobalTransform.origin));
	velMat.SetShaderParam("cam_prev_xform", leadCam.PrevGlobalTransform.basis.Inverse() * leadCam.GlobalTransform.basis);
  }
}
