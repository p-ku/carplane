using Godot;
using System;

public class Wind : Godot.AudioStreamPlayer
{
  float sample_hz = 22050f; // Keep the number of samples to mix low, GDScript is not super fast.
  float pulse_hz = 440f;
  float phase = 0f;
  int to_fill;
  AudioEffectLowPassFilter lowPass;
  AudioEffectBandPassFilter bandPass;

  Car car;

  AudioStreamPlayback playback; // Actual playback stream, assigned in _ready().


  public override void _Ready()
  {
    // car = (Car)GetNode("../../Car");
    car = (Car)GetTree().Root.FindNode("Car", true, false);

    lowPass = new AudioEffectLowPassFilter();
    bandPass = new AudioEffectBandPassFilter();

    //lowPassFilter = AudioServer.GetBusEffect(1, 0);
    /*     // Stream. = sample_hz; // Setting mix rate is only possible before play().
        playback = GetStreamPlayback();


        this._FillBuffer(); // Prefill, do before play() to avoid delay.
        Play(); */
    AudioServer.AddBusEffect(1, bandPass);

    bandPass.CutoffHz = 1f;
    VolumeDb = -24f;

    Play();
  }
  public override void _Process(float delta)
  {
    //AudioServer.SetBusEffectEnabled(1, 0, true);
    bandPass.CutoffHz = 20f * car.angDampSq;
    VolumeDb = -36f + car.angDampSq;
    //lowPassFilter.Set("CutoffHz", 0f);
  }
}


