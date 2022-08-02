using Godot;
using System;

public class DebugOverlay : Label
{
  string label_text;
  ulong mem;
  int order;
  Globals vars;
  Car car;
  string[] sizes = { "B", "KB", "MB", "GB", "TB" };

  public override void _Process(float delta)
  {
    vars.fps = Engine.GetFramesPerSecond();
    label_text = "";
    mem = OS.GetStaticMemoryUsage();
    order = 0;
    while (mem >= 1024 && order < sizes.Length - 1)
    {
      order++;
      mem = mem / 1024;
    }
    label_text += GD.Str("FPS: ", vars.fps) + "\n";
    //  label_text += GD.Str("FrameTime: ", vars.frameTime) + "\n";
    //  label_text += GD.Str("Totcount: ", vars.totCount) + "\n";
    // label_text += String.Format("{0:0.##} {1}", mem, sizes[order]) + "\n";
    // label_text += GD.Str("Drag: ", vars.DragMag) + "N" + "\n";
    // label_text += GD.Str("Lift: ", car.lift.Length()) + "N" + "\n";
    // label_text += GD.Str("AngDamp", vars.AngDamp) + "\n";

    Text = label_text;
  }
  public override void _Ready()
  {
    vars = (Globals)GetTree().Root.FindNode("Globals", true, false);
    car = (Car)GetTree().Root.FindNode("Car", true, false);
    // cam = (CarCam)GetTree().Root.FindNode("CarCam", true, false);

  }
}
