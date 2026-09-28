extends SceneTree

var failures: Array[String] = []

func _initialize() -> void:
	call_deferred("_run")

func _check(condition: bool, description: String) -> void:
	if condition:
		print("SMOKE PASS: ", description)
	else:
		push_error("SMOKE FAIL: " + description)
		failures.append(description)

func _run() -> void:
	var packed: PackedScene = load("res://gta/scenes/bootstrap/main.tscn")
	var game: GTAGame = packed.instantiate() as GTAGame
	root.add_child(game)
	current_scene = game
	for i in 5:
		await physics_frame
	_check(game.world.get_road_lanes().size() >= 8, "road loops")
	_check(game.world.get_pedestrian_points().size() >= 100, "pedestrian points")
	_check(game.world.get_district_points().size() == 6, "six districts")
	_check(game.vehicles.size() >= 20, "vehicles spawned")
	_check(game.pedestrians.size() >= 20, "pedestrians spawned")
	_check(game.player.camera.current, "third-person camera active")
	var traffic_car: GTAVehicle = game.traffic_routes[0]["vehicle"]
	for route_info in game.traffic_routes:
		var candidate: GTAVehicle = route_info["vehicle"]
		if candidate.global_position.distance_to(game.player.global_position) < traffic_car.global_position.distance_to(game.player.global_position):
			traffic_car = candidate
	print("SMOKE NOTE: closest traffic distance=", traffic_car.global_position.distance_to(game.player.global_position))
	var traffic_start := traffic_car.global_position
	for i in 80:
		await physics_frame
	_check(traffic_car.global_position.distance_to(traffic_start) > 1.0, "civilian traffic moves")
	var walk_start := game.player.global_position
	Input.action_press("gta_move_forward")
	for i in 40:
		await physics_frame
	Input.action_release("gta_move_forward")
	_check(game.player.global_position.distance_to(walk_start) > 1.0, "player walks")
	var ammo_before: int = game.player.get_ammo()["magazine"]
	game.player._fire_weapon(GTAPlayer.WEAPONS["pistol"])
	for i in 3:
		await physics_frame
	_check(game.player.get_ammo()["magazine"] < ammo_before, "firearm path consumes ammo")
	var first: GTAVehicle = game.vehicles[0]
	game.player.global_position = first.global_position + Vector3(0.0, 0.0, 3.0)
	_check(first.enter(game.player), "enter vehicle")
	_check(game.player.current_vehicle == first, "driver possession")
	var drive_start := first.global_position
	Input.action_press("gta_move_forward")
	for i in 60:
		await physics_frame
	Input.action_release("gta_move_forward")
	_check(first.global_position.distance_to(drive_start) > 1.0, "player drives car")
	first.exit()
	_check(game.player.current_vehicle == null, "exit vehicle")
	var previous_count := game.vehicles.size()
	game.admin_action("spawn_vehicle", "airplane")
	_check(game.vehicles.size() == previous_count + 1 and game.vehicles[-1].kind == "airplane", "admin spawns airplane")
	game.admin_action("spawn_vehicle", "boat")
	_check(game.vehicles[-1].kind == "boat", "admin spawns boat")
	game.admin_action("wanted", "5")
	_check(game.wanted.stars == 5, "five-star wanted level")
	for i in 20:
		await physics_frame
	_check(game.police_units.size() > 0, "police response")
	game.wanted.set_officer_sight(false, game.player.global_position)
	game.wanted._process(4.0)
	_check(game.wanted.state == "search", "search after line of sight lost")
	game.wanted.clear()
	_check(game.wanted.stars == 0, "wanted clears")
	game.set_weather("rain")
	_check(game.rain.emitting, "rain emission")
	game.set_weather("clear")
	_check(not game.rain.emitting, "clear weather")
	var cash_before: int = game.player.cash
	game.shop_action("weapon_shop", "smg")
	_check(game.player.cash == cash_before - 600 and game.player.get_current_weapon_id() == "smg", "shop purchase equips weapon and costs money")
	game.shop_action("weapon_shop", "suppressor")
	_check(bool(game.player.get_weapon_mods().get("suppressor", false)), "weapon shop installs suppressor")
	game.player.health = 73.0
	game.save_game()
	game.player.health = 8.0
	game.load_game()
	_check(absf(game.player.health - 73.0) < 0.1, "save/load health")
	first.speed = 0.0
	first.velocity = Vector3.ZERO
	first.global_position = Vector3(60.0, 0.7, 0.0)
	first.engine_upgrade = 2
	first.health = 84.0
	_check(first.enter(game.player), "enter vehicle before save")
	game.save_game()
	first.global_position = Vector3(130.0, 0.7, 0.0)
	first.engine_upgrade = 0
	first.health = 12.0
	game.load_game()
	for i in 3:
		await physics_frame
	_check(game.player.current_vehicle == first and first.global_position.distance_to(Vector3(60.0, 0.7, 0.0)) < 2.0, "save/load occupied vehicle position")
	_check(first.engine_upgrade == 2 and absf(first.health - 84.0) < 0.1, "save/load occupied vehicle state")
	first.exit()
	game.hud.toggle_admin()
	_check(game.hud.admin_open and game.player.ui_blocked, "admin blocks movement")
	game.hud.close_menus()
	_check(not game.player.ui_blocked, "admin closes")
	game.hud.open_weapon_wheel()
	_check(game.hud.wheel_open and Engine.time_scale < 0.5, "weapon wheel slows time")
	game.hud.close_weapon_wheel()
	_check(not game.hud.wheel_open and absf(Engine.time_scale - 1.0) < 0.01, "weapon wheel equips and restores time")
	for i in 3:
		await physics_frame
	print("SMOKE RESULT: %d failures" % failures.size())
	game.queue_free()
	for i in 3:
		await process_frame
	quit(0 if failures.is_empty() else 1)
