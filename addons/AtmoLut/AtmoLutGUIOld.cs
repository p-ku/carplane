//using Godot;
//using System;
//
//[Tool]
//public class AtmoLutGUI : Panel
//{
//  // public static readonly float PlanetRadius = 256f;
//  Label planRadLabel, atmoHeightLabel, intensityLabel, qualityLabel;
//  internal HSlider planRadSlide, atmoHeightSlide, intensitySlide, qualitySlide;
//  SpinBox planRadSpin, atmoHeightSpin, intensitySpin, qualitySpin;
//  Button buildButton;
//  ProgressBar progBar;
//  double intensity, planRad, planRadSq, atmoHeight, atmoRad, atmoRadSq;
//  int quality;
//  //public static readonly Vector3 betaRay0 = new Vector3(.0519673f, .121427f, .296453f);
//  static readonly double[] betaR0 = { .0519673d, .121427d, .296453d };
//  // static readonly Vector3 betaMie0 = .21f * Vector3.One;
//  // static readonly double[] betaMie0 = { .21m, .21m, .21m };
//  static readonly double betaM0 = .21d;
//  static readonly double g = .76d;
//  static readonly double gg = g * g;
//
//  static readonly double phaseConstR = 3d / (16d * Math.PI);
//  static readonly double phaseConstM = phaseConstR * 2d;
//  // static readonly double[] scaleHeight = { 8.5m, 1.2m };
//  static readonly double scaleR = 8.5d;
//  static readonly double scaleM = 1.2d;
//  public override void _Ready()
//  {
//    //   Label[] labels = { planRadLabel, atmoHeightLabel, intensityLabel, qualityLabel };
//    //   foreach (Label label in labels)
//    //   {
//    //
//    //   }
//    //
//    // GetNode("BuildButton").Connect("pressed", this, "BuildPressed");
//    buildButton = (Button)GetTree().Root.FindNode("BuildButton", true, false);
//    progBar = (ProgressBar)GetTree().Root.FindNode("ProgressBar", true, false);
//
//
//    planRadSpin = (SpinBox)GetTree().Root.FindNode("PlanRadSpin", true, false);
//    planRadSlide = (HSlider)GetTree().Root.FindNode("PlanRadSlide", true, false);
//    planRadLabel = (Label)GetTree().Root.FindNode("PlanRadLabel", true, false);
//    planRadSpin = (SpinBox)GetTree().Root.FindNode("PlanRadSpin", true, false);
//    atmoHeightSlide = (HSlider)GetTree().Root.FindNode("AtmoHeightSlide", true, false);
//    atmoHeightLabel = (Label)GetTree().Root.FindNode("AtmoHeightLabel", true, false);
//    atmoHeightSpin = (SpinBox)GetTree().Root.FindNode("AtmoHeightSpin", true, false);
//    intensityLabel = (Label)GetTree().Root.FindNode("IntensityLabel", true, false);
//    intensitySpin = (SpinBox)GetTree().Root.FindNode("IntensitySpin", true, false);
//    intensitySlide = (HSlider)GetTree().Root.FindNode("IntensitySlide", true, false);
//    qualityLabel = (Label)GetTree().Root.FindNode("QualityLabel", true, false);
//    qualitySpin = (SpinBox)GetTree().Root.FindNode("QualitySpin", true, false);
//    qualitySlide = (HSlider)GetTree().Root.FindNode("QualitySlide", true, false);
//
//    buildButton.Connect("pressed", this, "BuildPressed");
//    planRadSlide.Connect("value_changed", this, "planRadChanged");
//    planRadSpin.Connect("value_changed", this, "planRadChanged");
//    atmoHeightSlide.Connect("value_changed", this, "atmoHeightChanged");
//    atmoHeightSpin.Connect("value_changed", this, "atmoHeightChanged");
//    intensitySlide.Connect("value_changed", this, "intensityChanged");
//    intensitySpin.Connect("value_changed", this, "intensityChanged");
//    qualitySlide.Connect("value_changed", this, "qualityChanged");
//    qualitySpin.Connect("value_changed", this, "qualityChanged");
//
//    planRad = planRadSpin.Value;
//    atmoHeight = atmoHeightSpin.Value;
//    intensity = intensitySpin.Value;
//    quality = (int)qualitySpin.Value;
//    planRadSq = planRadSpin.Value * planRadSpin.Value;
//    atmoRad = planRad + atmoHeight;
//    atmoRadSq = atmoRad * atmoRad;
//
//
//    // planRad = 26d;
//    // atmoHeight = 100d;
//    // intensity = 1d;
//    // quality = 1;
//    // planRadSq = planRad * planRad;
//    // atmoRad = planRad + atmoHeight;
//    // atmoRadSq = atmoRad * atmoRad;
//
//    //GD.Print(planRadSq);
//
//    // Vector3 boo = 2f * AtmoConst.betaMie0;
//  }
//
//
//
//
//  public void planRadChanged(float val)
//  {
//    planRadSpin.Value = val;
//    planRadSlide.Value = val;
//    planRad = val;
//    planRadSq = val * val;
//
//    atmoRad = planRad + atmoHeight;
//    atmoRadSq = atmoRad * atmoRad;
//  }
//  public void atmoHeightChanged(float val)
//  {
//    atmoHeightSpin.Value = val;
//    atmoHeightSlide.Value = val;
//    atmoHeight = val;
//    atmoRad = planRad + atmoHeight;
//    atmoRadSq = atmoRad * atmoRad;
//  }
//  public void intensityChanged(float val)
//  {
//    intensitySpin.Value = val;
//    intensitySlide.Value = val;
//    intensity = val;
//  }
//  public void qualityChanged(float val)
//  {
//    qualitySpin.Value = val;
//    qualitySlide.Value = val;
//    quality = (int)val;
//  }
//  public void BuildPressed()
//  {
//    progBar.Value = 0d;
//    progBar.Visible = true;
//    GD.Print("Building . . .");
//
//    //  double[,,] depthMie = new double[2, quality, quality];// = new double[quality, quality, quality];
//
//    //(double, double, double)[,] densities = 
//    march(atmoRad);
//    //  lightStep(densities);
//    GD.Print("Complete.");
//    progBar.Visible = false;
//
//  }
//  // internal (double, double, double)[,] calculateDensity(double maxAngle, double maxDepth, double baseHeight, bool isLight = false, double bL = 0d, double dL = 0d)
//  internal double[,][] march(
//   double originMag, double maxAngle = Math.PI * 0.5d, bool isLight = false, double b0 = 0d, double d0 = 0d)
//
//  {// int[][,] jaggedArray4 = new int[3][,]
//   // double[,,] densities = new double[2, quality, quality];// = new double[quality, quality, quality];
//    double[,][] dens = new double[quality, quality][];// = new double[quality, quality, quality];
//    double[,][] densL = new double[quality, quality][];// = new double[quality, quality, quality];
//    Image outImg = new Image();
//
//    outImg.Create(quality * quality, quality * quality, false, Image.Format.Rgbah);
//    outImg.Lock();
//    // double[] mu = new double[quality];
//    //	double[,,][] color = new double[quality, quality, quality][];
//    float[] color = new float[4];
//
//    // double[][] dirNorm = new double[quality][];
//    double[] dirNorm = { 0d, 0d };
//
//    for (int dirStep = 0; dirStep < quality; dirStep++)
//    {
//      // Starts from atmosphere tangent, ends looking down. Relative to start position.
//      double lookAngle = Math.PI - maxAngle * (quality - dirStep) / quality;
//
//      // double lookAngle = 0.5d * Math.PI + 0.5d * Math.PI * (dirStep + 1d) / quality;
//      double lookSin = Math.Sin(lookAngle), lookCos = Math.Cos(lookAngle);
//      //   dirNorm[(int)dirStep] = { lookSin, lookCos };
//      // dirNorm[(int)dirStep][0] = lookSin;
//      // dirNorm[(int)dirStep][1] = lookCos;
//      dirNorm[0] = lookSin;
//      dirNorm[1] = lookCos;
//      //  double[] lookDir = { lookCos, lookSin };
//      //  double[] startPos = { 0m, atmoRad };
//
//
//
//
//      //   double[] thru = { (-b - (double)Math.Sqrt((double)d)) * .5m, (-b + (double)Math.Sqrt((double)d)) * .5m };
//      //   double[] rayLength = { Math.Max(thru[0], 0m), hitsPlanet ? toSurface : thru[1] };
//
//      double fullDist;
//
//
//      if (isLight)
//        fullDist = (-b0 + Math.Sqrt(d0)) * .5d;
//      else
//      {
//        double b = 2d * lookCos; // 2 * dot(dir,pos), pos being UP. dot(dir, (0,1))
//                                 // double c = 0m;
//        double bb = b * b;
//        double d = bb;
//
//        double cp = atmoRadSq - planRadSq;
//        double dp = bb - 4d * cp;
//        double toSurface = -b - Math.Sqrt(dp) * 0.5d;
//        //   if (dp < 0m)
//        bool hitsPlanet = dp > 0d && toSurface > 0d;
//        fullDist = hitsPlanet ? toSurface : (-b + Math.Sqrt(d)) * .5d;
//      }
//      double step = fullDist / quality;
//      double halfStep = step * 0.5d;
//      // double depth = depStep / quality;
//      //  if (rayLength[0] < rayLength[1])
//
//      //   double fullPath = rayLength[1] - rayLength[0];
//      //   double rayPos = rayLength[0];
//
//      //  double fullPath = rayLength[1] - rayLength[0];
//
//
//      // double[] opt = { 0m, 0m };
//      double sumDensR = 0d, sumDensM = 0d;
//      //   for (double depStep = quality; depStep > -0.5d; depStep--)
//      //   {
//      double[,][] finalR = new double[quality, quality][];
//      double[,][] finalM = new double[quality, quality][];
//
//      for (int depStep = 0; depStep < quality; depStep++)
//      {
//        //  double rayPos = rayLength * (quality - depStep - 1d) / quality;
//        double curDist = fullDist * (depStep + 1d) / quality;
//
//        double midPoint = curDist - halfStep;
//
//        double[] posVec = { lookSin * midPoint, originMag + lookCos * midPoint };
//
//        double posMag = Math.Sqrt((posVec[0] * posVec[0] + posVec[1] * posVec[1]));
//        double height = posMag - planRad;
//
//        double stepDensR = Math.Exp((-height / AtmoConst.scaleRay)) * step;
//        double stepDensM = Math.Exp((-height / AtmoConst.scaleMie)) * step;
//
//        sumDensR += stepDensR;
//        sumDensM += stepDensM;
//        // depthRay[(int)dirStep, (int)depStep] = optRay;
//        // depthMie[(int)dirStep, (int)depStep] = optMie;
//        //  densities[0, (int)dirStep, (int)depStep] = optRay;
//        //  densities[1, (int)dirStep, (int)depStep] = optMie;
//        dens[dirStep, depStep] = new double[4];
//
//        dens[dirStep, depStep][0] = sumDensR;
//        dens[dirStep, depStep][1] = sumDensM;
//        // dens[(int)dirStep, (int)depStep, 0][2] = dirNorm[(int)dirStep][0];
//        // dens[(int)dirStep, (int)depStep, 0][3] = dirNorm[(int)dirStep][1];
//        dens[dirStep, depStep][2] = dirNorm[0];
//        dens[dirStep, depStep][3] = dirNorm[1];
//
//        if (!isLight)
//        {
//          double posAngle = Math.Atan(posVec[0] / posVec[1]);
//          double bL = 2d * posMag * Math.Cos(lookAngle - posAngle);
//          double cL = posMag - atmoRadSq;
//          double dL = bL * bL - 4d * cL;
//          // double height = densLook.Item3;
//          double maxLightAngle = Math.PI - Math.Atan(planRad / (planRad + height));
//          densL[dirStep, depStep] = new double[4];
//
//          densL = march(posMag, maxLightAngle, true, bL, dL);
//          //  double densRay = dens[(int)dirStep, (int)depStep][0];
//          //  double densMie = dens[(int)dirStep, (int)depStep][1];
//          //     foreach (double[] density in densL)
//          //     {
//          double[][] rayConst = new double[quality][];
//          double[] mieConst = new double[quality];
//          for (int i = 0; i < quality; i++)
//          {
//            for (int j = 0; j < quality; j++)
//            {
//              double densLR = densL[i, j][0];
//              double densLM = densL[i, j][1];
//              double[] attenRay = { 0d, 0d, 0d };
//              double[] attenMie = { 0d, 0d, 0d };
//              for (int lightRot = 0; lightRot < quality; lightRot++)
//              {
//                //  for (double lightRot = 0d; lightRot < 2d * Math.PI; lightRot += 2d * Math.PI / quality)
//                double rotAngle = 2d * Math.PI * lightRot / quality;
//
//                double mu = dirNorm[0] * densL[i, j][2] + dirNorm[1] * densL[i, j][3];
//                // double mu = dens[i, j, 0][2] * densL[i, j, 0][2] + dens[i, j, 0][3] * densL[i, j, 0][3];
//
//                mu *= Math.Cos(rotAngle);
//                double muMu = mu * mu;
//                double phaseMnum = phaseConstM * ((1d - gg) * (muMu + 1d));
//                double phaseMden = Math.Pow(1d + gg - 2d * mu * g, 1.5d) * (2d + gg);
//                double phaseM = phaseMnum / phaseMden;
//                mieConst[lightRot] = phaseM * betaM0;
//                double phaseR = phaseConstR + phaseConstR * muMu;
//                //  rayConst[(int)lightRot][0] = 0d;
//
//                for (int bas = 0; bas < 3; bas++)
//                {
//                  rayConst[lightRot] = new double[3];
//                  rayConst[lightRot][bas] = phaseR * betaR0[bas];
//                }
//
//              }
//              finalR[i, j] = new double[3];
//              finalM[i, j] = new double[3];
//              for (int bas = 0; bas < 3; bas++)
//              {
//                attenRay[bas] += stepDensR * Math.Exp(-(betaR0[bas] * (sumDensR + densLR) + 1.1 * betaM0 * (sumDensM + densLM)));
//                attenMie[bas] += stepDensM * Math.Exp(-(betaR0[bas] * (sumDensR + densLR) + 1.1 * betaM0 * (sumDensM + densLM)));
//
//                finalR[i, j][bas] += attenRay[bas];
//                finalM[i, j][bas] += attenMie[bas];
//
//              }
//            }
//          }
//          double[] blockage = { 0d, 0d, 0d };
//          for (int k = 0; k < 3; k++)
//          {
//            blockage[k] = Math.Exp(-(betaR0[k] * sumDensR + 1.1 * betaM0 * sumDensM));
//          }
//
//          for (int lightRot = 0; lightRot < quality; lightRot++)
//          {
//            for (int lightDir = 0; lightDir < quality; lightDir++)
//            {
//              for (int bas = 0; bas < 3; bas++)
//              {
//                //  color[(int)dirStep, (int)depStep, (int)lightRot] = new double[4];
//                //  color[(int)dirStep, (int)depStep, (int)lightRot][bas] = rayConst[(int)lightRot][bas] * finalR[(int)dirStep, (int)depStep][bas] + mieConst[(int)lightRot] * finalM[(int)dirStep, (int)depStep][bas];
//                //  color[(int)dirStep, (int)depStep, (int)lightRot] = new double[4];
//                color[bas] = (float)(rayConst[lightRot][bas] * finalR[dirStep, depStep][bas] + mieConst[lightRot] * finalM[dirStep, depStep][bas]);
//
//              }
//              //   color[(int)dirStep, (int)depStep, (int)lightRot][3] = 1d - Math.Sqrt(blockage[0] * blockage[0] + blockage[1] * blockage[1] + blockage[2] * blockage[2]);
//              //   color[(int)dirStep, (int)depStep, (int)lightRot][3] = 1d - Math.Sqrt(blockage[0] * blockage[0] + blockage[1] * blockage[1] + blockage[2] * blockage[2]);
//              color[3] = (float)(1d - Math.Sqrt(blockage[0] * blockage[0] + blockage[1] * blockage[1] + blockage[2] * blockage[2]));
//              int pixelX = dirStep + lightDir * quality;
//              int pixelY = depStep + lightDir * quality;
//
//              outImg.SetPixel(pixelX, pixelY, new Color(color[0], color[1], color[2], color[3]));
//
//              GD.Print(color[0], color[1], color[2], color[3]);
//            }
//          }
//
//        }
//
//        //  GD.Print(optRay);
//        //  GD.Print(optMie);
//        // GD.Print(AtmoConst.scaleMie);
//      }
//      // if (isLight)
//      // {
//      //   lightCos[(int)dirStep] = lookCos;
//      // }
//      progBar.Value = (double)dirStep / (double)quality;
//    }
//
//    if (isLight)
//      //  double[][] lightOut =
//      return dens;
//
//
//    outImg.Unlock();
//    outImg.SavePng("res://test.png");
//
//    return null;
//
//  }
//
//  //  internal void lightStep((double, double, double)[,] densities)
//  //  {
//  //    for (int dirStep = 0; dirStep < quality; dirStep++)
//  //    {
//  //      // double[] posVec = { lookCos * midPoint, atmoRad + lookSin * midPoint };
//  //
//  //
//  //      for (int depStep = 0; depStep < quality; depStep++)
//  //      {
//  //        (double, double, double) densLook = densities[dirStep, depStep];
//  //        (double, double, double) densLight = densities[dirStep, quality - 1];
//  //        double lightDensRay = densLight.Item1 - densLook.Item1;
//  //        double lightDensMie = densLight.Item2 - densLook.Item2;
//  //        double height = densLook.Item3;
//  //        for (double lightStep = 0d; depStep < quality; depStep++)
//  //        {
//  //          double maxAngle = Math.PI - Math.Atan(planRad / (planRad + height));
//  //          double lightAngle = maxAngle * lightStep / quality;
//  //
//  //        }
//  //      }
//  //    }
//  //  }
//}
//