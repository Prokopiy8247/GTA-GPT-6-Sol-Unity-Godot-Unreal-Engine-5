class_name GTAVehicle
extends CharacterBody3D

## Arcade vehicle controller shared by road vehicles, motorcycles, boats and aircraft.
## Imported models face -Z with their ground contact at local Y = 0.

signal destroyed
signal crime(kind: String, position: Vector3)
signal horned(position: Vector3)
signal health_changed(value: float, maximum: float)

@export var kind: String = "compact"
@export_file("*.glb") var model_path: String = ""
@export var water_level: float = 0.1
@export var paint_color: Color = Color(0.22, 0.63, 0.78)

var driver: Node = null
var health: float = 160.0
var max_health: float = 160.0
var speed: float = 0.0 # metres per second, signed for road vehicles
var engine_on: bool = true
var lights_on: bool = false
var siren_on: bool = false
var ai_target: Vector3 = Vector3.ZERO
var ai_speed: float = 12.0
var ai_enabled: bool = false
var speed_multiplier: float = 1.0
var brake_multiplier: float = 1.0
var grip_multiplier: float = 1.0
var armor_multiplier: float = 1.0
var engine_upgrade: int = 0
var brake_upgrade: int = 0

var _player_control: bool = false
var _profile: Dictionary = {}
var _throttle: float = 0.0
var _steering: float = 0.0
var _braking: bool = false
var _handbrake: bool = false
var _lift_input: float = 0.0
var _flight_throttle: float = 0.0
var _horn_time: float = 0.0
var _impact_cooldown: float = 0.0
var _destroyed: bool = false
var _engine_player: AudioStreamPlayer3D
var _horn_player: AudioStreamPlayer3D
var _siren_player: AudioStreamPlayer3D
var _explosion_player: AudioStreamPlayer3D
var _headlights: Array[SpotLight3D] = []
var _brake_lamps: Array[MeshInstance3D] = []
var _visual_base: Vector3 = Vector3.ZERO
var _paint_index: int = 0
var _ai_driver_visual: Node3D = null
const PAINTS: Array[Color] = [Color(0.22, 0.63, 0.78), Color(0.93, 0.23, 0.19), Color(0.95, 0.82, 0.28), Color(0.1, 0.15, 0.24), Color(0.83, 0.87, 0.82), Color(0.45, 0.3, 0.65)]

@onready var _collider: CollisionShape3D = $CollisionShape3D
@onready var _visual: Node3D = $VisualRoot
@onready var _effects: Node3D = $Effects


func _ready() -> void:
	add_to_group("vehicles")
	floor_snap_length = 0.35
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	_configure_profile()
	_load_visual()
	_make_lights()
	_make_audio()
	_visual_base = _visual.position
	if kind == "boat":
		global_position.y = water_level


func setup(new_kind: String, new_model_path: String = "") -> void:
	kind = _normalize_kind(new_kind)
	model_path = new_model_path
	if is_node_ready():
		_configure_profile()
		_load_visual()
		_make_lights()


func _normalize_kind(value: String) -> String:
	var normalized := value.to_lower().replace("car_", "")
	match normalized:
		"police_cruiser": return "police"
		"motorbike", "bike": return "motorcycle"
		"speedboat", "motorboat": return "boat"
		"propeller_airplane", "plane": return "airplane"
		"police_helicopter": return "police_helicopter"
	return normalized


func _configure_profile() -> void:
	kind = _normalize_kind(kind)
	var dimensions := Vector3(1.9, 1.4, 4.1)
	_profile = {"max_speed": 25.0, "accel": 12.0, "brake": 24.0, "reverse": 9.0, "turn": 1.55, "grip": 4.8}
	match kind:
		"compact":
			_profile.merge({"max_speed": 29.0, "accel": 16.0, "turn": 1.8}, true)
			dimensions = Vector3(1.75, 1.32, 3.55)
		"sedan":
			_profile.merge({"max_speed": 31.0, "accel": 14.0}, true)
		"sport":
			_profile.merge({"max_speed": 44.0, "accel": 24.0, "brake": 31.0, "turn": 1.95}, true)
			dimensions = Vector3(2.05, 1.25, 4.35)
		"muscle":
			_profile.merge({"max_speed": 39.0, "accel": 22.0, "turn": 1.4, "grip": 3.7}, true)
		"suv", "police", "pickup":
			_profile.merge({"max_speed": 32.0, "accel": 13.0, "turn": 1.3}, true)
			dimensions = Vector3(2.1, 1.9, 4.8)
		"van", "truck":
			_profile.merge({"max_speed": 23.0, "accel": 9.0, "turn": 1.1}, true)
			dimensions = Vector3(2.2, 2.5, 5.2)
		"motorcycle":
			_profile.merge({"max_speed": 37.0, "accel": 21.0, "brake": 29.0, "turn": 2.2}, true)
			dimensions = Vector3(0.85, 1.45, 2.3)
		"boat":
			_profile.merge({"max_speed": 27.0, "accel": 11.0, "brake": 8.0, "turn": 1.05}, true)
			dimensions = Vector3(2.35, 1.15, 5.3)
		"helicopter", "police_helicopter":
			_profile.merge({"max_speed": 36.0, "accel": 10.0, "turn": 1.25}, true)
			dimensions = Vector3(2.6, 2.45, 7.0)
		"airplane", "jet":
			_profile.merge({"max_speed": 57.0, "accel": 8.0, "turn": 0.85}, true)
			dimensions = Vector3(9.0, 2.05, 7.4)
	max_health = 100.0 if kind == "motorcycle" else (220.0 if kind in ["helicopter", "police_helicopter", "airplane", "jet"] else 160.0)
	health = max_health
	if is_node_ready():
		var shape := BoxShape3D.new()
		shape.size = dimensions
		_collider.shape = shape
		_collider.position.y = dimensions.y * 0.5 + (0.1 if kind not in ["boat", "helicopter", "police_helicopter", "airplane", "jet"] else 0.0)
	set_meta("dimensions", dimensions)


func _model_filename() -> String:
	match kind:
		"compact", "sedan", "sport", "police": return "car_%s.glb" % kind
		"muscle", "suv": return "car_%s.glb" % kind
		"pickup", "van": return "%s.glb" % kind
		"police_helicopter": return "helicopter.glb"
		"jet": return "airplane.glb"
	return "%s.glb" % kind


func _load_visual() -> void:
	for child in _visual.get_children():
		child.queue_free()
	var path := model_path if not model_path.is_empty() else "res://gta/generated/models/%s" % _model_filename()
	if ResourceLoader.exists(path):
		var resource := load(path)
		if resource is PackedScene:
			var model: Node3D = resource.instantiate() as Node3D
			if model != null:
				_visual.add_child(model)
				model.owner = null
				_apply_paint()
				return
	_build_fallback_visual()


func _build_fallback_visual() -> void:
	var dimensions: Vector3 = get_meta("dimensions", Vector3(1.9, 1.4, 4.1))
	var body := MeshInstance3D.new()
	body.name = "Body"
	var mesh := BoxMesh.new()
	mesh.size = Vector3(dimensions.x * 0.94, dimensions.y * 0.57, dimensions.z * 0.9)
	body.mesh = mesh
	body.position.y = dimensions.y * 0.44
	var material := StandardMaterial3D.new()
	material.albedo_color = paint_color
	material.metallic = 0.36
	material.roughness = 0.3
	body.material_override = material
	_visual.add_child(body)
	if kind not in ["boat", "helicopter", "police_helicopter", "airplane", "jet", "motorcycle"]:
		var roof := MeshInstance3D.new()
		roof.mesh = BoxMesh.new()
		(roof.mesh as BoxMesh).size = Vector3(dimensions.x * 0.73, dimensions.y * 0.45, dimensions.z * 0.45)
		roof.position = Vector3(0, dimensions.y * 0.8, dimensions.z * 0.04)
		roof.material_override = material
		_visual.add_child(roof)
	if kind == "motorcycle":
		for z in [-0.75, 0.75]:
			var wheel := MeshInstance3D.new()
			wheel.mesh = CylinderMesh.new()
			(wheel.mesh as CylinderMesh).top_radius = 0.45
			(wheel.mesh as CylinderMesh).bottom_radius = 0.45
			(wheel.mesh as CylinderMesh).height = 0.18
			wheel.rotation.z = PI * 0.5
			wheel.position = Vector3(0, 0.45, z)
			_visual.add_child(wheel)


func _make_lights() -> void:
	for lamp in _headlights:
		if is_instance_valid(lamp): lamp.queue_free()
	for lamp in _brake_lamps:
		if is_instance_valid(lamp): lamp.queue_free()
	_headlights.clear()
	_brake_lamps.clear()
	var dimensions: Vector3 = get_meta("dimensions", Vector3(1.9, 1.4, 4.1))
	for side in [-1.0, 1.0]:
		var lamp := SpotLight3D.new()
		lamp.position = Vector3(side * dimensions.x * 0.34, dimensions.y * 0.45, -dimensions.z * 0.47)
		lamp.spot_range = 24.0
		lamp.spot_angle = 32.0
		lamp.light_energy = 1.9
		lamp.light_color = Color(1.0, 0.92, 0.75)
		lamp.visible = lights_on
		_effects.add_child(lamp)
		_headlights.append(lamp)
		var brake := MeshInstance3D.new()
		var brake_mesh := SphereMesh.new()
		brake_mesh.radius = 0.12
		brake_mesh.height = 0.24
		brake.mesh = brake_mesh
		brake.position = Vector3(side * dimensions.x * 0.36, dimensions.y * 0.46, dimensions.z * 0.48)
		var brake_mat := StandardMaterial3D.new()
		brake_mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		brake_mat.albedo_color = Color(0.45, 0.02, 0.02)
		brake.material_override = brake_mat
		_effects.add_child(brake)
		_brake_lamps.append(brake)


func _make_audio() -> void:
	_engine_player = AudioStreamPlayer3D.new()
	_engine_player.name = "EngineAudio"
	_engine_player.stream = _load_audio("res://gta/generated/audio/engine.wav", 73.0, 1.0, true)
	_engine_player.volume_db = -31.0
	_engine_player.max_distance = 38.0
	add_child(_engine_player)
	_engine_player.play()
	_horn_player = AudioStreamPlayer3D.new()
	_horn_player.name = "HornAudio"
	_horn_player.stream = _load_audio("res://gta/generated/audio/horn.wav", 310.0, 0.32, false)
	_horn_player.volume_db = -15.0
	_horn_player.max_distance = 55.0
	add_child(_horn_player)
	_siren_player = AudioStreamPlayer3D.new()
	_siren_player.name = "SirenAudio"
	_siren_player.stream = _load_audio("res://gta/generated/audio/siren.wav", 680.0, 1.0, true)
	_siren_player.volume_db = -18.0
	_siren_player.max_distance = 75.0
	add_child(_siren_player)
	_explosion_player = AudioStreamPlayer3D.new()
	_explosion_player.name = "ExplosionAudio"
	_explosion_player.stream = _load_audio("res://gta/generated/audio/explosion.wav", 52.0, 0.5, false)
	_explosion_player.volume_db = -8.0
	_explosion_player.max_distance = 95.0
	add_child(_explosion_player)


func _load_audio(path: String, fallback_hz: float, duration: float, looped: bool) -> AudioStream:
	if ResourceLoader.exists(path):
		var stream: AudioStream = load(path)
		if stream is AudioStreamWAV:
			stream = stream.duplicate()
			if looped: (stream as AudioStreamWAV).loop_mode = AudioStreamWAV.LOOP_FORWARD
		return stream
	return _tone_stream(fallback_hz, duration, looped)


func _tone_stream(frequency: float, duration: float, looped: bool) -> AudioStreamWAV:
	var rate := 11025
	var count := int(rate * duration)
	var bytes := PackedByteArray()
	bytes.resize(count * 2)
	for i in range(count):
		var t := float(i) / float(rate)
		var envelope := 1.0 if looped else minf(1.0, t * 20.0) * minf(1.0, (duration - t) * 10.0)
		var tone := sin(TAU * frequency * t) * 0.33 + sin(TAU * frequency * 2.02 * t) * 0.08
		bytes.encode_s16(i * 2, int(clampf(tone * envelope, -1.0, 1.0) * 32767.0))
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = rate
	wav.data = bytes
	if looped:
		wav.loop_mode = AudioStreamWAV.LOOP_FORWARD
		wav.loop_end = count
	return wav


func set_ai_target(target: Vector3, target_speed: float = 12.0) -> void:
	ai_target = target
	ai_speed = maxf(target_speed, 0.0)
	ai_enabled = true
	_player_control = false
	if is_node_ready(): _ensure_ai_driver_visual()


func stop_ai() -> void:
	ai_enabled = false
	_throttle = 0.0
	_braking = true


func activate_player_input(active: bool) -> void:
	_player_control = active
	if active: ai_enabled = false


func enter(new_driver: Node) -> bool:
	if _destroyed or new_driver == null:
		return false
	var stolen := ai_enabled or (driver != null and driver != new_driver)
	if driver != null and driver != new_driver:
		if driver.has_method("on_carjacked"):
			driver.call("on_carjacked", self)
		elif driver is Node3D:
			(driver as Node3D).global_position = global_position + global_transform.basis.x * 2.5
			(driver as Node3D).show()
	if stolen or kind in ["police", "police_helicopter"]:
		var theft_kind := "police_theft" if kind in ["police", "police_helicopter"] else "vehicle_theft"
		if new_driver.has_signal("crime"):
			new_driver.emit_signal("crime", theft_kind, global_position)
		else:
			crime.emit(theft_kind, global_position)
	if stolen: _eject_ai_driver_visual(new_driver)
	driver = new_driver
	_player_control = true
	ai_enabled = false
	engine_on = true
	if new_driver.has_method("set_vehicle"):
		new_driver.call("set_vehicle", self)
	return true


func exit() -> Vector3:
	var dimensions: Vector3 = get_meta("dimensions", Vector3(1.9, 1.4, 4.1))
	var exit_position := global_position + global_transform.basis.x * (dimensions.x * 0.5 + 1.25)
	exit_position.y = global_position.y + (0.8 if kind == "boat" else 0.05)
	var previous_driver := driver
	driver = null
	_player_control = false
	if previous_driver != null and previous_driver.has_method("set_vehicle"):
		previous_driver.call("set_vehicle", null)
	return exit_position


func _physics_process(delta: float) -> void:
	if _destroyed:
		return
	_impact_cooldown = maxf(0.0, _impact_cooldown - delta)
	_horn_time = maxf(0.0, _horn_time - delta)
	_read_controls()
	if kind == "boat":
		_drive_boat(delta)
	elif kind in ["helicopter", "police_helicopter"]:
		_drive_helicopter(delta)
	elif kind in ["airplane", "jet"]:
		_drive_airplane(delta)
	else:
		_drive_ground(delta)
	_update_presentation(delta)


func _read_controls() -> void:
	_throttle = 0.0
	_steering = 0.0
	_lift_input = 0.0
	_braking = false
	_handbrake = false
	if _player_control and driver != null and not (driver is GTAPlayer and (driver as GTAPlayer).ui_blocked):
		_throttle = Input.get_action_strength("gta_move_forward") - Input.get_action_strength("gta_move_back")
		_steering = Input.get_action_strength("gta_move_right") - Input.get_action_strength("gta_move_left")
		_lift_input = Input.get_action_strength("gta_jump") - Input.get_action_strength("gta_crouch")
		_braking = Input.is_action_pressed("gta_move_back") and speed > 1.0
		_handbrake = Input.is_action_pressed("gta_jump") and kind not in ["helicopter", "police_helicopter", "airplane", "jet"]
	elif ai_enabled:
		var local_target := global_transform.basis.inverse() * (ai_target - global_position)
		var distance := local_target.length()
		var target_angle := atan2(-local_target.x, -local_target.z)
		_steering = clampf(-target_angle * 1.4, -1.0, 1.0)
		var target_speed := minf(ai_speed, float(_profile.get("max_speed", 25.0)))
		if distance < 7.0:
			target_speed *= clampf(distance / 7.0, 0.0, 1.0)
		_throttle = 1.0 if speed < target_speed else 0.0
		_braking = speed > target_speed + 1.5
	else:
		_braking = false


func _drive_ground(delta: float) -> void:
	var max_speed: float = float(_profile["max_speed"]) * speed_multiplier * (1.0 + engine_upgrade * 0.09)
	var acceleration: float = float(_profile["accel"]) * (1.0 + engine_upgrade * 0.12) * (0.52 if health < max_health * 0.3 else 1.0)
	if not engine_on:
		_throttle = 0.0
	if _braking:
		speed = move_toward(speed, 0.0, float(_profile["brake"]) * brake_multiplier * (1.0 + brake_upgrade * 0.14) * delta)
	elif _throttle > 0.05:
		speed = move_toward(speed, max_speed * _throttle, acceleration * delta)
	elif _throttle < -0.05:
		speed = move_toward(speed, -float(_profile["reverse"]) * absf(_throttle), acceleration * 0.75 * delta)
	else:
		speed = move_toward(speed, 0.0, (7.0 if _handbrake else 3.0) * delta)
	if _handbrake:
		speed = move_toward(speed, 0.0, 13.0 * delta)
	var turning_factor := clampf(absf(speed) / 8.0, 0.0, 1.0)
	var direction_sign := signf(speed)
	rotation.y += -_steering * direction_sign * float(_profile["turn"]) * turning_factor * delta * (1.25 if _handbrake else 1.0) * grip_multiplier
	var motion := -global_transform.basis.z * speed
	velocity.x = motion.x
	velocity.z = motion.z
	velocity.y -= 22.0 * delta
	var previous_velocity := velocity
	move_and_slide()
	_check_impact(previous_velocity)
	if not is_on_floor() and global_position.y < -12.0:
		global_position.y = 1.5
		velocity.y = 0.0


func _drive_boat(delta: float) -> void:
	var target_speed := float(_profile["max_speed"]) * speed_multiplier * (1.0 + engine_upgrade * 0.09) * _throttle
	speed = move_toward(speed, target_speed, (float(_profile["accel"]) if _throttle != 0.0 else float(_profile["brake"])) * delta)
	rotation.y += -_steering * float(_profile["turn"]) * clampf(absf(speed) / 7.0, 0.0, 1.0) * delta
	var forward := -global_transform.basis.z * speed
	velocity = Vector3(forward.x, (water_level - global_position.y) * 4.0, forward.z)
	var previous_velocity := velocity
	move_and_slide()
	_check_impact(previous_velocity)
	_visual.rotation.z = lerpf(_visual.rotation.z, -_steering * 0.055, delta * 2.0)


func _drive_helicopter(delta: float) -> void:
	var forward := -global_transform.basis.z
	if _player_control:
		_flight_throttle = move_toward(_flight_throttle, clampf(_throttle, -0.5, 1.0), delta * 2.0)
	elif ai_enabled:
		_flight_throttle = move_toward(_flight_throttle, 0.8, delta)
	else:
		_flight_throttle = move_toward(_flight_throttle, 0.0, delta)
	rotation.y -= _steering * 1.15 * delta
	var horizontal := -global_transform.basis.z * _flight_throttle * float(_profile["max_speed"]) * speed_multiplier
	velocity.x = move_toward(velocity.x, horizontal.x, 10.0 * delta)
	velocity.z = move_toward(velocity.z, horizontal.z, 10.0 * delta)
	if _player_control:
		velocity.y = move_toward(velocity.y, _lift_input * 11.0, 16.0 * delta)
	elif ai_enabled:
		velocity.y = move_toward(velocity.y, clampf((ai_target.y - global_position.y) * 1.5, -5.0, 5.0), 7.0 * delta)
	else:
		velocity.y -= 4.0 * delta
	var before := velocity
	move_and_slide()
	_check_impact(before)
	speed = Vector2(velocity.x, velocity.z).length()
	_visual.rotation.x = lerpf(_visual.rotation.x, -_flight_throttle * 0.16, delta * 2.0)
	_visual.rotation.z = lerpf(_visual.rotation.z, _steering * 0.18, delta * 2.0)
	if global_position.y < -10.0:
		damage(200.0)


func _drive_airplane(delta: float) -> void:
	if _player_control:
		_flight_throttle = move_toward(_flight_throttle, clampf(maxf(_throttle, 0.0), 0.0, 1.0), delta * 0.7)
	elif ai_enabled:
		_flight_throttle = move_toward(_flight_throttle, 0.85, delta * 0.7)
	else:
		_flight_throttle = move_toward(_flight_throttle, 0.0, delta * 0.6)
	var desired_speed := _flight_throttle * float(_profile["max_speed"]) * speed_multiplier
	speed = move_toward(speed, desired_speed, float(_profile["accel"]) * delta)
	var air_control := clampf(speed / 14.0, 0.15, 1.0)
	rotation.y -= _steering * float(_profile["turn"]) * air_control * delta
	if not is_on_floor() or speed > 17.0:
		rotation.x = clampf(rotation.x + _lift_input * 0.52 * air_control * delta, -0.4, 0.4)
	else:
		rotation.x = move_toward(rotation.x, 0.0, delta)
	_visual.rotation.z = lerpf(_visual.rotation.z, _steering * 0.23, delta * 2.0)
	var forward := -global_transform.basis.z
	var lift := maxf(0.0, speed - 15.0) * 0.34 + forward.y * speed * 0.8
	velocity = forward * speed
	velocity.y += lift - (6.5 if speed < 15.0 else 2.5)
	var before := velocity
	move_and_slide()
	_check_impact(before)
	if global_position.y < -10.0:
		damage(200.0)


func _check_impact(previous_velocity: Vector3) -> void:
	if _impact_cooldown > 0.0 or get_slide_collision_count() == 0:
		return
	var delta_speed := (previous_velocity - velocity).length()
	if delta_speed < 9.0:
		return
	_impact_cooldown = 0.45
	damage((delta_speed - 7.0) * (3.2 if kind in ["airplane", "jet", "helicopter", "police_helicopter"] else 1.5))
	if driver != null and driver.has_method("damage") and delta_speed > 17.0:
		driver.call("damage", (delta_speed - 15.0) * 0.85)


func _update_presentation(delta: float) -> void:
	for lamp in _headlights:
		if is_instance_valid(lamp): lamp.visible = lights_on and not _destroyed
	for lamp in _brake_lamps:
		if is_instance_valid(lamp):
			var mat := lamp.material_override as StandardMaterial3D
			mat.albedo_color = Color(1.0, 0.12, 0.07) if _braking or _handbrake else Color(0.38, 0.025, 0.025)
	if is_instance_valid(_engine_player):
		_engine_player.pitch_scale = clampf(0.76 + absf(speed) / 44.0, 0.7, 2.1)
		_engine_player.volume_db = (-46.0 if not engine_on else clampf(-30.0 + absf(speed) * 0.23, -30.0, -18.0))
	if kind == "motorcycle":
		_visual.rotation.z = lerpf(_visual.rotation.z, _steering * clampf(absf(speed) / 20.0, 0.0, 1.0) * 0.23, delta * 3.0)
	elif kind not in ["boat", "helicopter", "police_helicopter", "airplane", "jet"]:
		_visual.rotation.z = lerpf(_visual.rotation.z, _steering * clampf(absf(speed) / 30.0, 0.0, 1.0) * 0.055, delta * 4.0)


func horn() -> void:
	if _horn_time > 0.0 or _destroyed:
		return
	_horn_time = 0.45
	if is_instance_valid(_horn_player): _horn_player.play()
	horned.emit(global_position)


func toggle_lights() -> void:
	lights_on = not lights_on


func toggle_siren() -> void:
	if kind not in ["police", "police_helicopter"]:
		return
	siren_on = not siren_on
	for lamp in _headlights:
		lamp.light_color = Color(0.3, 0.5, 1.0) if siren_on else Color(1.0, 0.92, 0.75)
	if is_instance_valid(_siren_player):
		if siren_on: _siren_player.play()
		else: _siren_player.stop()


func next_paint() -> Color:
	_paint_index = posmod(_paint_index + 1, PAINTS.size())
	paint_color = PAINTS[_paint_index]
	_apply_paint()
	return paint_color


func _apply_paint() -> void:
	if not is_node_ready(): return
	var nodes: Array[Node] = [_visual]
	while not nodes.is_empty():
		var node: Node = nodes.pop_back()
		for child in node.get_children(): nodes.append(child)
		if node is MeshInstance3D:
			var mesh_node := node as MeshInstance3D
			var name_lower := mesh_node.name.to_lower()
			if name_lower.contains("handle") or name_lower.contains("lamp") or name_lower.contains("glass") or name_lower.contains("tire") or name_lower.contains("wheel") or name_lower.contains("hub"):
				continue
			if name_lower.contains("body") or name_lower.contains("paint") or name_lower.contains("shell") or name_lower.contains("hood") or name_lower.contains("door") or name_lower.contains("lower") or name_lower.contains("roof") or name_lower.contains("fender") or name_lower.contains("panel"):
				var material := StandardMaterial3D.new()
				material.albedo_color = paint_color
				material.metallic = 0.4
				material.roughness = 0.27
				mesh_node.material_override = material


func take_damage(amount: float, _attacker: Node = null) -> void:
	damage(amount)


func _ensure_ai_driver_visual() -> void:
	if _ai_driver_visual != null or driver != null or kind in ["helicopter", "police_helicopter", "airplane", "jet"]:
		return
	var path := "res://gta/generated/models/%s.glb" % ("npc_police" if kind == "police" else "npc_civilian")
	if not ResourceLoader.exists(path): return
	var resource := load(path)
	if not (resource is PackedScene): return
	_ai_driver_visual = resource.instantiate() as Node3D
	if _ai_driver_visual == null: return
	_ai_driver_visual.name = "DriverVisual"
	_ai_driver_visual.position = Vector3(-0.42, 0.27, 0.20) if kind not in ["motorcycle", "boat"] else Vector3(0.0, 0.18, 0.12)
	_ai_driver_visual.scale = Vector3.ONE * (0.72 if kind != "motorcycle" else 0.9)
	_visual.add_child(_ai_driver_visual)


func _eject_ai_driver_visual(thief: Node) -> void:
	if _ai_driver_visual == null or not is_instance_valid(_ai_driver_visual):
		_spawn_displaced_driver(global_position + global_transform.basis.x * 2.8, thief)
		return
	var visual := _ai_driver_visual
	_ai_driver_visual = null
	var destination := get_tree().current_scene
	if destination == null: destination = get_parent()
	if destination == null:
		visual.queue_free()
		return
	visual.reparent(destination, true)
	var tween := create_tween()
	tween.tween_property(visual, "global_position", global_position + global_transform.basis.x * 2.8 + Vector3.UP * 0.1, 0.42)
	tween.parallel().tween_property(visual, "rotation:z", 1.2, 0.42)
	tween.tween_callback(func(): _spawn_displaced_driver(visual.global_position, thief))
	tween.tween_callback(visual.queue_free)


func _spawn_displaced_driver(position: Vector3, thief: Node) -> void:
	var destination := get_tree().current_scene
	if destination == null: destination = get_parent()
	if destination == null: return
	var person := GTANPC.new()
	person.setup("police" if kind == "police" else "civilian")
	destination.add_child(person)
	person.global_position = position
	if thief is Node3D:
		person.set_player(thief as Node3D)
		if kind == "police": person.set_alert(true, (thief as Node3D).global_position)
		else: person.panic((thief as Node3D).global_position)
	person.killed.connect(func(victim: GTANPC, attacker: Node) -> void:
		if is_instance_valid(thief) and attacker == thief and thief.has_signal("crime"):
			thief.emit_signal("crime", "police_assault" if victim.kind == "police" else "murder", victim.global_position)
	)
	person.attacked_player.connect(func(amount: float) -> void:
		if is_instance_valid(thief) and thief.has_method("damage"): thief.call("damage", amount)
	)
	get_tree().create_timer(48.0).timeout.connect(func() -> void:
		if is_instance_valid(person): person.queue_free()
	)


func damage(amount: float) -> void:
	if _destroyed or amount <= 0.0:
		return
	health = maxf(0.0, health - amount / maxf(armor_multiplier, 0.1))
	health_changed.emit(health, max_health)
	if health <= 0.0:
		_destroy()


func repair(amount: float = -1.0) -> void:
	if amount < 0.0: amount = max_health
	health = minf(max_health, health + amount)
	_destroyed = false
	engine_on = true
	_collider.disabled = false
	health_changed.emit(health, max_health)


func _destroy() -> void:
	_destroyed = true
	engine_on = false
	_player_control = false
	ai_enabled = false
	lights_on = false
	if is_instance_valid(_engine_player): _engine_player.stop()
	if is_instance_valid(_siren_player): _siren_player.stop()
	if is_instance_valid(_explosion_player): _explosion_player.play()
	var blast := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 0.5
	sphere.height = 1.0
	blast.mesh = sphere
	blast.position.y = 1.0
	var material := StandardMaterial3D.new()
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.albedo_color = Color(1.0, 0.43, 0.04, 0.85)
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	blast.material_override = material
	_effects.add_child(blast)
	var tween := create_tween()
	tween.tween_property(blast, "scale", Vector3(4.0, 4.0, 4.0), 0.32)
	tween.parallel().tween_property(material, "albedo_color", Color(0.15, 0.12, 0.12, 0.0), 0.36)
	tween.tween_callback(blast.queue_free)
	if driver != null and driver.has_method("damage"):
		driver.call("damage", 65.0)
	destroyed.emit()


func get_speed_kmh() -> float:
	return absf(speed) * 3.6


func get_vehicle_type() -> String:
	return kind


func is_destroyed() -> bool:
	return _destroyed
