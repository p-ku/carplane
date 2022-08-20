using Godot;
using System;
// https://www.reddit.com/r/godot/comments/tzx19b/wip_godot_3x_perobject_motion_blur_solution/

public class MotionBlur : ColorRect
{
  Camera leadCam, velCam, colorCam, noBlurCam;
  Camera[] camGroup;
  static uint layer20 = 524288, allLayers = 1048575;
  ShaderMaterial velMat;
  Vector3 blurAngle;
  [Export(PropertyHint.Range, "0.013,1")]
  float blurAmount = 0.5f;
  [Export(PropertyHint.Range, "3,35")]
  uint blurSteps = 19;
  [Export(PropertyHint.Range, "1,3")]
  uint blurRadiusFactor = 2;

  [Export(PropertyHint.Layers3dRender)]
  uint blurLayers = allLayers;
  Transform PrevGlobalTransform = Transform.Identity;
  Vector2 FovRad, FovHalfRad;
  ColorRect lensFlare;

  public override void _Ready()
  {
	Vector2 renderRes = GetTree().Root.GetViewport().Size;
	leadCam = (Camera)GetParent();
	velCam = (Camera)GetNode("VelocityBuffer/VelocityCam");
	colorCam = (Camera)GetNode("ColorBuffer/ColorCam");
	noBlurCam = (Camera)GetNode("NoBlurBuffer/NoBlurCam");

	FovRad.y = Mathf.Deg2Rad(leadCam.Fov);
	FovRad.x = 2f * Mathf.Atan(renderRes.x * Mathf.Tan(FovRad.y * 0.5f) / renderRes.y);
	FovHalfRad = FovRad * 0.5f;
	leadCam.Current = false;
	MeshInstance velMesh = (MeshInstance)GetNode("VelocityBuffer/VelocityCam/VelocityMesh");

	ColorRect tiledVel = (ColorRect)GetNode("TiledBuffer/TiledVelocity");
	ColorRect neighborVel = (ColorRect)GetNode("NeighborBuffer/NeighborVelocity");
	lensFlare = (ColorRect)GetNode("LensFlare");

	Viewport velView = (Viewport)GetNode("VelocityBuffer");
	Viewport tiledView = (Viewport)GetNode("TiledBuffer");
	Viewport neighborView = (Viewport)GetNode("NeighborBuffer");
	Viewport colView = (Viewport)GetNode("ColorBuffer");
	Viewport noBlurView = (Viewport)GetNode("NoBlurBuffer");

	int blurTileSize = 20 * Mathf.RoundToInt(Mathf.Pow(2f, blurRadiusFactor - 1));

	colView.Size = renderRes;
	velView.Size = renderRes;
	tiledView.Size = velView.Size / blurTileSize;
	neighborView.Size = tiledView.Size;

	float j_prime_term = 0.95f * 27 / blurSteps;

	Vector2 halfUvDepthVec = new Vector2(Mathf.Tan(FovHalfRad.x), Mathf.Tan(FovHalfRad.y));

	Vector2 invReso = Vector2.One / renderRes;
	Vector2 tileUvSize = blurTileSize * invReso;
	GD.Print(invReso);
	GD.Print(tileUvSize);

	// Initiate velocity pass.
	velMat = velMesh.GetActiveMaterial(0) as ShaderMaterial;

	velMat.SetShaderParam("res_depth_vec", 0.5f * velView.Size.y / halfUvDepthVec.y);
	velMat.SetShaderParam("tile_pixel_size", blurTileSize);
	velMat.SetShaderParam("shutter_angle", blurAmount);
	velMat.SetShaderParam("half_reso", renderRes * 0.5f);

	// Initiate blur tile pass.
	(tiledVel.Material as ShaderMaterial).SetShaderParam("velocity_buffer", velView.GetTexture());
	(tiledVel.Material as ShaderMaterial).SetShaderParam("inv_reso", invReso);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("half_inv_reso", invReso / 2);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("tile_uv_size", tileUvSize);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("half_tile_uv_size_x", tileUvSize.x / 2);
	(tiledVel.Material as ShaderMaterial).SetShaderParam("start_half", 0);

	// Initiate blur neighbor pass.
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tiled_velocity", tiledView.GetTexture());
	(neighborVel.Material as ShaderMaterial).SetShaderParam("tile_uv_size", tileUvSize);

	// Noise for blur.
	OpenSimplexNoise blurNoise = new OpenSimplexNoise();
	blurNoise.Octaves = 1;
	blurNoise.Period = 1f;

	NoiseTexture noiseTex = new NoiseTexture();
	noiseTex.Width = (int)renderRes.x;
	noiseTex.Height = (int)renderRes.y;
	noiseTex.Noise = blurNoise;

	// Create final image.
	ShaderMaterial blurMat = Material as ShaderMaterial;

	blurMat.SetShaderParam("noise", noiseTex);
	blurMat.SetShaderParam("velocity_buffer", velView.GetTexture());
	blurMat.SetShaderParam("neighbor_buffer", neighborView.GetTexture());
	blurMat.SetShaderParam("color_buffer", colView.GetTexture());
	blurMat.SetShaderParam("reso", renderRes);
	blurMat.SetShaderParam("tile_pixel_size", blurTileSize);
	blurMat.SetShaderParam("steps", blurSteps);
	blurMat.SetShaderParam("j_prime_term", j_prime_term);



	// The velocity mesh resides on layer20;
	colorCam.CullMask = allLayers - layer20;
	velCam.CullMask = leadCam.CullMask;

	uint noBlurCull = allLayers - blurLayers;

	//	if (blurLayers != allLayers)
	//	  velMat.SetShaderParam("all_blur", false);
	//	else
	//	  velMat.SetShaderParam("all_blur", true);

	noBlurView.Size = renderRes;
	noBlurCam.CullMask = noBlurCull < layer20 ? noBlurCull : noBlurCull - layer20;
	velCam.CullMask = leadCam.CullMask < layer20 ? velCam.CullMask + layer20 : velCam.CullMask;

	velMat.SetShaderParam("no_blur_mask", noBlurView.GetTexture());
	camGroup = new Camera[3];
	camGroup[0] = colorCam;
	camGroup[1] = velCam;
	camGroup[2] = noBlurCam;

	foreach (Camera cam in camGroup)
	{
	  cam.DopplerTracking = Camera.DopplerTrackingEnum.Disabled;
	  cam.Projection = leadCam.Projection;
	  cam.Near = leadCam.Near;
	  cam.Far = leadCam.Far;
	  cam.Fov = leadCam.Fov;
	}

	if (Visible == true)
	  velCam.Visible = true;
  }

  public override void _Process(float delta)
  {


	foreach (Camera cam in camGroup)
	  cam.GlobalTransform = leadCam.GlobalTransform;

	blurAngle.x = PrevGlobalTransform.basis.x.AngleTo(leadCam.GlobalTransform.basis.x);
	blurAngle.y = PrevGlobalTransform.basis.y.AngleTo(leadCam.GlobalTransform.basis.y);
	blurAngle.z = PrevGlobalTransform.basis.z.AngleTo(leadCam.GlobalTransform.basis.z);

	if (blurAngle.x > 1.57f | blurAngle.y > 1.57f | blurAngle.z > 1.57f)
	{
	  velMat.SetShaderParam("snap", true);
	}
	else velMat.SetShaderParam("snap", false);

	velMat.SetShaderParam("cam_prev_pos", -leadCam.ToLocal(PrevGlobalTransform.origin));
	velMat.SetShaderParam("cam_prev_xform", leadCam.GlobalTransform.basis.Inverse() * PrevGlobalTransform.basis);

	PrevGlobalTransform = leadCam.GlobalTransform;
  }
}
