use gdnative::prelude::*;

mod extensions;
mod vehicle;

fn init(handle: InitHandle) {
    handle.add_class::<vehicle::Vehicle>();
}

godot_init!(init);
