using Godot;
using System;

[Tool]
public class AtmoLutGUI : Panel
{
  // public static readonly float PlanetRadius = 256f;
  Label planRadLabel, atmoHeightLabel, intensityLabel, qualityLabel;
  internal HSlider planRadSlide, atmoHeightSlide, intensitySlide, qualitySlide;
  SpinBox planRadSpin, atmoHeightSpin, intensitySpin, qualitySpin;
  Button buildButton;
  ProgressBar progBar;
  double intensity, planRad, planRadSq, atmoHeight, atmoRad, atmoRadSq;
  int quality;
  //public static readonly Vector3 betaRay0 = new Vector3(.0519673f, .121427f, .296453f);
  static readonly double[] betaR0 = { .0519673d, .121427d, .296453d };
  // static readonly Vector3 betaMie0 = .21f * Vector3.One;
  // static readonly double[] betaMie0 = { .21m, .21m, .21m };
  static readonly double betaM0 = .21d;
  static readonly double g = .76d;
  static readonly double gg = g * g;
  double tanDist;
  static readonly double phaseConstR = 3d / (16d * Math.PI);
  static readonly double phaseConstM = phaseConstR * 2d;
  // static readonly double[] scaleHeight = { 8.5m, 1.2m };
  static readonly double scaleR = 8.5d;
  static readonly double scaleM = 1.2d;
  public override void _Ready()
  {
	//   Label[] labels = { planRadLabel, atmoHeightLabel, intensityLabel, qualityLabel };
	//   foreach (Label label in labels)
	//   {
	//
	//   }
	//
	// GetNode("BuildButton").Connect("pressed", this, "BuildPressed");
	buildButton = (Button)GetTree().Root.FindNode("BuildButton", true, false);
	progBar = (ProgressBar)GetTree().Root.FindNode("ProgressBar", true, false);


	planRadSpin = (SpinBox)GetTree().Root.FindNode("PlanRadSpin", true, false);
	planRadSlide = (HSlider)GetTree().Root.FindNode("PlanRadSlide", true, false);
	planRadLabel = (Label)GetTree().Root.FindNode("PlanRadLabel", true, false);
	planRadSpin = (SpinBox)GetTree().Root.FindNode("PlanRadSpin", true, false);
	atmoHeightSlide = (HSlider)GetTree().Root.FindNode("AtmoHeightSlide", true, false);
	atmoHeightLabel = (Label)GetTree().Root.FindNode("AtmoHeightLabel", true, false);
	atmoHeightSpin = (SpinBox)GetTree().Root.FindNode("AtmoHeightSpin", true, false);
	intensityLabel = (Label)GetTree().Root.FindNode("IntensityLabel", true, false);
	intensitySpin = (SpinBox)GetTree().Root.FindNode("IntensitySpin", true, false);
	intensitySlide = (HSlider)GetTree().Root.FindNode("IntensitySlide", true, false);
	qualityLabel = (Label)GetTree().Root.FindNode("QualityLabel", true, false);
	qualitySpin = (SpinBox)GetTree().Root.FindNode("QualitySpin", true, false);
	qualitySlide = (HSlider)GetTree().Root.FindNode("QualitySlide", true, false);

	buildButton.Connect("pressed", this, "BuildPressed");
	planRadSlide.Connect("value_changed", this, "planRadChanged");
	planRadSpin.Connect("value_changed", this, "planRadChanged");
	atmoHeightSlide.Connect("value_changed", this, "atmoHeightChanged");
	atmoHeightSpin.Connect("value_changed", this, "atmoHeightChanged");
	intensitySlide.Connect("value_changed", this, "intensityChanged");
	intensitySpin.Connect("value_changed", this, "intensityChanged");
	qualitySlide.Connect("value_changed", this, "qualityChanged");
	qualitySpin.Connect("value_changed", this, "qualityChanged");

	planRad = planRadSpin.Value;
	atmoHeight = atmoHeightSpin.Value;
	intensity = intensitySpin.Value;
	quality = (int)qualitySpin.Value;
	planRadSq = planRadSpin.Value * planRadSpin.Value;
	atmoRad = planRad + atmoHeight;
	atmoRadSq = atmoRad * atmoRad;
	tanDist = planRad * Math.Tan(Math.Acos(planRad / atmoRad));

	// planRad = 26d;
	// atmoHeight = 100d;
	// intensity = 1d;
	// quality = 1;
	// planRadSq = planRad * planRad;
	// atmoRad = planRad + atmoHeight;
	// atmoRadSq = atmoRad * atmoRad;

	//GD.Print(planRadSq);

	// Vector3 boo = 2f * AtmoConst.betaMie0;
  }




  public void planRadChanged(float val)
  {
	planRadSpin.Value = val;
	planRadSlide.Value = val;
	planRad = val;
	planRadSq = val * val;

	atmoRad = planRad + atmoHeight;
	atmoRadSq = atmoRad * atmoRad;
  }
  public void atmoHeightChanged(float val)
  {
	atmoHeightSpin.Value = val;
	atmoHeightSlide.Value = val;
	atmoHeight = val;
	atmoRad = planRad + atmoHeight;
	atmoRadSq = atmoRad * atmoRad;
  }
  public void intensityChanged(float val)
  {
	intensitySpin.Value = val;
	intensitySlide.Value = val;
	intensity = val;
  }
  public void qualityChanged(float val)
  {
	qualitySpin.Value = val;
	qualitySlide.Value = val;
	quality = (int)val;
  }
  public void BuildPressed()
  {
	progBar.Value = 0d;
	progBar.Visible = true;
	GD.Print("Building . . .");

	//  double[,,] depthMie = new double[2, quality, quality];// = new double[quality, quality, quality];

	//(double, double, double)[,] densities = 
	march();
	//  lightStep(densities);
	GD.Print("Complete.");
	progBar.Visible = false;

  }
  // internal (double, double, double)[,] calculateDensity(double maxAngle, double maxDepth, double baseHeight, bool isLight = false, double bL = 0d, double dL = 0d)
  internal void march()
  {
	double[,] densR = new double[quality, quality];// = new double[quality, quality, quality];
	double[,] densM = new double[quality, quality];// = new double[quality, quality, quality];

	double[,] densLR = new double[quality, quality];// = new double[quality, quality, quality];
	double[,] densLM = new double[quality, quality];// = new double[quality, quality, quality];

	Image outImg = new Image();

	outImg.Create(2 * (quality + quality * quality), 2 * (quality + quality * quality), false, Image.Format.Rgbah);
	outImg.Lock();
	// double[] mu = new double[quality];
	//	double[,,][] color = new double[quality, quality, quality][];
	float[] color = new float[4];

	// double[][] dirNorm = new double[quality][];
	double[,] dirNorm = new double[quality, 2];
	double[,] dirVec = new double[quality, 2];
	double[] dirMag = new double[quality];

	//   double planetViewAngle = Math.Atan(planRad / atmoRad);
	//   int planetSteps = (int)Math.Round(quality * planetViewAngle / (Math.PI - planetViewAngle));

	double noPlanetAngle = Math.Atan(atmoRad / planRad);

	//  double stepAngle = planetViewAngle / (double)planetSteps;
	int forwardSteps = (int)Math.Round(quality * 0.5d * Math.PI / (0.5d * Math.PI + noPlanetAngle));
	double stepAngle = 0.5d * Math.PI / (double)forwardSteps;

	//  int forwardSteps = (int)Math.Round(quality * 0.5d * Math.PI / (Math.PI - planetViewAngle));
	//  double stepAngle = planetViewAngle / planetSteps;

	//    double lightDoubleSteps = quality * maxLightAngle / Math.PI;
	double[] sumDensR = new double[quality];
	double[] sumDensM = new double[quality];
	double[,] stepDensR = new double[quality, quality];
	double[,] stepDensM = new double[quality, quality];

	double[] sumDensLR = new double[quality];
	double[] sumDensLM = new double[quality];
	double[,] stepDensLR = new double[quality, quality];
	double[,] stepDensLM = new double[quality, quality];
	double[] lookAngle = new double[quality];

	double[,] posMag = new double[quality, quality];
	double[,] posAngle = new double[quality, quality];
	double[] fullDist = new double[quality];


	for (int dir = 0; dir < forwardSteps; dir++)
	{


	  // Starts looking down, ends at 90deg + tan dist. Relative to start position.
	  lookAngle[dir] = Math.PI - dir * stepAngle;

	  double lookSin = Math.Sin(lookAngle[dir]);
	  double lookCos = Math.Cos(lookAngle[dir]);

	  dirNorm[dir, 0] = lookSin;
	  dirNorm[dir, 1] = lookCos;
	  dirVec[dir, 0] = dirNorm[dir, 0];
	  dirVec[dir, 1] = atmoRad + dirNorm[dir, 1];
	  dirMag[dir] = Math.Sqrt((dirVec[dir, 0] * dirVec[dir, 0] + dirVec[dir, 1] * dirVec[dir, 1]));

	  //     double b = 2d * lookCos * atmoRad; // 2 * dot(dir,pos), pos being UP. dot(dir, (0,1))
	  double b = 2d * atmoRad * dirNorm[dir, 1]; // 2 * dot(dir,pos), pos being UP. dot(dir, (0,1))

	  double bb = b * b;
	  double d = bb;

	  double cp = atmoRadSq - planRadSq;
	  double dp = bb - 4d * cp;
	  double toSurface = (-b - Math.Sqrt(dp)) * 0.5d;

	  bool hitsPlanet = dp > 0d && toSurface > 0d;
	  fullDist[dir] = hitsPlanet ? toSurface : (-b + Math.Sqrt(d)) * .5d;

	  double step = fullDist[dir] / quality;
	  double halfStep = step * 0.5d;

	  for (int dep = 0; dep < quality; dep++)
	  {
		double curDist = fullDist[dir] * (dep + 1d) / quality;

		double midPoint = curDist - halfStep;

		double[] posVec = { lookSin * midPoint, atmoRad + lookCos * midPoint };
		posMag[dir, dep] = Math.Sqrt((posVec[0] * posVec[0] + posVec[1] * posVec[1]));

		posAngle[dir, dep] = Math.Atan(posVec[0] / posVec[1]);

		double height = posMag[dir, dep] - planRad;

		stepDensR[dir, dep] = Math.Exp((-height / AtmoConst.scaleRay)) * step;
		stepDensM[dir, dep] = Math.Exp((-height / AtmoConst.scaleMie)) * step;

		sumDensR[dir] += stepDensR[dir, dep];
		sumDensM[dir] += stepDensM[dir, dep];

		densR[dir, dep] = sumDensR[dir];
		densM[dir, dep] = sumDensM[dir];

		stepDensLR[dir, dep] = stepDensR[dir, dep];
		stepDensLM[dir, dep] = stepDensM[dir, dep];

		densLR[dir, dep] = densR[dir, dep];
		densLM[dir, dep] = densM[dir, dep];
	  }
	  sumDensLR[dir] = sumDensR[dir];
	  sumDensLM[dir] = sumDensM[dir];
	}
	// calculate reverse light depths.
	for (int dir = 0; dir < quality - forwardSteps - 1; dir++)
	{
	  for (int dep = 0; dep < quality; dep++)
	  {
		dirNorm[forwardSteps + dir, 0] = -dirNorm[forwardSteps - dir, 0];

		dirNorm[forwardSteps + dir, 0] = -dirNorm[forwardSteps - dir, 0];
		dirNorm[forwardSteps + dir, 1] = -dirNorm[forwardSteps - dir, 1];

		sumDensLR[forwardSteps + dir + 1] = sumDensLR[forwardSteps - dir - 1] - densLR[forwardSteps - dir - 1, dep];
		sumDensLM[forwardSteps + dir + 1] = sumDensLM[forwardSteps - dir - 1] - densLM[forwardSteps - dir - 1, dep];
	  }
	}


	for (int dir = 0; dir < forwardSteps; dir++)
	{
	  for (int dirL = 0; dirL < quality; dirL++)
	  {
		double[] mieConst = new double[quality];
		double[,] rayConst = new double[quality, 3];

		double[] mu = new double[quality];
		double[] muMu = new double[quality];
		double[] phaseMnum = new double[quality];
		double[] phaseMden = new double[quality];
		double[] phaseM = new double[quality];
		double[] phaseR = new double[quality];

		double[] finalR = new double[3];
		double[] finalM = new double[3];

		for (int lightRot = 0; lightRot < quality; lightRot++)
		{
		  double rotAngle = 2d * Math.PI * lightRot / quality;

		  mu[lightRot] = dirNorm[dir, 0] * dirNorm[dirL, 0] + dirNorm[dir, 1] * dirNorm[dirL, 1];
		  mu[lightRot] *= Math.Cos(rotAngle);
		  muMu[lightRot] = mu[lightRot] * mu[lightRot];
		  phaseMnum[lightRot] = phaseConstM * ((1d - gg) * (muMu[lightRot] + 1d));
		  phaseMden[lightRot] = Math.Pow(1d + gg - 2d * mu[lightRot] * g, 1.5d) * (2d + gg);
		  phaseM[lightRot] = phaseMnum[lightRot] / phaseMden[lightRot];
		  mieConst[lightRot] = phaseM[lightRot] * betaM0;
		  phaseR[lightRot] = phaseConstR + phaseConstR * muMu[lightRot];


		  //  rayConst[(int)lightRot][0] = 0d;
		  //    rayConst[lightRot] = new double[3];

		  for (int bas = 0; bas < 3; bas++)
		  {
			rayConst[lightRot, bas] = phaseR[lightRot] * betaR0[bas];
		  }
		}
		double[] attenRay = new double[3];
		double[] attenMie = new double[3];
		for (int dep = 0; dep < quality; dep++)
		{
		  double bL = 2d * posMag[dep, dir] * Math.Cos(lookAngle[dir] - posAngle[dep, dir]);
		  double cL = posMag[dep, dir] - atmoRadSq;
		  double dL = bL * bL - 4d * cL;
		  double lightDist = 0.5d * (-bL + Math.Sqrt(dL));
		  int depL = Math.Max((int)Math.Round(lightDist / fullDist[dirL]), quality - 1);


		  // finalR[dep, dir] = new double[3];
		  // finalM[dep, dir] = new double[3];

		  for (int bas = 0; bas < 3; bas++)
		  {
			attenRay[bas] += stepDensR[dir, dep] * Math.Exp(-(betaR0[bas] * (densR[dep, dir] + densLR[depL, dirL]) + 1.1 * betaM0 * (densM[dep, dir] + densLM[depL, dirL])));
			attenMie[bas] += stepDensM[dir, dep] * Math.Exp(-(betaR0[bas] * (densR[dep, dir] + densLR[depL, dirL]) + 1.1 * betaM0 * (densM[dep, dir] + densLM[depL, dirL])));

			//    finalR[dep, dir][bas] += attenRay[bas];
			//    finalM[dep, dir][bas] += attenMie[bas];
			finalR[bas] += attenRay[bas];
			finalM[bas] += attenMie[bas];
		  }
		  double[] blockage = new double[3];
		  for (int bas = 0; bas < 3; bas++)
		  {
			blockage[bas] = Math.Exp(-(betaR0[bas] * densR[dep, dir] + 1.1d * betaM0 * densM[dep, dir]));
		  }
		  for (int lightRot = 0; lightRot < quality; lightRot++)
		  {
			for (int bas = 0; bas < 3; bas++)
			{
			  color[bas] = (float)(rayConst[lightRot, bas] * finalR[bas] + mieConst[lightRot] * finalM[bas]);
			}
			color[3] = (float)(1d - Math.Sqrt(blockage[0] * blockage[0] + blockage[1] * blockage[1] + blockage[2] * blockage[2]));
			int pixelX = dir + dirL + dir * quality + dirL * quality;
			int pixelY = dep + depL + dep * quality + depL * quality;

			outImg.SetPixel(pixelX, pixelY, new Color(color[0], color[1], color[2], color[3]));




			GD.Print(color[0], color[1], color[2], color[3]);
		  }
		}
	  }
	}
	outImg.Unlock();
	outImg.SavePng("res://test.png");

	Image testImg = new Image();

	// testImg.Create(2 * (quality + quality * quality), 2 * (quality + quality * quality), false, Image.Format.Rgbah);
	//  Texture script = GD.Load<Texture>("res://addons/CustomNodes/");
	//    byte[] testTex = (byte[])GD.Load("res://neg-x-0-0-0.dat");
	byte[] testTex = System.IO.File.ReadAllBytes("./neg-x-0-0-0.dat");
	GD.Print(testTex);
	testImg.CreateFromData(256, 256, true, Image.Format.Rgbe9995, testTex);
	testImg.SavePng("res://testbrune.png");
  }
}
