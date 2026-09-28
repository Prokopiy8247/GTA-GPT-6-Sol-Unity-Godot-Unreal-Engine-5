class_name GTAWeaponWheel
extends Control

var player: GTAPlayer
var selected_index: int = 0
var mouse_moved: bool = false
var initial_mouse_position: Vector2
var center: Vector2
var radius: float = 185.0

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	visible = false

func open_for(value: GTAPlayer) -> void:
	player = value
	visible = true
	mouse_moved = false
	selected_index = maxi(0, player.owned_weapons.find(player.current_weapon_id))
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	center = get_viewport_rect().size * 0.5
	initial_mouse_position = get_viewport().get_mouse_position()
	queue_redraw()

func select_and_close() -> String:
	visible = false
	if player == null or player.owned_weapons.is_empty():
		return ""
	var id: String = player.owned_weapons[clampi(selected_index, 0, player.owned_weapons.size() - 1)]
	player.set_weapon(id)
	queue_redraw()
	return id

func _process(_delta: float) -> void:
	if not visible or player == null:
		return
	center = get_viewport_rect().size * 0.5
	var displacement := get_viewport().get_mouse_position() - center
	if get_viewport().get_mouse_position().distance_to(initial_mouse_position) > 16.0:
		mouse_moved = true
	if mouse_moved and displacement.length() > 28.0 and not player.owned_weapons.is_empty():
		var angle := fposmod(displacement.angle() + PI * 0.5 + PI / float(player.owned_weapons.size()), TAU)
		selected_index = mini(player.owned_weapons.size() - 1, int(floor(angle / TAU * player.owned_weapons.size())))
	queue_redraw()

func _draw() -> void:
	if not visible or player == null or player.owned_weapons.is_empty():
		return
	draw_rect(Rect2(Vector2.ZERO, size), Color(0.015, 0.045, 0.065, 0.7), true)
	var count := player.owned_weapons.size()
	for index in count:
		var start_angle := -PI * 0.5 + TAU * (float(index) - 0.5) / float(count)
		var end_angle := -PI * 0.5 + TAU * (float(index) + 0.5) / float(count)
		var points := PackedVector2Array([center])
		for step in range(19):
			var angle := lerpf(start_angle, end_angle, float(step) / 18.0)
			points.append(center + Vector2(cos(angle), sin(angle)) * radius)
		var tint := Color(0.24, 0.60, 0.60, 0.93) if index == selected_index else Color(0.06, 0.17, 0.22, 0.95)
		draw_colored_polygon(points, tint)
		draw_line(center, points[1], Color(0.41, 0.78, 0.76), 2.0)
		var middle := -PI * 0.5 + TAU * float(index) / float(count)
		var label_pos := center + Vector2(cos(middle), sin(middle)) * radius * 0.72
		var weapon: String = player.owned_weapons[index]
		var label := weapon.to_upper().replace("_", " ")
		var font: Font = ThemeDB.fallback_font
		var text_width := font.get_string_size(label, HORIZONTAL_ALIGNMENT_LEFT, -1, 16).x
		draw_string(font, label_pos - Vector2(text_width * 0.5, -5.0), label, HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color(1.0, 0.92, 0.77) if index == selected_index else Color(0.82, 0.90, 0.90))
	draw_arc(center, radius, 0.0, TAU, 64, Color(0.41, 0.78, 0.76), 3.0)
	draw_circle(center, radius * 0.29, Color(0.025, 0.09, 0.12))
	var current := player.owned_weapons[selected_index].to_upper().replace("_", " ")
	var font: Font = ThemeDB.fallback_font
	var width := font.get_string_size(current, HORIZONTAL_ALIGNMENT_LEFT, -1, 18).x
	draw_string(font, center + Vector2(-width * 0.5, 7.0), current, HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color(1.0, 0.87, 0.58))
	draw_string(font, center + Vector2(-102.0, radius + 46.0), "TAB  SELECT  ·  RELEASE  EQUIP", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color(0.88, 0.94, 0.94))
