class_name GTAAmbientLife
extends Node3D

## Small, distance-limited wildlife layer. All creatures are visual MultiMesh
## instances; no per-animal Nodes, collision bodies or physics ticks are used.

const BIRD_COUNT := 24
const FISH_COUNT := 26
const BIRD_DRAW_DISTANCE := 175.0
const FISH_DRAW_DISTANCE := 52.0
const TICK_INTERVAL := 0.08

var _world: GTAWorld
var _player: GTAPlayer
var _enabled := true
var _rng := RandomNumberGenerator.new()
var _time := 0.0
var _accumulator := 0.0
var _birds: Array[Dictionary] = []
var _fish: Array[Dictionary] = []
var _danger_position := Vector3.ZERO
var _danger_remaining := 0.0
var _active_birds := 0
var _active_fish := 0

var _bird_bodies: MultiMesh
var _bird_wings: MultiMesh
var _fish_bodies: MultiMesh
var _fish_tails: MultiMesh


func setup(world: GTAWorld, player: GTAPlayer) -> void:
	_world = world
	if is_instance_valid(_player) and _player.fired.is_connected(_on_player_fired):
		_player.fired.disconnect(_on_player_fired)
	_player = player
	if is_instance_valid(_player) and not _player.fired.is_connected(_on_player_fired):
		_player.fired.connect(_on_player_fired)


func set_enabled(value: bool) -> void:
	_enabled = value
	visible = value
	set_process(value)


func get_stats() -> Dictionary:
	return {
		"birds_total": BIRD_COUNT,
		"fish_total": FISH_COUNT,
		"birds_active": _active_birds,
		"fish_active": _active_fish,
	}


func _ready() -> void:
	_rng.seed = 60591
	_create_visuals()
	_spawn_agents()
	for i in BIRD_COUNT:
		_hide_bird(i)
	for i in FISH_COUNT:
		_hide_fish(i)
	set_enabled(_enabled)


func _process(delta: float) -> void:
	if not is_instance_valid(_player) or not is_instance_valid(_world):
		return
	_accumulator += minf(delta, 0.2)
	if _accumulator < TICK_INTERVAL:
		return
	var step := _accumulator
	_accumulator = 0.0
	_time += step
	_danger_remaining = maxf(0.0, _danger_remaining - step)
	_update_birds(step)
	_update_fish(step)


func _on_player_fired(position: Vector3, _kind: String) -> void:
	_danger_position = position
	_danger_remaining = 3.2


func _player_position() -> Vector3:
	if _player.is_inside_tree():
		return _player.global_position
	return _player.position


func _create_visuals() -> void:
	var bird_material := StandardMaterial3D.new()
	bird_material.albedo_color = Color("#f3ecdc")
	bird_material.roughness = 0.83
	bird_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	var fish_material := StandardMaterial3D.new()
	fish_material.albedo_color = Color("#e8ae70")
	fish_material.roughness = 0.55
	fish_material.cull_mode = BaseMaterial3D.CULL_DISABLED
	var tail_material := StandardMaterial3D.new()
	tail_material.albedo_color = Color("#d47e65")
	tail_material.roughness = 0.64
	tail_material.cull_mode = BaseMaterial3D.CULL_DISABLED

	var body_mesh := SphereMesh.new()
	body_mesh.radial_segments = 7
	body_mesh.rings = 4
	_bird_bodies = _make_multimesh(body_mesh, BIRD_COUNT, bird_material, "Bird bodies")
	_bird_wings = _make_multimesh(_make_wing_mesh(), BIRD_COUNT * 2, bird_material, "Bird wings")
	var fish_mesh := SphereMesh.new()
	fish_mesh.radial_segments = 7
	fish_mesh.rings = 4
	_fish_bodies = _make_multimesh(fish_mesh, FISH_COUNT, fish_material, "Fish bodies")
	_fish_tails = _make_multimesh(_make_tail_mesh(), FISH_COUNT, tail_material, "Fish tails")


func _make_multimesh(mesh: Mesh, count: int, material: Material, display_name: String) -> MultiMesh:
	var multimesh := MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.mesh = mesh
	multimesh.instance_count = count
	var renderer := MultiMeshInstance3D.new()
	renderer.name = display_name
	renderer.multimesh = multimesh
	renderer.material_override = material
	renderer.custom_aabb = AABB(Vector3(-300.0, -4.0, -300.0), Vector3(600.0, 55.0, 620.0))
	add_child(renderer)
	return multimesh


func _make_wing_mesh() -> ArrayMesh:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface.add_vertex(Vector3(0.0, 0.0, -0.09))
	surface.add_vertex(Vector3(1.13, 0.06, -0.28))
	surface.add_vertex(Vector3(0.75, 0.0, 0.36))
	surface.generate_normals()
	return surface.commit()


func _make_tail_mesh() -> ArrayMesh:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface.add_vertex(Vector3(0.0, 0.0, 0.38))
	surface.add_vertex(Vector3(-0.26, 0.13, 0.87))
	surface.add_vertex(Vector3(0.26, 0.13, 0.87))
	surface.generate_normals()
	return surface.commit()


func _spawn_agents() -> void:
	var flock_homes := [
		Vector3(25.0, 22.0, -20.0),
		Vector3(-188.0, 19.0, -185.0),
		Vector3(-70.0, 24.0, 236.0),
	]
	for i in BIRD_COUNT:
		var flock := int(i / 8)
		var home: Vector3 = flock_homes[flock]
		var phase := _rng.randf_range(0.0, TAU)
		var radius := _rng.randf_range(5.0, 15.0)
		var position := home + Vector3(cos(phase) * radius, _rng.randf_range(-3.0, 3.0), sin(phase) * radius)
		_birds.append({
			"position": position,
			"velocity": Vector3(-sin(phase), 0.0, cos(phase)) * 4.5,
			"home": home,
			"phase": phase,
			"radius": radius,
			"speed": _rng.randf_range(5.2, 8.3),
		})
	for i in FISH_COUNT:
		var home := Vector3(_rng.randf_range(-235.0, 235.0), _rng.randf_range(-2.6, -0.9), _rng.randf_range(232.0, 289.0))
		var phase := _rng.randf_range(0.0, TAU)
		_fish.append({
			"position": home,
			"velocity": Vector3.FORWARD * 1.5,
			"home": home,
			"phase": phase,
			"radius": _rng.randf_range(3.0, 12.0),
			"speed": _rng.randf_range(1.5, 3.2),
		})


func _update_birds(step: float) -> void:
	var player_pos := _player_position()
	_active_birds = 0
	for i in BIRD_COUNT:
		var bird: Dictionary = _birds[i]
		var position: Vector3 = bird["position"]
		var home: Vector3 = bird["home"]
		var phase: float = bird["phase"]
		var radius: float = bird["radius"]
		var speed: float = bird["speed"]
		var distance := position.distance_to(player_pos)
		if distance > BIRD_DRAW_DISTANCE:
			_hide_bird(i)
			continue
		_active_birds += 1
		var orbit_angle := phase + _time * (speed / maxf(radius, 3.0)) * 0.48
		var target := home + Vector3(cos(orbit_angle) * radius, sin(orbit_angle * 1.7 + phase) * 2.2, sin(orbit_angle) * radius)
		if distance < 27.0:
			var away := (position - player_pos).normalized()
			target += Vector3(away.x * 24.0, 12.0, away.z * 24.0)
		if _danger_remaining > 0.0 and position.distance_to(_danger_position) < 58.0:
			var away_from_shot := (position - _danger_position).normalized()
			target += Vector3(away_from_shot.x * 37.0, 17.0, away_from_shot.z * 37.0)
		var velocity: Vector3 = bird["velocity"]
		var desired := (target - position).normalized() * speed
		velocity = velocity.lerp(desired, minf(1.0, step * 1.7))
		position += velocity * step
		position.x = clampf(position.x, -283.0, 283.0)
		position.z = clampf(position.z, -281.0, 298.0)
		position.y = clampf(position.y, 9.0, 42.0)
		bird["velocity"] = velocity
		bird["position"] = position
		_birds[i] = bird
		var forward := velocity.normalized()
		if forward.length_squared() < 0.01:
			forward = Vector3.FORWARD
		var facing := Basis.looking_at(forward, Vector3.UP)
		var bank := clampf(velocity.x * 0.015, -0.16, 0.16)
		facing = facing.rotated(Vector3.FORWARD, bank)
		_bird_bodies.set_instance_transform(i, Transform3D(facing.scaled(Vector3(0.2, 0.13, 0.42)), position))
		var flap := sin(_time * 7.8 + phase * 2.0) * 0.38
		_bird_wings.set_instance_transform(i, Transform3D(facing * Basis(Vector3.BACK, flap), position))
		_bird_wings.set_instance_transform(i + BIRD_COUNT, Transform3D(facing * Basis(Vector3.BACK, -flap).scaled(Vector3(-1.0, 1.0, 1.0)), position))


func _hide_bird(index: int) -> void:
	var hidden := Transform3D(Basis().scaled(Vector3.ZERO), Vector3.ZERO)
	_bird_bodies.set_instance_transform(index, hidden)
	_bird_wings.set_instance_transform(index, hidden)
	_bird_wings.set_instance_transform(index + BIRD_COUNT, hidden)


func _update_fish(step: float) -> void:
	var player_pos := _player_position()
	_active_fish = 0
	for i in FISH_COUNT:
		var fish: Dictionary = _fish[i]
		var position: Vector3 = fish["position"]
		var distance := position.distance_to(player_pos)
		if distance > FISH_DRAW_DISTANCE:
			_hide_fish(i)
			continue
		_active_fish += 1
		var home: Vector3 = fish["home"]
		var phase: float = fish["phase"]
		var radius: float = fish["radius"]
		var speed: float = fish["speed"]
		var orbit_angle := phase + _time * speed / maxf(radius, 3.0)
		var target := home + Vector3(cos(orbit_angle) * radius, sin(orbit_angle * 2.0) * 0.32, sin(orbit_angle) * radius)
		if distance < 13.0 and _world.is_water_position(player_pos):
			var away := position - player_pos
			away.y = 0.0
			target += away.normalized() * 14.0
		if _danger_remaining > 0.0 and position.distance_to(_danger_position) < 22.0:
			var away_from_shot := position - _danger_position
			away_from_shot.y = 0.0
			target += away_from_shot.normalized() * 13.0
		var velocity: Vector3 = fish["velocity"]
		var desired := (target - position).normalized() * speed
		velocity = velocity.lerp(desired, minf(1.0, step * 1.4))
		position += velocity * step
		position.x = clampf(position.x, -286.0, 286.0)
		position.z = clampf(position.z, 226.0, 295.0)
		position.y = clampf(position.y, -3.7, -0.52)
		fish["position"] = position
		fish["velocity"] = velocity
		_fish[i] = fish
		var forward := Vector3(velocity.x, 0.0, velocity.z).normalized()
		if forward.length_squared() < 0.01:
			forward = Vector3.FORWARD
		var facing := Basis.looking_at(forward, Vector3.UP)
		_fish_bodies.set_instance_transform(i, Transform3D(facing.scaled(Vector3(0.18, 0.15, 0.55)), position))
		var tail_sway := sin(_time * 10.0 + phase) * 0.22
		_fish_tails.set_instance_transform(i, Transform3D(facing * Basis(Vector3.UP, tail_sway), position))


func _hide_fish(index: int) -> void:
	var hidden := Transform3D(Basis().scaled(Vector3.ZERO), Vector3.ZERO)
	_fish_bodies.set_instance_transform(index, hidden)
	_fish_tails.set_instance_transform(index, hidden)
