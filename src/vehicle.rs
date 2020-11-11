use gdnative::api::RigidBody;
use gdnative::nativescript::init::property::{EnumHint, IntHint, StringHint};
use gdnative::prelude::*;

#[derive(gdnative::NativeClass)]
#[inherit(RigidBody)]
#[register_with(register_properties)]
pub struct Vehicle {}

fn register_properties(builder: &ClassBuilder<Vehicle>) {
    builder
        .add_property::<String>("test/test_enum")
        .with_hint(StringHint::Enum(EnumHint::new(vec![
            "Hello".into(),
            "World".into(),
            "Testing".into(),
        ])))
        .with_getter(|_: &Vehicle, _| "Hello".to_string())
        .done();

    builder
        .add_property("test/test_flags")
        .with_hint(IntHint::Flags(EnumHint::new(vec![
            "A".into(),
            "B".into(),
            "C".into(),
            "D".into(),
        ])))
        .with_getter(|_: &Vehicle, _| 0)
        .done();
}

#[gdnative::methods]
impl Vehicle {
    fn new(_owner: &RigidBody) -> Self {
        Vehicle {}
    }

    #[export]
    fn _physics_process(&mut self, owner: &RigidBody, delta: f32) {
        let mut thrust = Vector3::new(0.0, 0.0, 0.0);
        let mut drag = Vector3::new(0.0, 0.0, 0.0);
        let position = Vector3::new(0.0, 0.0, 0.0);
        /*         let distance_squared =
            owner.to_global(position).length() * owner.to_global(position).length();
        let gravity_dir = -owner.to_global(position).normalize();
        let gravity_mag = 1000.0 / distance_squared; */

        let gravity_dir = -owner.to_global(position).normalize();
        //let gravity_mag = 1962.0 * 544.0 / owner.to_global(position).length(); //based on radius
        let gravity_mag = 1200.0 * 9.8;
        let gravity = Vector3::new(
            gravity_mag * gravity_dir.x,
            gravity_mag * gravity_dir.y,
            gravity_mag * gravity_dir.z,
        );
        let input = Input::godot_singleton();
        /*         if Input::is_action_pressed(&input, "ui_right") {
            force.x += 5.0
        }
        if Input::is_action_pressed(&input, "ui_left") {
            force.x -= 5.0
        }
        if Input::is_action_pressed(&input, "ui_down") {
            force.y += 5.0
        }
        if Input::is_action_pressed(&input, "ui_up") {
            force.y -= 5.0
        } */
        if Input::is_key_pressed(&input, 32) {
            // thrust.z = 100.0 * (200.0 / owner.to_global(position).length())
            thrust.z = 544.0 * (1962.0 / owner.to_global(position).length())
        } else {
            thrust.z = 0.0
        }

        let drag_dir = -owner.linear_velocity().normalize();
        let drag_mag = 0.1
            * (200.0 / owner.to_global(position).length())
            * owner.linear_velocity().length()
            * owner.linear_velocity().length()
            * (100.0 / owner.to_global(position).length());
        let drag = Vector3::new(
            drag_mag * drag_dir.x,
            drag_mag * drag_dir.y,
            drag_mag * drag_dir.z,
        );
        thrust = owner.global_transform().basis.xform(thrust);
        let aoa = Vector3::new(0.0, 0.0, 1.0).cross(-drag_dir);
        owner.add_central_force(thrust);
        if drag_mag > 0.0 {
            owner.add_central_force(drag);
        }
        owner.add_central_force(gravity);
    }

    #[export]
    fn _ready(&mut self, owner: &RigidBody) {
        owner.set_physics_process(true);
    }
    /*
    #[export]
    fn _physics_process(&mut self, owner: &RigidBody, delta: f64) {
        use gdnative::api::SpatialMaterial;

        self.time += delta as f32;
        owner.rotate_y(self.rotate_speed * delta);

        let offset = Vector3::new(0.0, 1.0, 0.0) * self.time.cos() * 0.5;
        owner.set_translation(self.start + offset);

        if let Some(mat) = owner.get_surface_material(0) {
            let mat = unsafe { mat.assume_safe() };
            let mat = mat.cast::<SpatialMaterial>().expect("Incorrect material");
            mat.set_albedo(Color::rgba(self.time.cos().abs(), 0.0, 0.0, 1.0));
        }
    }*/
}
