class_name GTAHUD
extends CanvasLayer

var game
var health_label: Label
var armor_label: Label
var ammo_label: Label
var money_label: Label
var wanted_label: Label
var status_label: Label
var prompt_label: Label
var clock_label: Label
var speed_label: Label
var prompt_back: ColorRect
var map_widget: GTAMinimap
var full_map_widget: GTAMinimap
var admin_panel: PanelContainer
var shop_panel: PanelContainer
var pause_panel: PanelContainer
var weapon_wheel: GTAWeaponWheel
var admin_open: bool = false
var shop_open: bool = false
var pause_open: bool = false
var full_map_open: bool = false
var wheel_open: bool = false
var shop_kind: String = ""
var hint_text: String = ""
var hint_timer: float = 0.0

func _ready() -> void:
	_build_hud()
	_build_admin()
	_build_shop()
	_build_pause()
	weapon_wheel = GTAWeaponWheel.new()
	add_child(weapon_wheel)
	_set_menu_visibility()

func set_game(value) -> void:
	game = value
	map_widget.game = value
	full_map_widget.game = value
	if game.world != null:
		map_widget.set_map(game.world.get_road_lanes(), game.world.get_location_points())
		full_map_widget.set_map(game.world.get_road_lanes(), game.world.get_location_points())

func _process(delta: float) -> void:
	if game == null or game.player == null:
		return
	var player = game.player
	var ammo: Dictionary = player.get_ammo()
	health_label.text = "HEALTH  %03d" % int(player.health)
	armor_label.text = "ARMOR  %03d" % int(player.armor)
	ammo_label.text = "%s  %02d / %03d" % [player.get_current_weapon_id().to_upper(), int(ammo.get("magazine", 0)), int(ammo.get("reserve", 0))]
	money_label.text = "$%s" % str(int(player.cash))
	wanted_label.text = "★".repeat(game.wanted.stars) + "☆".repeat(5 - game.wanted.stars)
	wanted_label.modulate = Color(1.0, 0.64, 0.41) if game.wanted.state == "pursuit" else Color(0.83, 0.91, 0.98)
	status_label.text = game.wanted.state.to_upper() if game.wanted.stars > 0 else game.current_district_name()
	clock_label.text = "%02d:%02d   %s" % [int(game.time_of_day), int(fmod(game.time_of_day, 1.0) * 60.0), game.weather.to_upper()]
	if player.current_vehicle != null:
		var vehicle = player.current_vehicle
		speed_label.text = "%03d km/h  ·  %s" % [int(vehicle.get_speed_kmh()), vehicle.kind.to_upper()]
	else:
		speed_label.text = ""
	var location_hint: String = game.get_interaction_hint()
	prompt_label.text = location_hint if hint_timer <= 0.0 else hint_text
	prompt_back.visible = prompt_label.text != ""
	hint_timer = maxf(0.0, hint_timer - delta)
	map_widget.visible = not full_map_open

func notify(text_value: String, seconds: float = 3.0) -> void:
	hint_text = text_value
	hint_timer = seconds

func is_open() -> bool:
	return admin_open or shop_open or pause_open or full_map_open or wheel_open

func open_weapon_wheel() -> void:
	if game == null or game.player == null or is_open():
		return
	wheel_open = true
	weapon_wheel.open_for(game.player)
	Engine.time_scale = 0.32
	_set_menu_visibility()

func close_weapon_wheel() -> void:
	if not wheel_open:
		return
	var chosen := weapon_wheel.select_and_close()
	wheel_open = false
	Engine.time_scale = 1.0
	_set_menu_visibility()
	if chosen != "":
		notify("Equipped " + chosen.replace("_", " "), 1.2)

func toggle_admin() -> void:
	admin_open = not admin_open
	shop_open = false
	pause_open = false
	full_map_open = false
	_set_menu_visibility()

func toggle_pause() -> void:
	pause_open = not pause_open
	admin_open = false
	shop_open = false
	full_map_open = false
	_set_menu_visibility()

func toggle_map() -> void:
	full_map_open = not full_map_open
	admin_open = false
	shop_open = false
	pause_open = false
	_set_menu_visibility()

func open_shop(kind: String) -> void:
	shop_kind = kind
	shop_open = true
	admin_open = false
	pause_open = false
	full_map_open = false
	_refresh_shop()
	_set_menu_visibility()

func close_menus() -> void:
	if wheel_open:
		close_weapon_wheel()
	admin_open = false
	shop_open = false
	pause_open = false
	full_map_open = false
	_set_menu_visibility()

func _set_menu_visibility() -> void:
	admin_panel.visible = admin_open or full_map_open
	shop_panel.visible = shop_open
	pause_panel.visible = pause_open
	if game != null and game.player != null:
		game.player.ui_blocked = is_open()
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if is_open() else Input.MOUSE_MODE_CAPTURED
	if full_map_open:
		admin_panel.get_node("Body/Title").text = "HARBORLINE · MAP"
		admin_panel.get_node("Body/Subtitle").text = "M / Esc close · amber: public locations · blue: police"
		admin_panel.get_node("Body/Scroll").visible = false
		full_map_widget.visible = true
	else:
		admin_panel.get_node("Body/Title").text = "HARBORLINE · BENCHMARK CONSOLE"
		admin_panel.get_node("Body/Subtitle").text = "F10 close · all options are free-roam testing tools"
		admin_panel.get_node("Body/Scroll").visible = true
		full_map_widget.visible = false

func _build_hud() -> void:
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)
	var left_back := ColorRect.new()
	left_back.color = Color(0.025, 0.09, 0.13, 0.78)
	left_back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	left_back.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_LEFT)
	left_back.offset_left = 16
	left_back.offset_right = 264
	left_back.offset_top = -316
	left_back.offset_bottom = -14
	root.add_child(left_back)
	var right_back := ColorRect.new()
	right_back.color = Color(0.025, 0.09, 0.13, 0.78)
	right_back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	right_back.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	right_back.offset_left = -366
	right_back.offset_right = -15
	right_back.offset_top = -172
	right_back.offset_bottom = -14
	root.add_child(right_back)
	prompt_back = ColorRect.new()
	prompt_back.color = Color(0.025, 0.09, 0.13, 0.78)
	prompt_back.mouse_filter = Control.MOUSE_FILTER_IGNORE
	prompt_back.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	prompt_back.offset_left = -270
	prompt_back.offset_right = 270
	prompt_back.offset_top = -91
	prompt_back.offset_bottom = -43
	root.add_child(prompt_back)
	var top := HBoxContainer.new()
	top.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	top.offset_left = 24
	top.offset_top = 18
	top.offset_right = -24
	root.add_child(top)
	var title := Label.new()
	title.text = "HARBORLINE  /  FREE ROAM"
	title.add_theme_color_override("font_color", Color(0.98, 0.80, 0.49))
	title.add_theme_font_size_override("font_size", 19)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(title)
	clock_label = Label.new()
	clock_label.add_theme_font_size_override("font_size", 17)
	top.add_child(clock_label)
	var left := VBoxContainer.new()
	left.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_LEFT)
	left.offset_left = 24
	left.offset_bottom = -20
	left.offset_top = -310
	left.offset_right = 255
	root.add_child(left)
	map_widget = GTAMinimap.new()
	left.add_child(map_widget)
	health_label = _label(left, "HEALTH  100", 16)
	armor_label = _label(left, "ARMOR  000", 16)
	var right := VBoxContainer.new()
	right.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	right.offset_left = -350
	right.offset_right = -24
	right.offset_top = -165
	right.offset_bottom = -20
	right.alignment = BoxContainer.ALIGNMENT_END
	root.add_child(right)
	wanted_label = _label(right, "☆☆☆☆☆", 27)
	status_label = _label(right, "FREE ROAM", 14)
	ammo_label = _label(right, "PISTOL  12 / 60", 18)
	money_label = _label(right, "$2000", 18)
	speed_label = _label(right, "", 16)
	prompt_label = Label.new()
	prompt_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	prompt_label.offset_left = -265
	prompt_label.offset_right = 265
	prompt_label.offset_top = -85
	prompt_label.offset_bottom = -48
	prompt_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt_label.add_theme_font_size_override("font_size", 18)
	prompt_label.add_theme_color_override("font_color", Color(1.0, 0.83, 0.54))
	root.add_child(prompt_label)

func _build_admin() -> void:
	admin_panel = _menu_panel(Vector2(860, 640))
	var body := VBoxContainer.new()
	body.name = "Body"
	admin_panel.add_child(body)
	var title := _label(body, "HARBORLINE · BENCHMARK CONSOLE", 24)
	title.name = "Title"
	var subtitle := _label(body, "F10 close · all options are free-roam testing tools", 14)
	subtitle.name = "Subtitle"
	var scroll := ScrollContainer.new()
	scroll.name = "Scroll"
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_child(scroll)
	var items := VBoxContainer.new()
	items.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(items)
	full_map_widget = GTAMinimap.new()
	full_map_widget.custom_minimum_size = Vector2(540.0, 540.0)
	full_map_widget.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	full_map_widget.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_child(full_map_widget)
	_add_admin_section(items, "DISTRICTS", ["downtown", "residential", "industrial", "waterfront", "green", "airfield"], "teleport")
	_add_admin_section(items, "VEHICLES", ["car_compact", "car_sedan", "car_sport", "car_suv", "pickup", "van", "car_police", "motorcycle", "boat", "helicopter", "airplane"], "spawn_vehicle")
	_add_admin_section(items, "WEAPONS", ["fists", "knife", "bat", "pistol", "heavy_pistol", "smg", "shotgun", "rifle", "sniper", "refill_ammo"], "weapon")
	_add_admin_section(items, "WANTED", ["0", "1", "2", "3", "4", "5", "spawn_police"], "wanted")
	_add_admin_section(items, "WEATHER", ["clear", "cloudy", "rain", "fog", "storm"], "weather")
	_add_admin_section(items, "TIME", ["dawn", "noon", "sunset", "night"], "time")
	_add_admin_section(items, "SANDBOX", ["cash", "heal", "armor", "repair", "customize", "parachute", "scuba", "max_skills", "reset_skills", "taxi", "wildlife", "invulnerable", "traffic", "pedestrians", "fps", "coordinates", "save", "load"], "utility")

func _build_shop() -> void:
	shop_panel = _menu_panel(Vector2(520, 520))
	var body := VBoxContainer.new()
	body.name = "Body"
	shop_panel.add_child(body)
	var title := _label(body, "SHOP", 24)
	title.name = "Title"
	_label(body, "E / Esc close", 14)
	var scroll := ScrollContainer.new()
	scroll.name = "Scroll"
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_child(scroll)
	var items := VBoxContainer.new()
	items.name = "Items"
	items.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(items)

func _refresh_shop() -> void:
	if shop_panel == null:
		return
	shop_panel.get_node("Body/Title").text = shop_kind.to_upper().replace("_", " ")
	var items: VBoxContainer = shop_panel.get_node("Body/Scroll/Items")
	for child in items.get_children():
		child.queue_free()
	var offers: Array = []
	match shop_kind:
		"weapon_shop": offers = [["Knife · $90", "knife"], ["Bat · $110", "bat"], ["Pistol + ammo · $250", "pistol"], ["Heavy pistol · $450", "heavy_pistol"], ["SMG + ammo · $600", "smg"], ["Shotgun + ammo · $750", "shotgun"], ["Rifle + ammo · $1100", "rifle"], ["Sniper rifle · $1800", "sniper"], ["Ammo refill · $120", "ammo"], ["Suppressor · $240", "suppressor"], ["Extended magazine · $180", "extended_mag"], ["Stability grip · $160", "grip"], ["Optic · $320", "optic"], ["Armor · $160", "armor"]]
		"garage": offers = [["Repair vehicle · $200", "repair"], ["Next paint · $150", "paint"], ["Engine upgrade · $500", "engine"], ["Brake upgrade · $350", "brakes"]]
		"hospital": offers = [["Medical care · $120", "heal"], ["Armor · $160", "armor"]]
		"safehouse": offers = [["Save game", "save"], ["Rest until morning", "sleep"], ["Change outfit", "outfit"]]
		_: offers = [["Health · $120", "heal"], ["Armor · $160", "armor"]]
	for offer in offers:
		var button := Button.new()
		button.text = offer[0]
		button.custom_minimum_size.y = 42
		button.pressed.connect(func(): game.shop_action(shop_kind, offer[1]))
		items.add_child(button)

func _build_pause() -> void:
	pause_panel = _menu_panel(Vector2(460, 380))
	var body := VBoxContainer.new()
	pause_panel.add_child(body)
	_label(body, "HARBORLINE", 28)
	_label(body, "PAUSED · Esc resume", 17)
	_label(body, "WASD move · Mouse orbit · Shift sprint · Space jump", 14)
	_label(body, "F enter/exit · E interact · LMB fire · RMB aim", 14)
	_label(body, "R reload · 1–4 weapons · Tab weapon select", 14)
	_label(body, "M map · F10 benchmark menu · F5 save · F9 load", 14)
	var save_button := Button.new()
	save_button.text = "SAVE GAME"
	save_button.pressed.connect(func(): game.save_game())
	body.add_child(save_button)
	var resume := Button.new()
	resume.text = "RESUME"
	resume.pressed.connect(close_menus)
	body.add_child(resume)

func _menu_panel(dimensions: Vector2) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	panel.position = -dimensions * 0.5
	panel.size = dimensions
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.035, 0.105, 0.14, 0.96)
	style.border_color = Color(0.31, 0.71, 0.68)
	style.set_border_width_all(2)
	style.set_content_margin_all(18)
	panel.add_theme_stylebox_override("panel", style)
	add_child(panel)
	return panel

func _label(parent: Node, value: String, font_size: int) -> Label:
	var label := Label.new()
	label.text = value
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", Color(0.89, 0.94, 0.94))
	parent.add_child(label)
	return label

func _add_admin_section(parent: VBoxContainer, name_value: String, options: Array, action: String) -> void:
	_label(parent, name_value, 17)
	var grid := GridContainer.new()
	grid.columns = 4
	parent.add_child(grid)
	for option in options:
		var button := Button.new()
		button.text = str(option).replace("_", " ").to_upper()
		button.custom_minimum_size = Vector2(182, 38)
		button.pressed.connect(func(): game.admin_action(action, str(option)))
		grid.add_child(button)
