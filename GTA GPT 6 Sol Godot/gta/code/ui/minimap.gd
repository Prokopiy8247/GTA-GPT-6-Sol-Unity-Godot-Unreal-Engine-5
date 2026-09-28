class_name GTAMinimap
extends Control

var game
var road_lines: Array = []
var landmarks: Dictionary = {}

func _ready() -> void:
	if custom_minimum_size == Vector2.ZERO:
		custom_minimum_size = Vector2(216.0, 216.0)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func set_map(lines: Array, points: Dictionary) -> void:
	road_lines = lines
	landmarks = points
	queue_redraw()

func _process(_delta: float) -> void:
	queue_redraw()

func _draw() -> void:
	var bounds := Rect2(Vector2.ZERO, size)
	draw_rect(bounds, Color(0.04, 0.10, 0.14, 0.90), true)
	draw_rect(bounds, Color(0.29, 0.66, 0.65, 0.9), false, 2.0)
	draw_rect(Rect2(Vector2(0, size.y * 0.86), Vector2(size.x, size.y * 0.14)), Color(0.05, 0.30, 0.40, 0.9), true)
	for lane in road_lines:
		if lane.size() < 2:
			continue
		for index in range(lane.size()):
			var a: Vector2 = _map(lane[index])
			var b: Vector2 = _map(lane[(index + 1) % lane.size()])
			draw_line(a, b, Color(0.46, 0.53, 0.54, 0.75), 2.0, true)
	for key in landmarks:
		var point: Vector3 = landmarks[key]
		draw_circle(_map(point), 2.8, Color(0.98, 0.76, 0.34))
	if game == null or game.player == null:
		return
	for npc in game.police_units:
		if is_instance_valid(npc) and npc.health > 0.0:
			draw_circle(_map(npc.global_position), 3.5, Color(0.35, 0.62, 0.98))
	var center := _map(game.player.global_position)
	var yaw: float = game.player.rotation.y
	var forward := Vector2(-sin(yaw), -cos(yaw))
	var right := Vector2(-forward.y, forward.x)
	var triangle := PackedVector2Array([center + forward * 9.0, center - forward * 6.0 + right * 5.0, center - forward * 6.0 - right * 5.0])
	draw_colored_polygon(triangle, Color(1.0, 0.91, 0.62))

func _map(position: Vector3) -> Vector2:
	return Vector2((position.x + 300.0) / 600.0 * size.x, (position.z + 300.0) / 600.0 * size.y)
