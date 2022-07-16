# Equations

## Dimensions

Vehicle Length, L = 4 m
Vehicle Width, W = 2 m
Vehicle Height, H = 1 m
α, β, γ

## Aerodynamics

Air Density, ro = 1.229 kg / m^3
Lift Area, Al = 1 m^3
Projected Area, Ap = L W cos(α) cos(γ) + L H cos(β) cos(γ) + W H cos(α) cos(β)

Lift = (1/2) ro v^2 Cl Al

Drag = (1/2) ro v^2 Cd Ap

## Solid cuboid Moment of Inertia

Ih = (1/12) m (w^2 + d^2)
Iw = (1/12) m (d^2 + h^2)
Iw = (1/12) m (w^2 + h^2)

## [Color of sunlight](https://habr.com/en/post/479264/)

rgb(255,242,237)

## Motion Blur

Only linear camera motion is accounted for, and only in relation to stationary objects. Linear blur without angular blur retains the high speed feeling without impacting visibility too much.

These are the equations I derived:

    vec2 trans0 = pixel_pos_cam.xy - linear.xy;
    // angle from pixel to center view
    vec2 th = atan(pixel_pos_cam.xy / -pixel_pos_cam.z);
    // angles between start and end pixels (x and y axes translation) on x and y axes
    vec2 tht = atan(trans0 / -pixel_pos_cam.z);
    // tht.x = -tht.x;
    // tht.y = abs(tht.y);
    // angles between start and end pixel (z axis translation) on x and y axes
    vec2 thtz = atan(pixel_pos_cam.xy / (linear.z - pixel_pos_cam.z));

	vec2 disp = (2. * th - tht - thtz) / fov;