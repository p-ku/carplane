extends VehicleBody

# Camera variables


export(bool) var use_camera = true
var lookSensitivity = 0.1
var minLookAngle = -130.0
var maxLookAngle = 25.0
var followCameraAngle = 20
var camera_onoff = true
var cameraTimerSecond = 2
onready var cameraTimer = 0
var cameraOrbit
var followCameraY = 0

onready var rWing = $"../RightHingeJoint"
onready var lWing = $"../LeftHingeJoint"
var flying = false
# Car variables
# Can't fly below this speed
var min_flight_speed = 10
var max_flight_speed = 30
var turn_speed = 0.75
var pitch_speed = 0.5
var level_speed = 3.0
var throttle_delta = 30
var acceleration = 6.0
var altitude = 20
var forward_speed = 0
var target_speed = 0
var grounded = false
var roll_val = 0
var last_turn = 0
var smooth_input = 0
var pitch_val = 0

export(bool) var use_controls = true
export(bool) var show_settings = true
# These become just placeholders if presets are in use
var MAX_ENGINE_FORCE = 100.0
var MAX_BRAKE = 5.0
var MAX_STEERING = 0.5
var STEERING_SPEED = 7
var MAX_ROLLING = 0.7
var ROLLING_SPEED = 7
var MAX_PITCHING = 2
var PITCHING_SPEED = 7
var sines = 0
var linvel = 0
export var base_gravity = 15
var grav = 0
var gravit3 = 0
var bas = 0
var rolling = 0
var pitching = 0
var aoa = 0
var cl = 0
var lift = 0
var lift_mag = 0
var throttle_val = 0
var thrust_mag = 0
var torque_level = Vector3.ZERO
var torque_roll = Vector3.ZERO
var torque_pitch = Vector3.ZERO
var torque_yaw = Vector3.ZERO
################################################
################## Car Script ##################
################################################

func _ready():

	DebugOverlay.stats.add_property(self, "lift_mag", "")
	DebugOverlay.stats.add_property(self, "thrust_mag", "")
	DebugOverlay.stats.add_property(self, "pitch_val", "")
	DebugOverlay.stats.add_property(self, "roll_val", "")
	DebugOverlay.stats.add_property(self, "torque_level", "")
	DebugOverlay.stats.add_property(self, "torque_roll", "")
	DebugOverlay.stats.add_property(self, "torque_pitch", "")
	DebugOverlay.stats.add_property(self, "torque_yaw", "")

	# A camera node is attached if `Use Camera is checked
	#if(use_camera):
	#	cameraOrbit = Spatial.new()
	#	var aCameraNode : Camera = Camera.new()
	#	aCameraNode.translate(Vector3(0, 0, 0))
	#	aCameraNode.rotation_degrees.y = 180
	#	# You can change the camera position here
		# It is currently placed on the hood
	#	cameraOrbit.translate(Vector3(0, 0.6, 0.4))
	#	cameraOrbit.add_child(aCameraNode)
	#	add_child(cameraOrbit)
	#	
		# When the scene starts, the mouse disappeares
		#Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	
	# ..and the Settingspanel gets instanced
	
func car_force(state, current_transform,object_pos, target_position):
	roll_val = Input.get_action_strength("turn_left")-Input.get_action_strength("turn_right")
	#smooth_input = lerp(last_turn, roll_input, state.get_step())
	sines = sin(current_transform.basis.z.angle_to(object_pos))
	#var cur_level = current_transform.basis.z
	#var tar_level = target_position.normalized()#+current_transform.basis.xform(Vector3(0,pitching,0))).normalized()
	#torque_level = cur_level.cross(tar_level)*linear_velocity.length()+torque_pitch
	var cur_level = current_transform.basis.z
	var tar_level = target_position.normalized()*sines
	torque_level = cur_level.cross(tar_level)*cos(pitch_val)

	torque_pitch = (pitch_val-abs(roll_val))*current_transform.basis.x
	#torque_pitch = 3*pitching*current_transform.basis.x

	#var cur_yaw = current_transform.basis.z
	#var tar_yaw = (target_position.normalized()+current_transform.basis.xform(Vector3(0,pitching,0))).normalized()
	#torque_yaw = current_transform.basis.x*pitching*3
	#var rotation_angle = acos(cur_dir.x) - acos(target_dir.x)
	torque_roll = Vector3.ZERO
	var bank_angle = 0
	if flying:
		
		aoa = linear_velocity.angle_to(current_transform.basis.y)-PI/2
		var cur_roll = current_transform.basis.y
		#var tar_roll = (object_pos.normalized()+current_transform.basis.xform(Vector3(4*rolling,0,0))).normalized()
		var tar_roll = object_pos.normalized().rotated(current_transform.basis.z,-roll_val*PI/2)
		torque_roll = cur_roll.cross(tar_roll)*sines*cos(pitch_val)#(1-cur_roll.cross(tar_roll).dot(object_pos.normalized()))
		var cur_yaw = current_transform.basis.z
		var tar_yaw = target_position.normalized()*sines
		torque_yaw = cur_yaw.cross(tar_yaw)#+roll_val*current_transform.basis.y
		#torque_roll = current_transform.basis.z*rolling
		#linvel = transform.basis.z.dot(linear_velocity.normalized())
		linvel = linear_velocity.length_squared()
		#if linvel > 0:

		
		cl = 5.55*aoa+0.5
		if cl > 1.5:
			cl = 1.5
		if cl < -1.5:
			cl = 0
		#lift = cl*linear_velocity.normalized().rotated(current_transform.basis.x,-PI/2)*linear_velocity.length_squared()/(pow(object_pos.length(),1/2))
		#lift = linear_velocity.normalized().rotated(current_transform.basis.x,-PI/2)*linear_velocity.length_squared()/2
		#lift = (linear_velocity.normalized().rotated(current_transform.basis.x,-PI/2)+current_transform.basis.xform(Vector3(rolling,0,0)))*linear_velocity.length_squared()/2
		#lift = (object_pos.normalized()+object_pos.normalized().rotated(current_transform.basis.z*abs(roll_val),-roll_val*PI/2))*linear_velocity.length_squared()/2
		lift = (object_pos.normalized())*linear_velocity.length_squared()/2
		lift_mag = lift.length()
		#lift = current_transform.basis.y*pow(linear_velocity.length(),2)
		add_central_force(lift)
		#bank_angle = object_pos.angle_to(current_transform.basis.xform(Vector3(rolling,0,0)))
		#add_central_force(current_transform.basis.xform(Vector3(0,lift/cos(bank_angle)-lift,0)))
	#var roll_angle = acos(cur_roll.y) - acos(target_roll.y)
	#var roll_angle = cur_roll.angle_to(target_roll)
	#state.set_angular_velocity(current_transform.basis.z * ((rolling-MAX_ROLLING) / state.get_step()))
	#last_turn = smooth_input
	#add_central_force(Vector3(0,pitching,0))
	add_torque((torque_level+torque_roll+torque_pitch+torque_yaw)*1000)#*linear_velocity.length_squared())

func _integrate_forces(state):
	bas = transform.basis.z
	var toCarNorm = global_transform.origin.normalized()
	var toCar = global_transform.origin
	
	#if flying:
	car_force(state, transform, toCar, linear_velocity)
	

	
	#var toPlanet = global_transform.origin.direction_to(Vector3())
#+$"../../Spatial".global_transform.origin
	# This variable turns the camera when the car turns
	followCameraY = 0
	# If user wants to control the car
	if(!use_controls):
		return
	var steer_val = 0.0

	var brake_val = 0.0

	throttle_val = Input.get_action_strength("thrust")/2
	#add_central_force(transform.basis.xform(Vector3(0,0,thrust*500/pow(toCar.length(),1/3))))
	var thrust = transform.basis.xform(Vector3(0,0,(80000-abs(roll_val)*20000)/toCar.length()))
	add_central_force(thrust)
	thrust_mag = thrust.length()
	pitch_val =  Input.get_action_strength("pitch_down") - Input.get_action_strength("pitch_up")
	brake_val =  Input.get_action_strength("brake")
	steer_val = Input.get_action_strength("turn_left") - Input.get_action_strength("turn_right")
		#if(use_camera): followCameraY = 10
		#if(use_camera): followCameraY = -10
	if Input.is_action_just_pressed("ui_cancel"):
		# Show or hide the Settingspanel with pressing ESC
		if (show_settings):
			if(get_node("Settings").visible):
				get_node("Settings").visible = false
				camera_onoff = !camera_onoff
			else:
				get_node("Settings").visible = true
				camera_onoff = !camera_onoff
		# Show/hide the mouse with pressing ESC if there is a camera attached to the car
#		if (use_camera):
#			if(Input.get_mouse_mode() == Input.MOUSE_MODE_VISIBLE):
#				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
#			else:
#				Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	
	engine_force = throttle_val * MAX_ENGINE_FORCE
	brake = brake_val * MAX_BRAKE
	
	# Using lerp for a smooth steering
	steering = lerp(steering, steer_val * MAX_STEERING, STEERING_SPEED * state.get_step())
	#rolling = lerp(rolling, steer_val * MAX_ROLLING, ROLLING_SPEED * state.get_step())
	#pitching = -lerp(pitching, pitch_val * MAX_PITCHING, PITCHING_SPEED * state.get_step())
	rolling = steer_val * MAX_ROLLING
	pitching = pitch_val* MAX_PITCHING
	#var toCar = $Car.global_transform.origin

	#var density = pow(1-toCar.length()/100,3)
	var density = 10
	if flying:
		#add_force(Vector3(0,density*pow(linear_velocity.z,2),0),Vector3(toCar.x,toCar.y,toCar.z-0.3))

		#add_force(lift,Vector3(transform.origin.x,transform.origin.y,transform.origin.z-0.3))
		#add_force(lift,transform.basis.xform(Vector3(0,0,-0.5)))
		#add_central_force(-toPlanet*base_gravity*self.mass)
		var stable = Vector3(0,0,linear_velocity.angle_to(transform.basis.xform(Vector3(0,0,1)))*500)
		#add_force(stable,transform.basis.xform(Vector3(0,0,-1)))

	gravit3 = (-toCarNorm*base_gravity*self.mass)
	#add_central_force(gravit3)
	grav = gravit3.length()
################################################
################# Camera Script ################
################################################



func _process(delta):
	# If user wants to use the car camera
	$MeshInstance.look_at($"../../Spatial".transform.origin,transform.basis.y)
	if(!use_camera || !use_controls):
		return
	var rMotor = rWing.PARAM_MOTOR_TARGET_VELOCITY
	if Input.is_action_just_pressed("wings"):
		smooth_input = 0

		flying = !flying
		if flying:
			lWing.set_param(lWing.PARAM_MOTOR_TARGET_VELOCITY, -100)
			rWing.set_param(rWing.PARAM_MOTOR_TARGET_VELOCITY, -100)
		else:
			lWing.set_param(lWing.PARAM_MOTOR_TARGET_VELOCITY, 100)
			rWing.set_param(rWing.PARAM_MOTOR_TARGET_VELOCITY, 100)

