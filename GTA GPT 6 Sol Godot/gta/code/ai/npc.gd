class_name GTANPC
extends CharacterBody3D

signal killed(npc: GTANPC, attacker: Node)
signal attacked_player(amount: float)

var kind: String = "civilian"
var health: float = 75.0
var mode: String = "wander"
var player: Node3D
var patrol_points: Array[Vector3] = []
var target: Vector3 = Vector3.ZERO
var danger: Vector3 = Vector3.ZERO
var fear_time: float = 0.0
var attack_timer: float = 0.0
var retarget_timer: float = 0.0
var life_timer: float = 0.0
var body_visual: Node3D
var active: bool = true
var _animation_player: AnimationPlayer
var _idle_clip: String = ""
var _walk_clip: String = ""
var _playing_clip: String = ""

const CIVILIAN_VISUALS := ["npc_civilian", "npc_casual", "npc_business"]

func _ready() -> void:
	add_to_group("npcs")
	add_to_group("damageable")
	var shape := CollisionShape3D.new()
	var capsule := CapsuleShape3D.new()
	capsule.radius = 0.33
	capsule.height = 1.7
	shape.shape = capsule
	shape.position.y = 0.86
	add_child(shape)
	body_visual = Node3D.new()
	body_visual.name = "Appearance"
	add_child(body_visual)
	_load_appearance()
	retarget_timer = randf_range(0.0, 2.0)

func setup(npc_kind: String, visual_path: String = "") -> void:
	kind = npc_kind
	if visual_path != "":
		set_meta("visual_path", visual_path)
	if kind == "police" or kind == "tactical":
		health = 105.0 if kind == "police" else 160.0
	elif kind == "shopkeeper":
		health = 75.0
	else:
		health = 70.0
	if is_node_ready():
		_load_appearance()

func _load_appearance() -> void:
	if body_visual == null:
		return
	for child in body_visual.get_children():
		child.queue_free()
	_animation_player = null
	_idle_clip = ""
	_walk_clip = ""
	_playing_clip = ""
	var path: String = str(get_meta("visual_path", ""))
	if path == "":
		var model_id := "npc_police"
		if kind not in ["police", "tactical"]:
			model_id = CIVILIAN_VISUALS[randi() % CIVILIAN_VISUALS.size()]
		path = "res://gta/generated/models/%s.glb" % model_id
		set_meta("visual_path", path)
	if ResourceLoader.exists(path):
		var scene: PackedScene = load(path)
		if scene != null:
			var model := scene.instantiate()
			body_visual.add_child(model)
			var animation_nodes := model.find_children("*", "AnimationPlayer", true, false)
			if not animation_nodes.is_empty():
				_animation_player = animation_nodes[0] as AnimationPlayer
				var stem := path.get_file().get_basename()
				_idle_clip = stem + "_idle"
				_walk_clip = stem + "_walk"
				for clip in [_idle_clip, _walk_clip]:
					if _animation_player.has_animation(clip):
						_animation_player.get_animation(clip).loop_mode = Animation.LOOP_LINEAR
				_play_clip(_idle_clip)
			return
	var torso := MeshInstance3D.new()
	var mesh := CapsuleMesh.new()
	mesh.radius = 0.29
	mesh.height = 1.5
	torso.mesh = mesh
	torso.position.y = 0.92
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.11, 0.27, 0.40) if kind in ["police", "tactical"] else Color(0.75, 0.47, 0.33)
	torso.material_override = mat
	body_visual.add_child(torso)

func set_patrol_points(points: Array[Vector3]) -> void:
	patrol_points = points
	_choose_target()

func set_player(value: Node3D) -> void:
	player = value

func set_alert(alert: bool, last_known: Vector3) -> void:
	if kind in ["police", "tactical"]:
		mode = "pursue" if alert else "search"
		target = last_known

func panic(from_position: Vector3) -> void:
	if kind in ["police", "tactical"] or health <= 0.0:
		return
	danger = from_position
	fear_time = randf_range(7.0, 13.0)
	mode = "flee"

func take_damage(amount: float, attacker: Node = null) -> void:
	if health <= 0.0:
		return
	health -= amount
	if attacker is Node3D:
		panic((attacker as Node3D).global_position)
	if health <= 0.0:
		mode = "dead"
		velocity = Vector3.ZERO
		body_visual.rotation.z = 1.45
		body_visual.position.y = 0.38
		set_collision_layer_value(1, false)
		set_collision_mask_value(1, false)
		if _animation_player != null:
			_animation_player.stop()
		killed.emit(self, attacker)

func _physics_process(delta: float) -> void:
	if mode == "dead":
		life_timer += delta
		if life_timer > 22.0:
			queue_free()
		return
	if not active:
		return
	if player != null and global_position.distance_to(player.global_position) > 170.0:
		return
	attack_timer = maxf(0.0, attack_timer - delta)
	retarget_timer -= delta
	if not is_on_floor():
		velocity.y -= 24.0 * delta
	else:
		velocity.y = -0.1
	var move_direction := Vector3.ZERO
	var speed := 2.1
	if mode == "flee":
		fear_time -= delta
		move_direction = global_position - danger
		move_direction.y = 0.0
		speed = 5.3
		if fear_time <= 0.0:
			mode = "wander"
			_choose_target()
	elif mode == "pursue" and player != null:
		target = player.global_position
		move_direction = target - global_position
		move_direction.y = 0.0
		speed = 4.3
		if move_direction.length() < 27.0 and attack_timer <= 0.0 and _sees_player():
			attack_timer = 0.9 if kind == "tactical" else 1.35
			attacked_player.emit(8.0 if kind == "tactical" else 5.0)
	elif mode == "search" or mode == "wander":
		move_direction = target - global_position
		move_direction.y = 0.0
		if move_direction.length() < 1.5 or retarget_timer < -8.0:
			_choose_target()
	if move_direction.length_squared() > 0.2:
		var normalized := move_direction.normalized()
		velocity.x = move_toward(velocity.x, normalized.x * speed, delta * 16.0)
		velocity.z = move_toward(velocity.z, normalized.z * speed, delta * 16.0)
		body_visual.rotation.y = lerp_angle(body_visual.rotation.y, atan2(-normalized.x, -normalized.z), delta * 8.0)
	else:
		velocity.x = move_toward(velocity.x, 0.0, delta * 12.0)
		velocity.z = move_toward(velocity.z, 0.0, delta * 12.0)
	move_and_slide()
	if _animation_player != null:
		if Vector2(velocity.x, velocity.z).length() > 0.45:
			_animation_player.speed_scale = clampf(speed / 2.1, 0.8, 2.0)
			_play_clip(_walk_clip)
		else:
			_animation_player.speed_scale = 1.0
			_play_clip(_idle_clip)

func _play_clip(clip: String) -> void:
	if _animation_player == null or clip == _playing_clip or not _animation_player.has_animation(clip):
		return
	_animation_player.play(clip, 0.18)
	_playing_clip = clip

func _choose_target() -> void:
	retarget_timer = randf_range(3.0, 8.0)
	if patrol_points.is_empty():
		target = global_position + Vector3(randf_range(-20.0, 20.0), 0.0, randf_range(-20.0, 20.0))
		return
	var options: Array[Vector3] = []
	for point in patrol_points:
		if point.distance_to(global_position) < 50.0 and point.distance_to(global_position) > 8.0:
			options.append(point)
	if options.is_empty():
		target = patrol_points[randi() % patrol_points.size()]
	else:
		target = options[randi() % options.size()]

func _sees_player() -> bool:
	if player == null:
		return false
	var origin := global_position + Vector3.UP * 1.5
	var end := player.global_position + Vector3.UP * 1.2
	var query := PhysicsRayQueryParameters3D.create(origin, end)
	query.exclude = [get_rid()]
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	return hit.is_empty() or hit.get("collider") == player
