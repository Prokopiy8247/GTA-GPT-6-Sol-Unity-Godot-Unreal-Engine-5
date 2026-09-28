class_name GTAPlayer
extends CharacterBody3D

signal fired(position: Vector3, kind: String)
signal damaged(amount: float)
signal died
signal crime(kind: String, position: Vector3)
signal weapon_changed(id: String)
signal health_changed(value: float, maximum: float)
signal vehicle_changed(vehicle: GTAVehicle)

const WEAPONS := {
	"fists": {"damage": 20.0, "range": 2.2, "interval": 0.6, "capacity": 0, "reserve": 0, "pellets": 1, "spread": 0.0, "reload": 0.0, "automatic": false},
	"knife": {"damage": 36.0, "range": 2.4, "interval": 0.55, "capacity": 0, "reserve": 0, "pellets": 1, "spread": 0.0, "reload": 0.0, "automatic": false},
	"bat": {"damage": 31.0, "range": 2.8, "interval": 0.75, "capacity": 0, "reserve": 0, "pellets": 1, "spread": 0.0, "reload": 0.0, "automatic": false},
	"pistol": {"damage": 28.0, "range": 85.0, "interval": 0.24, "capacity": 15, "reserve": 105, "pellets": 1, "spread": 0.008, "reload": 1.45, "automatic": false},
	"heavy_pistol": {"damage": 43.0, "range": 110.0, "interval": 0.38, "capacity": 8, "reserve": 56, "pellets": 1, "spread": 0.011, "reload": 1.8, "automatic": false},
	"smg": {"damage": 15.0, "range": 70.0, "interval": 0.075, "capacity": 32, "reserve": 224, "pellets": 1, "spread": 0.021, "reload": 1.75, "automatic": true},
	"shotgun": {"damage": 13.0, "range": 35.0, "interval": 0.8, "capacity": 7, "reserve": 49, "pellets": 8, "spread": 0.067, "reload": 2.15, "automatic": false},
	"rifle": {"damage": 23.0, "range": 135.0, "interval": 0.105, "capacity": 30, "reserve": 180, "pellets": 1, "spread": 0.014, "reload": 2.0, "automatic": true},
	"sniper": {"damage": 95.0, "range": 320.0, "interval": 1.25, "capacity": 5, "reserve": 35, "pellets": 1, "spread": 0.001, "reload": 2.65, "automatic": false}
}

@export var walk_speed: float = 5.1
@export var sprint_speed: float = 9.2
@export var crouch_speed: float = 2.45
@export var jump_velocity: float = 7.4
@export var water_level: float = 0.1
@export_file("*.glb") var character_model_path: String = "res://gta/generated/models/player.glb"

var health: float = 100.0
var max_health: float = 100.0
var armor: float = 25.0
var cash: int = 1200
var ui_blocked: bool = false
var stealth_active: bool = false
var cover_active: bool = false
var has_parachute: bool = true
var scuba: bool = false
var parachute_deployed: bool = false
var breath: float = 25.0
var current_vehicle: GTAVehicle = null
var world: Node = null
var owned_weapons: Array[String] = ["fists", "pistol"]
var weapon_state: Dictionary = {}
var weapon_mods: Dictionary = {}
var current_weapon_id: String = "pistol"
var outfit_index: int = 0

var _camera_yaw: float = 0.0
var _camera_pitch: float = -0.13
var _fire_cooldown: float = 0.0
var _reload_remaining: float = 0.0
var _muzzle_remaining: float = 0.0
var _camera_shake: float = 0.0
var _aiming: bool = false
var _crouching: bool = false
var _swimming: bool = false
var _dead: bool = false
var _first_person: bool = false
var _spawn_position: Vector3 = Vector3(0, 2, 0)
var _walk_cycle: float = 0.0
var _model_instance: Node3D = null
var _animation_player: AnimationPlayer = null
var _weapon_instance: Node3D = null
var _weapon_attachment: Node3D = null
var _muzzle_light: OmniLight3D
var _muzzle_sphere: MeshInstance3D
var _parachute_visual: MeshInstance3D
var _gunshot_player: AudioStreamPlayer3D
var _footstep_player: AudioStreamPlayer3D
var _footstep_distance: float = 0.0
var _cover_normal: Vector3 = Vector3.ZERO
var _cover_low: bool = false
const OUTFIT_TINTS: Array[Color] = [Color(1.0, 1.0, 1.0), Color(0.65, 0.86, 1.0), Color(1.0, 0.76, 0.67), Color(0.82, 1.0, 0.78)]

@onready var _collider: CollisionShape3D = $CollisionShape3D
@onready var _visual: Node3D = $VisualRoot
@onready var _model_container: Node3D = $VisualRoot/ModelContainer
@onready var _weapon_socket: Node3D = $VisualRoot/WeaponSocket
@onready var _camera_pivot: Node3D = $CameraPivot
@onready var _spring_arm: SpringArm3D = $CameraPivot/SpringArm3D
@onready var camera: Camera3D = $CameraPivot/SpringArm3D/Camera3D


func _ready() -> void:
	_register_inputs()
	add_to_group("player")
	add_to_group("damageable")
	floor_snap_length = 0.22
	_spawn_position = global_position
	_camera_pivot.global_position = global_position + Vector3.UP * 1.6
	_camera_yaw = rotation.y
	for id in WEAPONS.keys():
		var info: Dictionary = WEAPONS[id]
		weapon_state[id] = {"magazine": int(info["capacity"]), "reserve": int(info["reserve"])}
		weapon_mods[id] = {}
	_load_character_model()
	_make_muzzle_effect()
	_make_parachute_visual()
	_make_audio()
	_refresh_weapon_model()
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _register_inputs() -> void:
	var keys := {
		"gta_move_forward": [KEY_W],
		"gta_move_back": [KEY_S],
		"gta_move_left": [KEY_A],
		"gta_move_right": [KEY_D],
		"gta_sprint": [KEY_SHIFT],
		"gta_jump": [KEY_SPACE],
		"gta_crouch": [KEY_CTRL, KEY_C],
		"gta_vehicle": [KEY_F],
		"gta_reload": [KEY_R],
		"gta_horn": [KEY_H],
		"gta_lights": [KEY_L],
		"gta_siren": [KEY_J],
		"gta_camera": [KEY_V],
		"gta_cover": [KEY_Q],
		"gta_stealth": [KEY_X]
	}
	for action_name in keys.keys():
		if not InputMap.has_action(action_name): InputMap.add_action(action_name)
		for key_code in keys[action_name]:
			var key_event := InputEventKey.new()
			key_event.physical_keycode = key_code
			if not InputMap.action_has_event(action_name, key_event):
				InputMap.action_add_event(action_name, key_event)
	for mouse_binding in [{"action": "gta_fire", "button": MOUSE_BUTTON_LEFT}, {"action": "gta_aim", "button": MOUSE_BUTTON_RIGHT}]:
		var action_name: String = mouse_binding["action"]
		if not InputMap.has_action(action_name): InputMap.add_action(action_name)
		var mouse_event := InputEventMouseButton.new()
		mouse_event.button_index = mouse_binding["button"]
		if not InputMap.action_has_event(action_name, mouse_event):
			InputMap.action_add_event(action_name, mouse_event)


func _unhandled_input(event: InputEvent) -> void:
	if ui_blocked or _dead:
		return
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		_camera_yaw -= event.relative.x * 0.00265
		_camera_pitch = clampf(_camera_pitch - event.relative.y * 0.00265, -1.18, 1.22)
	elif event is InputEventMouseButton and event.pressed:
		if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED and event.button_index == MOUSE_BUTTON_LEFT:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_WHEEL_UP:
			_cycle_weapon(-1)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_cycle_weapon(1)
	elif event is InputEventKey and event.pressed and not event.echo:
		if event.is_action_pressed("gta_stealth"):
			stealth_active = not stealth_active
		elif event.is_action_pressed("gta_cover"):
			if cover_active: cover_active = false
			else: _try_enter_cover()
		elif event.keycode == KEY_ESCAPE:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		elif event.keycode >= KEY_1 and event.keycode <= KEY_9:
			var index: int = int(event.keycode) - KEY_1
			if index < owned_weapons.size(): set_weapon(owned_weapons[index])


func _physics_process(delta: float) -> void:
	_fire_cooldown = maxf(0.0, _fire_cooldown - delta)
	_reload_remaining = maxf(0.0, _reload_remaining - delta)
	_muzzle_remaining = maxf(0.0, _muzzle_remaining - delta)
	_camera_shake = maxf(0.0, _camera_shake - delta * 2.0)
	if _reload_remaining == 0.0 and has_meta("reload_pending"):
		remove_meta("reload_pending")
		_complete_reload()
	_muzzle_light.visible = _muzzle_remaining > 0.0
	_muzzle_sphere.visible = _muzzle_remaining > 0.0
	if _dead:
		return
	if current_vehicle != null:
		if not is_instance_valid(current_vehicle):
			set_vehicle(null)
		else:
			global_position = current_vehicle.global_position + Vector3.UP * 0.6
			velocity = Vector3.ZERO
			if not ui_blocked:
				if Input.is_action_just_pressed("gta_vehicle"):
					var exit_position := current_vehicle.exit()
					global_position = exit_position
				if current_vehicle != null:
					if Input.is_action_just_pressed("gta_horn"): current_vehicle.horn()
					if Input.is_action_just_pressed("gta_lights"): current_vehicle.toggle_lights()
					if Input.is_action_just_pressed("gta_siren"): current_vehicle.toggle_siren()
					_aiming = Input.is_action_pressed("gta_aim")
					if current_weapon_id in ["pistol", "heavy_pistol", "smg"] and _aiming:
						_process_fire_input()
			if current_vehicle != null:
				return
	if ui_blocked:
		velocity.x = move_toward(velocity.x, 0.0, 16.0 * delta)
		velocity.z = move_toward(velocity.z, 0.0, 16.0 * delta)
		velocity.y -= 23.0 * delta
		move_and_slide()
		return
	if Input.is_action_just_pressed("gta_vehicle"):
		_try_enter_vehicle()
		if current_vehicle != null: return
	if Input.is_action_just_pressed("gta_camera"):
		_first_person = not _first_person
	if Input.is_action_just_pressed("gta_reload"):
		reload_weapon()
	_aiming = Input.is_action_pressed("gta_aim")
	_swimming = _in_water()
	if _swimming: cover_active = false
	_crouching = (Input.is_action_pressed("gta_crouch") or (cover_active and _cover_low) or stealth_active) and not _swimming
	if _swimming:
		_move_swimming(delta)
	elif cover_active:
		_move_cover(delta)
	else:
		_move_ground(delta)
	_process_fire_input()
	_update_visual_motion(delta)
	if global_position.y < -30.0:
		damage(100.0)


func _process(delta: float) -> void:
	if not is_node_ready(): return
	var focus := global_position + Vector3.UP * (1.8 if current_vehicle != null else (1.22 if _crouching else 1.56))
	if current_vehicle != null and is_instance_valid(current_vehicle):
		focus = current_vehicle.global_position + Vector3.UP * (2.5 if current_vehicle.kind in ["helicopter", "police_helicopter", "airplane", "jet"] else 1.65)
	_camera_pivot.global_position = _camera_pivot.global_position.lerp(focus, clampf(delta * 12.0, 0.0, 1.0))
	_camera_pivot.rotation = Vector3(_camera_pitch + randf_range(-_camera_shake, _camera_shake) * 0.03, _camera_yaw, 0.0)
	var target_arm := 0.1 if _first_person and current_vehicle == null else (2.55 if _aiming else (6.6 if current_vehicle != null and current_vehicle.kind in ["airplane", "jet", "helicopter", "police_helicopter"] else (5.3 if current_vehicle != null else 4.2)))
	_spring_arm.spring_length = lerpf(_spring_arm.spring_length, target_arm, clampf(delta * 9.0, 0.0, 1.0))
	camera.h_offset = lerpf(camera.h_offset, (0.42 if _aiming and not _first_person else 0.0), clampf(delta * 9.0, 0.0, 1.0))
	var optic: bool = bool((weapon_mods.get(current_weapon_id, {}) as Dictionary).get("optic", false))
	var target_fov := 24.0 if current_weapon_id == "sniper" and _aiming else ((39.0 if optic else 54.0) if _aiming else (78.0 if current_vehicle != null else 70.0))
	camera.fov = lerpf(camera.fov, target_fov, clampf(delta * 8.0, 0.0, 1.0))
	_visual.visible = current_vehicle == null and not (_first_person and _spring_arm.spring_length < 0.35)
	_parachute_visual.visible = parachute_deployed and current_vehicle == null


func _move_ground(delta: float) -> void:
	var input := Input.get_vector("gta_move_left", "gta_move_right", "gta_move_forward", "gta_move_back")
	var direction := Vector3(input.x, 0.0, input.y).rotated(Vector3.UP, _camera_yaw).normalized()
	var speed_target := (2.1 if stealth_active else crouch_speed) if _crouching else (sprint_speed if Input.is_action_pressed("gta_sprint") and not _aiming else walk_speed)
	if _aiming: speed_target *= 0.58
	var acceleration := 24.0 if is_on_floor() else 8.0
	velocity.x = move_toward(velocity.x, direction.x * speed_target, acceleration * delta)
	velocity.z = move_toward(velocity.z, direction.z * speed_target, acceleration * delta)
	if is_on_floor():
		parachute_deployed = false
		if Input.is_action_just_pressed("gta_jump") and not _crouching:
			velocity.y = jump_velocity
	else:
		if has_parachute and velocity.y < -7.0 and Input.is_action_just_pressed("gta_jump"):
			parachute_deployed = true
		if parachute_deployed:
			velocity.y = move_toward(velocity.y, -3.2, 19.0 * delta)
			velocity.x *= 1.0 + delta * 0.08
			velocity.z *= 1.0 + delta * 0.08
		else:
			velocity.y -= 23.0 * delta
	var falling_velocity := velocity.y
	move_and_slide()
	if is_on_floor() and falling_velocity < -18.0:
		damage(absf(falling_velocity) - 16.0)
	if _aiming:
		_visual.rotation.y = lerp_angle(_visual.rotation.y, _camera_yaw, clampf(delta * 13.0, 0.0, 1.0))
	elif direction.length_squared() > 0.01:
		_visual.rotation.y = lerp_angle(_visual.rotation.y, atan2(-direction.x, -direction.z), clampf(delta * 10.0, 0.0, 1.0))
	_collider.position.y = lerpf(_collider.position.y, 0.64 if _crouching else 0.9, clampf(delta * 12.0, 0.0, 1.0))
	var capsule := _collider.shape as CapsuleShape3D
	if capsule != null:
		capsule.height = lerpf(capsule.height, 1.25 if _crouching else 1.75, clampf(delta * 12.0, 0.0, 1.0))


func _try_enter_cover() -> void:
	if _swimming or current_vehicle != null or not is_on_floor(): return
	var forward := -_visual.global_transform.basis.z
	forward.y = 0.0
	forward = forward.normalized()
	var low_start := global_position + Vector3.UP * 0.78
	var low_hit := _trace(low_start, low_start + forward * 1.25)
	if low_hit.is_empty(): return
	var normal: Vector3 = low_hit["normal"]
	normal.y = 0.0
	if normal.length_squared() < 0.65: return
	_cover_normal = normal.normalized()
	var high_start := global_position + Vector3.UP * 1.36
	_cover_low = _trace(high_start, high_start + forward * 1.25).is_empty()
	cover_active = true
	velocity.x = 0.0
	velocity.z = 0.0


func _move_cover(delta: float) -> void:
	if Input.is_action_just_pressed("gta_jump") or (Input.is_action_pressed("gta_move_back") and not _aiming):
		cover_active = false
		_move_ground(delta)
		return
	var tangent := Vector3.UP.cross(_cover_normal).normalized()
	var side_input := Input.get_action_strength("gta_move_right") - Input.get_action_strength("gta_move_left")
	var sideways := tangent * side_input * (2.0 if stealth_active else 3.05)
	velocity.x = move_toward(velocity.x, sideways.x, 18.0 * delta)
	velocity.z = move_toward(velocity.z, sideways.z, 18.0 * delta)
	velocity.y -= 23.0 * delta
	move_and_slide()
	var ray_start := global_position + Vector3.UP * (0.68 if _cover_low else 1.05)
	var wall_hit := _trace(ray_start, ray_start - _cover_normal * 1.55)
	if wall_hit.is_empty() or not is_on_floor():
		cover_active = false
		return
	var wall_normal: Vector3 = wall_hit["normal"]
	wall_normal.y = 0.0
	if wall_normal.length_squared() < 0.55:
		cover_active = false
		return
	_cover_normal = wall_normal.normalized()
	var distance := (global_position - Vector3(wall_hit["position"].x, global_position.y, wall_hit["position"].z)).dot(_cover_normal)
	if distance > 1.32:
		cover_active = false
	elif distance < 0.57:
		global_position += _cover_normal * (0.57 - distance)
	_visual.rotation.y = lerp_angle(_visual.rotation.y, atan2(_cover_normal.x, _cover_normal.z), clampf(delta * 11.0, 0.0, 1.0))


func _in_water() -> bool:
	if global_position.y > water_level + 0.35: return false
	if is_on_floor() and global_position.y >= water_level - 0.25: return false
	if world != null and world.has_method("is_water_position"):
		return bool(world.call("is_water_position", global_position))
	if world != null and world.has_method("is_position_in_water"):
		return bool(world.call("is_position_in_water", global_position))
	return global_position.z >= 220.0


func _move_swimming(delta: float) -> void:
	parachute_deployed = false
	var input := Input.get_vector("gta_move_left", "gta_move_right", "gta_move_forward", "gta_move_back")
	var direction := Vector3(input.x, 0.0, input.y).rotated(Vector3.UP, _camera_yaw).normalized()
	var swimming_speed := 4.1 if Input.is_action_pressed("gta_sprint") else 2.8
	velocity.x = move_toward(velocity.x, direction.x * swimming_speed, 6.0 * delta)
	velocity.z = move_toward(velocity.z, direction.z * swimming_speed, 6.0 * delta)
	var target_y := water_level - (1.0 if Input.is_action_pressed("gta_crouch") else 0.35)
	if Input.is_action_pressed("gta_jump"): target_y = water_level + 0.15
	velocity.y = clampf((target_y - global_position.y) * 3.0, -2.5, 2.5)
	move_and_slide()
	if global_position.y < water_level - 0.8 and not scuba:
		breath = maxf(0.0, breath - delta)
		if breath <= 0.0: damage(9.0 * delta)
	else:
		breath = minf(25.0, breath + delta * 3.0)
	_visual.rotation.y = lerp_angle(_visual.rotation.y, atan2(-direction.x, -direction.z), clampf(delta * 6.0, 0.0, 1.0)) if direction.length_squared() > 0.01 else _visual.rotation.y


func _update_visual_motion(delta: float) -> void:
	var horizontal_speed := Vector2(velocity.x, velocity.z).length()
	_walk_cycle += horizontal_speed * delta * (1.7 if _swimming else 3.2)
	var bob := sin(_walk_cycle) * minf(horizontal_speed / 6.0, 1.0) * (0.025 if _crouching else 0.05)
	_visual.position.y = lerpf(_visual.position.y, bob, clampf(delta * 12.0, 0.0, 1.0))
	_visual.scale.y = lerpf(_visual.scale.y, 0.73 if _crouching else 1.0, clampf(delta * 10.0, 0.0, 1.0))
	_visual.rotation.z = lerpf(_visual.rotation.z, sin(_walk_cycle * 0.5) * minf(horizontal_speed / 9.0, 1.0) * 0.03, delta * 6.0)
	if is_instance_valid(_animation_player):
		var animation := "player_walk" if horizontal_speed > 0.9 and is_on_floor() else "player_idle"
		if _animation_player.has_animation(animation):
			if _animation_player.current_animation != animation: _animation_player.play(animation, 0.18)
			_animation_player.speed_scale = clampf(horizontal_speed / walk_speed, 0.8, 1.85) if animation == "player_walk" else 1.0
	if is_instance_valid(_footstep_player): _footstep_player.volume_db = -33.0 if stealth_active else -21.0
	if is_on_floor() and horizontal_speed > 1.1:
		_footstep_distance += horizontal_speed * delta
		if _footstep_distance > 1.7:
			_footstep_distance = 0.0
			if is_instance_valid(_footstep_player):
				_footstep_player.pitch_scale = randf_range(0.92, 1.09)
				_footstep_player.play()


func _process_fire_input() -> void:
	if _reload_remaining > 0.0 or _fire_cooldown > 0.0:
		return
	var weapon: Dictionary = WEAPONS.get(current_weapon_id, WEAPONS["fists"])
	var pressed := Input.is_action_pressed("gta_fire") if bool(weapon["automatic"]) else Input.is_action_just_pressed("gta_fire")
	if not pressed:
		return
	if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED:
		return
	if int(weapon["capacity"]) == 0:
		_melee_attack(weapon)
	else:
		_fire_weapon(weapon)


func _melee_attack(weapon: Dictionary) -> void:
	_fire_cooldown = float(weapon["interval"])
	var start := camera.global_position
	var end := start - camera.global_transform.basis.z * float(weapon["range"])
	var hit := _trace(start, end)
	if not hit.is_empty():
		var melee_damage := float(weapon["damage"])
		var collider: Object = hit.get("collider")
		if stealth_active and collider is GTANPC:
			var victim := collider as GTANPC
			var to_player := (global_position - victim.global_position).normalized()
			if (-victim.global_transform.basis.z).dot(to_player) < -0.25:
				melee_damage *= 3.2
		_hit_target(hit, melee_damage)
		crime.emit("assault", global_position)


func _fire_weapon(weapon: Dictionary) -> void:
	var state: Dictionary = weapon_state[current_weapon_id]
	if int(state["magazine"]) <= 0:
		reload_weapon()
		return
	state["magazine"] = int(state["magazine"]) - 1
	weapon_state[current_weapon_id] = state
	_fire_cooldown = float(weapon["interval"])
	var mods: Dictionary = weapon_mods.get(current_weapon_id, {})
	var suppressed := bool(mods.get("suppressor", false))
	_muzzle_remaining = 0.022 if suppressed else 0.055
	if is_instance_valid(_gunshot_player):
		_gunshot_player.pitch_scale = randf_range(0.92, 1.1)
		_gunshot_player.volume_db = -25.0 if suppressed else -8.0
		_gunshot_player.play()
	_camera_shake = minf(0.9, _camera_shake + (0.18 if current_weapon_id == "shotgun" else 0.055))
	_camera_pitch = clampf(_camera_pitch + (0.026 if current_weapon_id == "shotgun" else 0.008), -1.18, 1.22)
	var spread := float(weapon["spread"]) * (0.52 if _aiming else (2.2 if cover_active else 1.5)) * (1.75 if current_vehicle != null else 1.0) * (0.68 if bool(mods.get("grip", false)) else 1.0)
	var center := get_viewport().get_visible_rect().size * 0.5
	var start := camera.project_ray_origin(center)
	var base_dir := camera.project_ray_normal(center)
	var hit_position := start + base_dir * float(weapon["range"])
	for pellet in range(int(weapon["pellets"])):
		var direction := (base_dir + camera.global_transform.basis.x * randf_range(-spread, spread) + camera.global_transform.basis.y * randf_range(-spread, spread)).normalized()
		var end := start + direction * float(weapon["range"])
		var hit := _trace(start, end)
		if not hit.is_empty():
			hit_position = hit["position"]
			_hit_target(hit, float(weapon["damage"]))
			if pellet == 0: _spawn_impact(hit_position)
		elif pellet == 0:
			hit_position = end
	if get_tree().current_scene != null:
		_spawn_tracer(_weapon_socket.global_position, hit_position)
	fired.emit(_weapon_socket.global_position, "suppressed" if suppressed else current_weapon_id)


func _trace(start: Vector3, end: Vector3) -> Dictionary:
	var query := PhysicsRayQueryParameters3D.create(start, end)
	query.exclude = [get_rid()]
	if current_vehicle != null and is_instance_valid(current_vehicle): query.exclude.append(current_vehicle.get_rid())
	return get_world_3d().direct_space_state.intersect_ray(query)


func _hit_target(hit: Dictionary, amount: float) -> void:
	var collider: Object = hit.get("collider")
	if collider == null: return
	var target: Node = collider as Node
	for i in range(3):
		if target == null: break
		if target != self and target.has_method("take_damage"):
			target.call("take_damage", amount, self)
			if target is GTANPC and (target as GTANPC).kind in ["police", "tactical"]:
				crime.emit("police_assault", hit["position"])
			elif target.is_in_group("npcs"):
				crime.emit("assault", hit["position"])
			return
		if target != self and target.has_method("damage"):
			target.call("damage", amount)
			return
		target = target.get_parent()


func _spawn_tracer(start: Vector3, end: Vector3) -> void:
	var length := start.distance_to(end)
	if length < 0.01: return
	var tracer := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = 0.018
	mesh.bottom_radius = 0.018
	mesh.height = length
	tracer.mesh = mesh
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 0.83, 0.44, 0.8)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	tracer.material_override = mat
	get_tree().current_scene.add_child(tracer)
	tracer.global_position = (start + end) * 0.5
	tracer.quaternion = Quaternion(Vector3.UP, (end - start).normalized())
	var tween := create_tween()
	tween.tween_property(mat, "albedo_color", Color(1.0, 0.83, 0.44, 0.0), 0.12)
	tween.tween_callback(tracer.queue_free)


func _spawn_impact(position: Vector3) -> void:
	var spark := MeshInstance3D.new()
	var mesh := SphereMesh.new()
	mesh.radius = 0.075
	mesh.height = 0.15
	spark.mesh = mesh
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 0.78, 0.28, 1.0)
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	spark.material_override = mat
	get_tree().current_scene.add_child(spark)
	spark.global_position = position
	var tween := create_tween()
	tween.tween_property(spark, "scale", Vector3(0.05, 0.05, 0.05), 0.21)
	tween.parallel().tween_property(mat, "albedo_color", Color(1.0, 0.78, 0.28, 0.0), 0.21)
	tween.tween_callback(spark.queue_free)


func _make_muzzle_effect() -> void:
	_muzzle_light = OmniLight3D.new()
	_muzzle_light.light_color = Color(1.0, 0.73, 0.31)
	_muzzle_light.light_energy = 2.6
	_muzzle_light.omni_range = 3.5
	_muzzle_light.position = Vector3(0, 0, -0.56)
	_muzzle_light.visible = false
	_weapon_socket.add_child(_muzzle_light)
	_muzzle_sphere = MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 0.1
	sphere.height = 0.2
	_muzzle_sphere.mesh = sphere
	_muzzle_sphere.position = _muzzle_light.position
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 0.71, 0.2)
	_muzzle_sphere.material_override = mat
	_muzzle_sphere.visible = false
	_weapon_socket.add_child(_muzzle_sphere)
	_weapon_attachment = Node3D.new()
	_weapon_attachment.name = "WeaponAttachments"
	_weapon_socket.add_child(_weapon_attachment)


func _make_parachute_visual() -> void:
	_parachute_visual = MeshInstance3D.new()
	var mesh := SphereMesh.new()
	mesh.radius = 1.0
	mesh.height = 0.6
	_parachute_visual.mesh = mesh
	_parachute_visual.scale = Vector3(2.1, 0.55, 1.6)
	_parachute_visual.position = Vector3(0, 3.2, 0.4)
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.97, 0.57, 0.16)
	mat.cull_mode = BaseMaterial3D.CULL_DISABLED
	_parachute_visual.material_override = mat
	_parachute_visual.visible = false
	add_child(_parachute_visual)


func _make_audio() -> void:
	_gunshot_player = AudioStreamPlayer3D.new()
	_gunshot_player.name = "GunshotAudio"
	if ResourceLoader.exists("res://gta/generated/audio/gunshot.wav"):
		_gunshot_player.stream = load("res://gta/generated/audio/gunshot.wav")
	_gunshot_player.volume_db = -8.0
	_gunshot_player.max_distance = 110.0
	add_child(_gunshot_player)
	_footstep_player = AudioStreamPlayer3D.new()
	_footstep_player.name = "FootstepAudio"
	if ResourceLoader.exists("res://gta/generated/audio/footstep.wav"):
		_footstep_player.stream = load("res://gta/generated/audio/footstep.wav")
	_footstep_player.volume_db = -21.0
	_footstep_player.max_distance = 18.0
	add_child(_footstep_player)


func _load_character_model() -> void:
	for child in _model_container.get_children(): child.queue_free()
	if ResourceLoader.exists(character_model_path):
		var resource := load(character_model_path)
		if resource is PackedScene:
			_model_instance = resource.instantiate() as Node3D
			if _model_instance != null:
				_model_container.add_child(_model_instance)
				var animation_nodes := _model_instance.find_children("*", "AnimationPlayer", true, false)
				if not animation_nodes.is_empty():
					_animation_player = animation_nodes[0] as AnimationPlayer
					for animation_name in ["player_idle", "player_walk"]:
						if _animation_player.has_animation(animation_name):
							_animation_player.get_animation(animation_name).loop_mode = Animation.LOOP_LINEAR
					if _animation_player.has_animation("player_idle"): _animation_player.play("player_idle")
				return
	var torso := MeshInstance3D.new()
	var body := CapsuleMesh.new()
	body.radius = 0.36
	body.height = 1.45
	torso.mesh = body
	torso.position.y = 0.98
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.14, 0.21, 0.32)
	torso.material_override = mat
	_model_container.add_child(torso)
	_model_instance = torso


func _refresh_weapon_model() -> void:
	if _weapon_instance != null and is_instance_valid(_weapon_instance):
		_weapon_instance.queue_free()
	_weapon_instance = null
	if _weapon_attachment != null:
		for child in _weapon_attachment.get_children(): child.queue_free()
	if current_weapon_id == "fists": return
	var model_id := current_weapon_id
	var path := "res://gta/generated/models/%s.glb" % model_id
	if ResourceLoader.exists(path):
		var resource := load(path)
		if resource is PackedScene:
			_weapon_instance = resource.instantiate() as Node3D
			if _weapon_instance != null:
				_weapon_socket.add_child(_weapon_instance)
				_refresh_attachment_visuals()
				return
	var fallback := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = Vector3(0.11, 0.15, 0.5)
	fallback.mesh = mesh
	fallback.position.z = -0.25
	var material := StandardMaterial3D.new()
	material.albedo_color = Color(0.17, 0.18, 0.2)
	fallback.material_override = material
	_weapon_socket.add_child(fallback)
	_weapon_instance = fallback
	_refresh_attachment_visuals()


func _refresh_attachment_visuals() -> void:
	if _weapon_attachment == null: return
	var mods: Dictionary = weapon_mods.get(current_weapon_id, {})
	if bool(mods.get("suppressor", false)):
		var suppressor := MeshInstance3D.new()
		var cylinder := CylinderMesh.new()
		cylinder.top_radius = 0.053
		cylinder.bottom_radius = 0.053
		cylinder.height = 0.25
		suppressor.mesh = cylinder
		suppressor.rotation.x = PI * 0.5
		suppressor.position = Vector3(0.0, 0.0, -0.58)
		var material := StandardMaterial3D.new()
		material.albedo_color = Color(0.08, 0.1, 0.12)
		material.metallic = 0.45
		suppressor.material_override = material
		_weapon_attachment.add_child(suppressor)
	if bool(mods.get("optic", false)):
		var sight := MeshInstance3D.new()
		var mesh := BoxMesh.new()
		mesh.size = Vector3(0.1, 0.075, 0.17)
		sight.mesh = mesh
		sight.position = Vector3(0.0, 0.12, -0.23)
		var mat := StandardMaterial3D.new()
		mat.albedo_color = Color(0.11, 0.12, 0.14)
		sight.material_override = mat
		_weapon_attachment.add_child(sight)


func _try_enter_vehicle() -> void:
	var closest: GTAVehicle = null
	var closest_distance := 6.0
	for node in get_tree().get_nodes_in_group("vehicles"):
		if node is GTAVehicle and not node.is_destroyed():
			var vehicle := node as GTAVehicle
			var distance := global_position.distance_to(vehicle.global_position)
			if distance < closest_distance:
				closest = vehicle
				closest_distance = distance
	if closest != null: closest.enter(self)


func set_world(new_world: Node) -> void:
	world = new_world


func set_vehicle(vehicle: GTAVehicle) -> void:
	current_vehicle = vehicle
	cover_active = false
	_collider.disabled = vehicle != null
	velocity = Vector3.ZERO
	if vehicle != null:
		parachute_deployed = false
		global_position = vehicle.global_position + Vector3.UP * 0.6
	vehicle_changed.emit(vehicle)


func damage(amount: float) -> void:
	if _dead or amount <= 0.0: return
	var remaining := amount
	if armor > 0.0:
		var absorbed := minf(armor, remaining * 0.65)
		armor -= absorbed
		remaining -= absorbed
	health = maxf(0.0, health - remaining)
	_camera_shake = minf(1.0, _camera_shake + amount * 0.013)
	damaged.emit(amount)
	health_changed.emit(health, max_health)
	if health <= 0.0:
		_dead = true
		if current_vehicle != null and is_instance_valid(current_vehicle):
			var out := current_vehicle.exit()
			global_position = out
		died.emit()
		get_tree().create_timer(3.0).timeout.connect(_auto_respawn)


func take_damage(amount: float, _attacker: Node = null) -> void:
	damage(amount)


func _auto_respawn() -> void:
	if is_inside_tree() and _dead:
		respawn(_spawn_position)


func respawn(position: Vector3) -> void:
	_dead = false
	health = max_health
	armor = maxf(armor, 25.0)
	global_position = position
	_spawn_position = position
	velocity = Vector3.ZERO
	parachute_deployed = false
	breath = 25.0
	_camera_pivot.global_position = position + Vector3.UP * 1.6
	health_changed.emit(health, max_health)


func give_weapon(id: String, extra_ammo: int = -1) -> void:
	if not WEAPONS.has(id): return
	if id not in owned_weapons: owned_weapons.append(id)
	if not weapon_state.has(id):
		var info: Dictionary = WEAPONS[id]
		weapon_state[id] = {"magazine": int(info["capacity"]), "reserve": int(info["reserve"])}
		weapon_mods[id] = {}
	elif extra_ammo > 0:
		weapon_state[id]["reserve"] = int(weapon_state[id]["reserve"]) + extra_ammo


func refill_ammo() -> void:
	for id in owned_weapons:
		var info: Dictionary = WEAPONS.get(id, WEAPONS["fists"])
		weapon_state[id] = {"magazine": _capacity_for(id), "reserve": int(info["reserve"])}
	_reload_remaining = 0.0
	if has_meta("reload_pending"): remove_meta("reload_pending")


func cycle_outfit() -> int:
	outfit_index = posmod(outfit_index + 1, OUTFIT_TINTS.size())
	_apply_outfit_tint()
	return outfit_index


func _apply_outfit_tint() -> void:
	var tint := OUTFIT_TINTS[outfit_index]
	var nodes: Array[Node] = [_model_container]
	while not nodes.is_empty():
		var node: Node = nodes.pop_back()
		for child in node.get_children(): nodes.append(child)
		if node is MeshInstance3D:
			var mesh_node := node as MeshInstance3D
			var part_name := mesh_node.name.to_lower()
			if not (part_name.contains("torso") or part_name.contains("leg") or part_name.contains("jacket") or part_name.contains("shirt") or part_name.contains("pants")):
				continue
			if mesh_node.mesh == null: continue
			for surface in range(mesh_node.mesh.get_surface_count()):
				var source: Material = mesh_node.get_active_material(surface)
				if source is StandardMaterial3D:
					var key := "outfit_base_%d" % surface
					if not mesh_node.has_meta(key): mesh_node.set_meta(key, (source as StandardMaterial3D).albedo_color)
					var material := source.duplicate() as StandardMaterial3D
					material.albedo_color = (mesh_node.get_meta(key) as Color) * tint
					mesh_node.set_surface_override_material(surface, material)


func set_weapon(id: String) -> void:
	if id not in owned_weapons or not WEAPONS.has(id): return
	if current_vehicle != null and id not in ["fists", "pistol", "heavy_pistol", "smg"]: return
	if current_weapon_id == id: return
	current_weapon_id = id
	_reload_remaining = 0.0
	if has_meta("reload_pending"): remove_meta("reload_pending")
	_refresh_weapon_model()
	weapon_changed.emit(id)


func _cycle_weapon(step: int) -> void:
	if owned_weapons.is_empty(): return
	var index := owned_weapons.find(current_weapon_id)
	for i in range(owned_weapons.size()):
		index = posmod(index + step, owned_weapons.size())
		if current_vehicle == null or owned_weapons[index] in ["fists", "pistol", "heavy_pistol", "smg"]:
			set_weapon(owned_weapons[index])
			return


func reload_weapon() -> void:
	if _reload_remaining > 0.0: return
	var info: Dictionary = WEAPONS.get(current_weapon_id, WEAPONS["fists"])
	if int(info["capacity"]) == 0: return
	var state: Dictionary = weapon_state[current_weapon_id]
	if int(state["magazine"]) >= _capacity_for(current_weapon_id) or int(state["reserve"]) <= 0: return
	_reload_remaining = float(info["reload"])
	set_meta("reload_pending", true)


func _complete_reload() -> void:
	var info: Dictionary = WEAPONS.get(current_weapon_id, WEAPONS["fists"])
	var state: Dictionary = weapon_state[current_weapon_id]
	var amount: int = mini(_capacity_for(current_weapon_id) - int(state["magazine"]), int(state["reserve"]))
	state["magazine"] = int(state["magazine"]) + amount
	state["reserve"] = int(state["reserve"]) - amount
	weapon_state[current_weapon_id] = state


func get_current_weapon_id() -> String:
	return current_weapon_id


func get_ammo() -> Dictionary:
	var info: Dictionary = WEAPONS.get(current_weapon_id, WEAPONS["fists"])
	var state: Dictionary = weapon_state.get(current_weapon_id, {"magazine": 0, "reserve": 0})
	return {"magazine": int(state["magazine"]), "reserve": int(state["reserve"]), "capacity": _capacity_for(current_weapon_id), "reloading": _reload_remaining > 0.0}


func _capacity_for(id: String) -> int:
	var info: Dictionary = WEAPONS.get(id, WEAPONS["fists"])
	var base: int = int(info["capacity"])
	if base == 0: return 0
	var mods: Dictionary = weapon_mods.get(id, {})
	return int(ceilf(base * 1.5)) if bool(mods.get("extended_mag", false)) else base


func apply_weapon_mod(mod_id: String) -> bool:
	if current_weapon_id not in owned_weapons or mod_id not in ["suppressor", "extended_mag", "grip", "optic"]:
		return false
	var info: Dictionary = WEAPONS.get(current_weapon_id, WEAPONS["fists"])
	if int(info["capacity"]) == 0: return false
	var mods: Dictionary = weapon_mods.get(current_weapon_id, {})
	if bool(mods.get(mod_id, false)): return false
	mods[mod_id] = true
	weapon_mods[current_weapon_id] = mods
	if mod_id == "extended_mag":
		var state: Dictionary = weapon_state[current_weapon_id]
		state["magazine"] = mini(_capacity_for(current_weapon_id), int(state["magazine"]) + int(ceilf(int(info["capacity"]) * 0.5)))
		weapon_state[current_weapon_id] = state
	if mod_id in ["suppressor", "optic"]: _refresh_weapon_model()
	return true


func get_weapon_mods(id: String = "") -> Dictionary:
	if id.is_empty(): id = current_weapon_id
	return (weapon_mods.get(id, {}) as Dictionary).duplicate()


func get_inventory_state() -> Dictionary:
	return {"owned_weapons": owned_weapons.duplicate(), "weapon_state": weapon_state.duplicate(true), "weapon_mods": weapon_mods.duplicate(true), "current_weapon_id": current_weapon_id, "health": health, "armor": armor, "cash": cash, "has_parachute": has_parachute, "scuba": scuba, "outfit_index": outfit_index}


func restore_inventory_state(data: Dictionary) -> void:
	if data.has("owned_weapons"):
		owned_weapons.clear()
		for id in data["owned_weapons"]:
			if WEAPONS.has(str(id)): owned_weapons.append(str(id))
	if "fists" not in owned_weapons: owned_weapons.push_front("fists")
	if data.has("weapon_state") and data["weapon_state"] is Dictionary:
		for id in data["weapon_state"].keys():
			if WEAPONS.has(str(id)):
				var state: Dictionary = data["weapon_state"][id]
				weapon_state[id] = {"magazine": maxi(0, int(state.get("magazine", 0))), "reserve": maxi(0, int(state.get("reserve", 0)))}
	if data.has("weapon_mods") and data["weapon_mods"] is Dictionary:
		for id in data["weapon_mods"].keys():
			if WEAPONS.has(str(id)) and data["weapon_mods"][id] is Dictionary:
				weapon_mods[id] = (data["weapon_mods"][id] as Dictionary).duplicate()
	health = clampf(float(data.get("health", health)), 1.0, max_health)
	armor = maxf(0.0, float(data.get("armor", armor)))
	cash = maxi(0, int(data.get("cash", cash)))
	has_parachute = bool(data.get("has_parachute", has_parachute))
	scuba = bool(data.get("scuba", scuba))
	outfit_index = posmod(int(data.get("outfit_index", outfit_index)), OUTFIT_TINTS.size())
	_apply_outfit_tint()
	set_weapon(str(data.get("current_weapon_id", "fists")))
	_refresh_weapon_model()
	health_changed.emit(health, max_health)
