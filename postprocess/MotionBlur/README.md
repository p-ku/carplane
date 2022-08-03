# Motion Blur

This motion blur is based on "[A Fast and Stable Feature-Aware Motion Blur Filter](https://casual-effects.com/research/Guertin2014MotionBlur/index.html)" by Guertin, McGuire, and Nowrouzezahrai. I wanted good motion blur in Godot, but there didn't seem to be much out in the community beyond simple implementations. Simple can work. I just wasn't happy with the artifacts that come with it.

https://user-images.githubusercontent.com/66377062/182406779-630d31df-d975-48b7-9e9f-cd5d04e4e8c8.mp4

The quick and dirty: 
1. Find maximum pixel velocity in each *r* by *r* tile (20 pixels by 20 pixels in the video). 
2. Find maximum tile velocity amongst a tile and it's neighbors, 3 tiles by 3 tiles.
3. Weight blur samples and blur the image (I said quick and dirty, read the paper for details).

In the video, I'm showing each buffer that stores information for this method. The first is the velocity and depth buffer. Velocity is stored in the red and green channels, red is vertical, green is horizontal. When there's no motion, it's an ugly green color because velocities are normalized and centered at 0.5. The second buffer is showing max velocities for each tile. For this buffer only velocity is stored using red and green. The third buffer shows the max velocity for each tile's 3x3 tile neighborhood.

![blurexampleedit](https://user-images.githubusercontent.com/66377062/182424465-1122dff5-45dd-4d37-af4f-274d302c5193.jpg)


