class_name GTAGame
extends Node3D

var world: GTAWorld
var player: GTAPlayer
var wanted: GTAWanted
var hud: GTAHUD
var vehicles: Array[GTAVehicle] = []
var pedestrians: Array[GTANPC] = []
var police_units: Array[GTANPC] = []
var traffic_routes: Array[Dictionary] = []
var police_cars: Array[GTAVehicle] = []
var locations: Dictionary = {}
var time_of_day: float = 13.5
var weather: String = "clear"
var invulnerable: bool = false
var traffic_enabled: bool = true
var pedestrians_enabled: bool = true
var wildlife_enabled: bool = true
var show_fps: bool = false
var show_coordinates: bool = false
var witness_report_pending: bool = false
var pending_report_kind: String = ""
var pending_report_position: Vector3 = Vector3.ZERO
var pending_witness: GTANPC
var skills: Dictionary = {"stamina": 0.0, "shooting": 0.0, "strength": 0.0, "stealth": 0.0, "driving": 0.0, "flying": 0.0, "lung_capacity": 0.0}
var director_timer: float = 0.0
var visual_timer: float = 0.0
var signal_clock: float = 0.0
var east_west_green: bool = true
var rain: CPUParticles3D
var ambient_life: GTAAmbientLife
var effect_audio: AudioStreamPlayer
var ambient_audio: AudioStreamPlayer

const SAVE_PATH := "user://harborline_save.json"
const VEHICLE_TYPES := ["car_compact", "car_sedan", "car_sport", "car_suv", "pickup", "van", "car_police", "motorcycle", "boat", "helicopter", "airplane"]
const PLAYER_SCENE: PackedScene = preload("res://gta/code/player/player.tscn")
const VEHICLE_SCENE: PackedScene = preload("res://gta/code/vehicles/vehicle.tscn")

func _ready() -> void:
	randomize()
	world = GTAWorld.new()
	world.name = "World"
	add_child(world)
	locations = world.get_location_points()
	for district in world.get_district_points():
		if not locations.has(district):
			locations[district] = world.get_district_points()[district]
	wanted = GTAWanted.new()
	wanted.name = "Wanted"
	add_child(wanted)
	player = PLAYER_SCENE.instantiate() as GTAPlayer
	player.name = "Player"
	add_child(player)
	player.global_position = world.get_spawn_position() + Vector3.UP * 0.3
	player.set_world(world)
	ambient_life = GTAAmbientLife.new()
	ambient_life.name = "AmbientLife"
	add_child(ambient_life)
	ambient_life.setup(world, player)
	player.fired.connect(_on_player_fired)
	player.crime.connect(_on_player_crime)
	player.died.connect(_on_player_died)
	hud = GTAHUD.new()
	hud.name = "HUD"
	add_child(hud)
	hud.set_game(self)
	_make_rain()
	_make_audio()
	_populate_world()
	_apply_lighting()
	hud.notify("Welcome to Harborline · Free roam · F10 benchmark menu", 6.0)

func _exit_tree() -> void:
	if ambient_audio != null:
		ambient_audio.stop()
		ambient_audio.stream = null
	if effect_audio != null:
		effect_audio.stop()
		effect_audio.stream = null

func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey):
		return
	var key := event as InputEventKey
	if key.keycode == KEY_TAB:
		if key.pressed and not key.echo:
			hud.open_weapon_wheel()
		elif not key.pressed:
			hud.close_weapon_wheel()
		get_viewport().set_input_as_handled()
		return
	if not key.pressed or key.echo:
		return
	match key.keycode:
		KEY_F10:
			hud.toggle_admin()
			get_viewport().set_input_as_handled()
		KEY_ESCAPE:
			if hud.is_open():
				hud.close_menus()
			else:
				hud.toggle_pause()
			get_viewport().set_input_as_handled()
		KEY_M:
			hud.toggle_map()
			get_viewport().set_input_as_handled()
		KEY_E:
			if hud.shop_open:
				hud.close_menus()
			elif not hud.is_open():
				_interact()
			get_viewport().set_input_as_handled()
		KEY_F5:
			save_game()
		KEY_F9:
			load_game()

func _process(delta: float) -> void:
	time_of_day = fmod(time_of_day + delta / 95.0, 24.0)
	visual_timer += delta
	if visual_timer > 0.5:
		visual_timer = 0.0
		_apply_lighting()
	if rain != null:
		rain.global_position = player.global_position + Vector3.UP * 17.0
	if player.current_vehicle != null:
		var kind: String = player.current_vehicle.kind
		var skill: String = "flying" if kind in ["helicopter", "airplane"] else "driving"
		skills[skill] = minf(100.0, float(skills[skill]) + delta * 0.022)
	elif Input.is_key_pressed(KEY_SHIFT) and player.velocity.length() > 2.0:
		skills["stamina"] = minf(100.0, float(skills["stamina"]) + delta * 0.019)
	if show_fps and Engine.get_frames_drawn() % 30 == 0:
		hud.notify("FPS %d · NPC %d · Traffic %d" % [Engine.get_frames_per_second(), pedestrians.size(), traffic_routes.size()], 0.6)
	if show_coordinates and Engine.get_frames_drawn() % 30 == 0:
		hud.notify("Position %s · District %s" % [str(player.global_position.round()), current_district_name()], 0.6)

func _physics_process(delta: float) -> void:
	signal_clock = fmod(signal_clock + delta, 28.0)
	var next_east_west_green := signal_clock < 14.0
	if next_east_west_green != east_west_green:
		east_west_green = next_east_west_green
		world.set_traffic_signal_state(east_west_green)
	director_timer += delta
	if director_timer < 0.35:
		return
	director_timer = 0.0
	_update_traffic()
	_update_police()
	_maintain_population()

func _populate_world() -> void:
	spawn_vehicle("car_compact", world.get_spawn_position() + Vector3(11.0, 0.55, -7.0))
	var roads: Array = world.get_road_lanes()
	var count := 0
	for lane_index in range(roads.size()):
		var lane = roads[lane_index]
		if lane.size() < 4:
			continue
		for offset in range(3):
			if count >= 18:
				break
			var index: int = (offset * int(lane.size() / 3) + 1) % lane.size()
			var kind: String = ["car_compact", "car_sedan", "car_sport", "car_suv", "pickup", "van"][count % 6]
			var car := spawn_vehicle(kind, lane[index] + Vector3.UP * 0.55)
			if car != null:
				traffic_routes.append({"vehicle": car, "lane": lane_index, "waypoint": (index + 1) % lane.size()})
				count += 1
	for key in ["downtown", "residential", "industrial", "airfield"]:
		if locations.has(key):
			var parked_kind: String = ["car_sedan", "car_compact", "car_sport", "car_suv", "pickup", "van", "motorcycle"][vehicles.size() % 7]
			spawn_vehicle(parked_kind, locations[key] + Vector3(9.0, 0.7, 9.0))
	if locations.has("waterfront"):
		spawn_vehicle("boat", locations.get("boat_spawn", locations["waterfront"] + Vector3(8.0, 0.0, 68.0)))
	if locations.has("airfield"):
		spawn_vehicle("helicopter", locations.get("helipad", locations["airfield"]) + Vector3.UP * 0.7)
		spawn_vehicle("airplane", locations.get("runway", locations["airfield"]) + Vector3.UP * 0.7)
	var points: Array[Vector3] = world.get_pedestrian_points()
	for index in range(mini(32, points.size())):
		var point := points[(index * 7) % points.size()]
		_spawn_pedestrian(point, points)

func spawn_vehicle(kind: String, position: Vector3) -> GTAVehicle:
	if kind not in VEHICLE_TYPES:
		return null
	var vehicle := VEHICLE_SCENE.instantiate() as GTAVehicle
	vehicle.name = "Vehicle_%s_%d" % [kind, vehicles.size()]
	vehicle.setup(kind)
	add_child(vehicle)
	vehicle.global_position = position
	vehicle.destroyed.connect(_on_vehicle_destroyed.bind(vehicle))
	vehicles.append(vehicle)
	return vehicle

func _spawn_pedestrian(position: Vector3, points: Array[Vector3]) -> GTANPC:
	var npc := GTANPC.new()
	npc.setup("civilian")
	add_child(npc)
	npc.global_position = position + Vector3.UP * 0.15
	npc.set_player(player)
	npc.set_patrol_points(points)
	npc.killed.connect(_on_npc_killed)
	pedestrians.append(npc)
	return npc

func spawn_police() -> void:
	var level := maxi(1, wanted.stars)
	var position := _distant_spawn(55.0, 100.0)
	var npc := GTANPC.new()
	npc.setup("tactical" if level >= 4 and police_units.size() % 3 == 0 else "police")
	add_child(npc)
	npc.global_position = position + Vector3.UP * 0.2
	npc.set_player(player)
	npc.set_alert(true, player.global_position)
	npc.attacked_player.connect(_on_police_attack)
	npc.killed.connect(_on_npc_killed)
	police_units.append(npc)

func _update_traffic() -> void:
	var roads: Array = world.get_road_lanes()
	for info in traffic_routes:
		var vehicle: GTAVehicle = info["vehicle"]
		if not is_instance_valid(vehicle) or vehicle.driver != null:
			continue
		var lane_index: int = info["lane"]
		if lane_index >= roads.size():
			continue
		var lane = roads[lane_index]
		if lane.size() == 0:
			continue
		var waypoint: int = info["waypoint"]
		var destination: Vector3 = lane[waypoint]
		if vehicle.global_position.distance_to(destination) < 7.0:
			waypoint = (waypoint + 1) % lane.size()
			info["waypoint"] = waypoint
			destination = lane[waypoint]
		var close_to_player := vehicle.global_position.distance_to(player.global_position) < 155.0
		var target_speed := 13.0 if traffic_enabled and close_to_player else 0.0
		if target_speed > 0.0:
			if _traffic_must_stop(vehicle, destination):
				target_speed = 0.0
			else:
				target_speed = minf(target_speed, _safe_following_speed(vehicle, destination))
		vehicle.set_ai_target(destination, target_speed)
	for car in police_cars:
		if is_instance_valid(car):
			car.set_ai_target(player.global_position if wanted.state == "pursuit" else wanted.last_known, 22.0)


func _traffic_must_stop(vehicle: GTAVehicle, destination: Vector3) -> bool:
	var direction := destination - vehicle.global_position
	direction.y = 0.0
	if direction.length_squared() < 1.0:
		return false
	if absf(direction.x) > absf(direction.z) * 1.5:
		if east_west_green:
			return false
		var sign_x := signf(direction.x)
		for road_z in GTAWorld.ROAD_Z:
			if float(road_z) >= 210.0 or absf(vehicle.global_position.z - float(road_z)) > 7.5:
				continue
			for road_x in GTAWorld.ROAD_X:
				if float(road_x) == 240.0 and float(road_z) < 0.0:
					continue
				var ahead: float = (float(road_x) - vehicle.global_position.x) * sign_x
				if ahead >= 8.0 and ahead <= 32.0:
					return true
	else:
		if not east_west_green:
			return false
		var sign_z := signf(direction.z)
		for road_x in GTAWorld.ROAD_X:
			if absf(vehicle.global_position.x - float(road_x)) > 7.5:
				continue
			for road_z in GTAWorld.ROAD_Z:
				if float(road_z) >= 210.0 or (float(road_x) == 240.0 and float(road_z) < 0.0):
					continue
				var ahead: float = (float(road_z) - vehicle.global_position.z) * sign_z
				if ahead >= 8.0 and ahead <= 32.0:
					return true
	return false


func _safe_following_speed(vehicle: GTAVehicle, destination: Vector3) -> float:
	var direction := destination - vehicle.global_position
	direction.y = 0.0
	if direction.length_squared() < 1.0:
		return 13.0
	direction = direction.normalized()
	var limit := 13.0
	for other in vehicles:
		if not is_instance_valid(other) or other == vehicle or other.health <= 0.0:
			continue
		var separation := other.global_position - vehicle.global_position
		separation.y = 0.0
		var ahead := separation.dot(direction)
		if ahead < 0.0 or ahead > 18.0:
			continue
		var lateral := (separation - direction * ahead).length()
		if lateral < 3.1:
			limit = minf(limit, maxf(0.0, (ahead - 5.5) * 1.6))
	return limit

func _update_police() -> void:
	police_units = police_units.filter(func(npc): return is_instance_valid(npc) and npc.health > 0.0)
	police_cars = police_cars.filter(func(car): return is_instance_valid(car) and car.health > 0.0)
	var seen := false
	var sight_distance := 30.0 if player.stealth_active and player.current_vehicle == null else 60.0
	for officer in police_units:
		if officer.global_position.distance_to(player.global_position) < sight_distance and officer._sees_player():
			seen = true
		officer.set_alert(wanted.state == "pursuit", player.global_position if seen else wanted.last_known)
	for car in police_cars:
		if car.global_position.distance_to(player.global_position) < 35.0 and _line_of_sight(car.global_position + Vector3.UP * 2.0, player.global_position + Vector3.UP):
			seen = true
	wanted.set_officer_sight(seen, player.global_position)
	var desired_units := mini(12, wanted.stars * 2 + (2 if wanted.stars >= 4 else 0))
	if police_units.size() < desired_units and wanted.stars > 0:
		spawn_police()
	if wanted.stars > 0 and police_cars.size() < mini(3, wanted.stars):
		var car := spawn_vehicle("car_police", _distant_spawn(75.0, 125.0) + Vector3.UP * 0.55)
		if car != null:
			police_cars.append(car)

func _maintain_population() -> void:
	pedestrians = pedestrians.filter(func(npc): return is_instance_valid(npc) and npc.health > 0.0)
	if pedestrians_enabled and pedestrians.size() < 28:
		var points: Array[Vector3] = world.get_pedestrian_points()
		if not points.is_empty():
			var point := points[randi() % points.size()]
			if point.distance_to(player.global_position) < 125.0 and point.distance_to(player.global_position) > 35.0:
				_spawn_pedestrian(point, points)
	for npc in pedestrians:
		npc.active = pedestrians_enabled

func _distant_spawn(minimum: float, maximum: float) -> Vector3:
	var points: Array[Vector3] = world.get_pedestrian_points()
	var candidates: Array[Vector3] = []
	for point in points:
		var distance := point.distance_to(player.global_position)
		var in_camera := player.camera != null and player.camera.is_position_in_frustum(point + Vector3.UP * 1.5)
		if distance >= minimum and distance <= maximum and not in_camera:
			candidates.append(point)
	if not candidates.is_empty():
		return candidates[randi() % candidates.size()]
	return player.global_position + Vector3(randf_range(-maximum, maximum), 0.0, randf_range(-maximum, maximum))

func _line_of_sight(from: Vector3, to: Vector3) -> bool:
	var query := PhysicsRayQueryParameters3D.create(from, to)
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	return hit.is_empty() or hit.get("collider") == player or hit.get("collider") == player.current_vehicle

func _on_player_fired(position: Vector3, kind: String) -> void:
	register_crime("suppressed_gunfire" if kind == "suppressed" else "gunfire", position)
	skills["shooting"] = minf(100.0, float(skills["shooting"]) + 0.10)
	for npc in pedestrians:
		if is_instance_valid(npc) and npc.global_position.distance_to(position) < (14.0 if kind == "suppressed" else 36.0):
			npc.panic(position)

func _on_player_crime(kind: String, position: Vector3) -> void:
	register_crime(kind, position)

func _on_npc_killed(npc: GTANPC, attacker: Node) -> void:
	if attacker == player:
		register_crime("police_assault" if npc.kind in ["police", "tactical"] else "murder", npc.global_position)
		player.cash += randi_range(10, 55) if npc.kind == "civilian" else 0
		if npc.kind == "civilian":
			skills["strength"] = minf(100.0, float(skills["strength"]) + 0.3)
	for civilian in pedestrians:
		if is_instance_valid(civilian) and civilian.global_position.distance_to(npc.global_position) < 30.0:
			civilian.panic(npc.global_position)

func _on_police_attack(amount: float) -> void:
	if not invulnerable:
		player.damage(amount)

func _on_player_died() -> void:
	hud.notify("HOSPITAL · You were rescued", 5.0)
	wanted.clear()
	var spawn: Vector3 = locations.get("hospital", world.get_spawn_position())
	get_tree().create_timer(2.0).timeout.connect(func(): player.respawn(spawn + Vector3.UP * 0.5))

func _on_vehicle_destroyed(vehicle: GTAVehicle) -> void:
	_spawn_explosion(vehicle.global_position)
	register_crime("explosion", vehicle.global_position)
	for npc in pedestrians + police_units:
		if is_instance_valid(npc) and npc.global_position.distance_to(vehicle.global_position) < 10.0:
			npc.take_damage(80.0, player if vehicle.driver == player else null)
	if vehicle.driver == player:
		player.damage(85.0)

func register_crime(kind: String, position: Vector3) -> void:
	var officer_witness := false
	for officer in police_units:
		if is_instance_valid(officer) and officer.global_position.distance_to(position) < 85.0 and _line_of_sight(officer.global_position + Vector3.UP, position + Vector3.UP):
			officer_witness = true
			break
	if officer_witness:
		wanted.report_crime(kind, position, true)
		hud.notify("Officer witnessed the crime · Police alerted", 2.0)
		return
	var witness: GTANPC
	for npc in pedestrians:
		var witness_radius := 65.0 if kind in ["gunfire", "explosion"] else (17.0 if kind == "suppressed_gunfire" else (22.0 if player.stealth_active else 38.0))
		if is_instance_valid(npc) and npc.health > 0.0 and npc.global_position.distance_to(position) < witness_radius and _line_of_sight(npc.global_position + Vector3.UP, position + Vector3.UP):
			witness = npc
			break
	if witness == null:
		hud.notify("No witness saw the incident", 1.7)
		return
	witness.panic(position)
	if witness_report_pending:
		if float(GTAWanted.CRIME_HEAT.get(kind, 10.0)) > float(GTAWanted.CRIME_HEAT.get(pending_report_kind, 10.0)):
			pending_report_kind = kind
			pending_report_position = position
			pending_witness = witness
		return
	witness_report_pending = true
	pending_report_kind = kind
	pending_report_position = position
	pending_witness = witness
	hud.notify("Witness is calling police", 2.4)
	get_tree().create_timer(2.4).timeout.connect(_complete_witness_report)

func _complete_witness_report() -> void:
	if witness_report_pending and is_instance_valid(pending_witness) and pending_witness.health > 0.0:
		wanted.report_crime(pending_report_kind, pending_report_position, true)
		hud.notify("Witness report received · Police dispatched", 2.0)
	witness_report_pending = false
	pending_witness = null

func get_interaction_hint() -> String:
	if player == null:
		return ""
	if player.current_vehicle != null and locations.has("garage") and player.global_position.distance_to(locations["garage"]) < 13.0:
		return "[E] Open modification garage"
	for key in ["weapon_shop", "garage", "hospital", "safehouse", "gas_station"]:
		if locations.has(key) and player.global_position.distance_to(locations[key]) < 7.0:
			return "[E] " + key.replace("_", " ").capitalize()
	return ""

func _interact() -> void:
	for key in ["weapon_shop", "garage", "hospital", "safehouse", "gas_station"]:
		var radius := 13.0 if key == "garage" and player.current_vehicle != null else 7.0
		if locations.has(key) and player.global_position.distance_to(locations[key]) < radius:
			hud.open_shop(key)
			return

func shop_action(_shop: String, action: String) -> void:
	var prices := {"knife": 90, "bat": 110, "pistol": 250, "heavy_pistol": 450, "smg": 600, "shotgun": 750, "rifle": 1100, "sniper": 1800, "ammo": 120, "suppressor": 240, "extended_mag": 180, "grip": 160, "optic": 320, "armor": 160, "repair": 200, "paint": 150, "engine": 500, "brakes": 350, "heal": 120, "save": 0, "sleep": 0, "outfit": 80}
	if action in ["repair", "paint", "engine", "brakes"] and player.current_vehicle == null:
		hud.notify("Bring a vehicle into the garage", 2.5)
		return
	if action == "engine" and player.current_vehicle.engine_upgrade >= 3:
		hud.notify("Engine is fully upgraded", 2.0)
		return
	if action == "brakes" and player.current_vehicle.brake_upgrade >= 3:
		hud.notify("Brakes are fully upgraded", 2.0)
		return
	var cost: int = prices.get(action, 0)
	if player.cash < cost:
		hud.notify("Not enough cash", 2.5)
		return
	if action in ["suppressor", "extended_mag", "grip", "optic"]:
		if not player.apply_weapon_mod(action):
			hud.notify("Modification unavailable for this weapon", 2.5)
			return
		player.cash -= cost
		hud.notify("Installed: " + action.replace("_", " "), 2.5)
		return
	player.cash -= cost
	match action:
		"knife", "bat", "pistol", "heavy_pistol", "smg", "shotgun", "rifle", "sniper":
			player.give_weapon(action)
			player.set_weapon(action)
		"ammo": player.refill_ammo()
		"armor": player.armor = 100.0
		"heal": player.health = 100.0
		"repair":
			if player.current_vehicle != null:
				player.current_vehicle.repair()
		"paint":
			if player.current_vehicle != null:
				player.current_vehicle.next_paint()
		"engine":
			if player.current_vehicle != null:
				player.current_vehicle.engine_upgrade += 1
		"brakes":
			if player.current_vehicle != null:
				player.current_vehicle.brake_upgrade += 1
		"save": save_game()
		"sleep": time_of_day = 8.0
		"outfit": player.cycle_outfit()
	hud.notify("Service complete: " + action.replace("_", " "), 2.5)

func admin_action(section: String, option: String) -> void:
	match section:
		"teleport":
			if locations.has(option):
				if player.current_vehicle != null:
					player.current_vehicle.global_position = locations[option] + Vector3.UP * 1.5
				else:
					player.global_position = locations[option] + Vector3.UP * 1.5
		"spawn_vehicle":
			var spawn := player.global_position - player.global_transform.basis.z * 8.0
			if option == "boat":
				spawn = locations.get("boat_spawn", Vector3(-80.0, 0.8, 258.0))
			elif option == "helicopter":
				spawn = locations.get("helipad", Vector3(183.0, 0.2, -36.0))
			elif option == "airplane":
				spawn = locations.get("runway", Vector3(219.0, 0.2, -152.0))
			spawn_vehicle(option, spawn + Vector3.UP * 0.6)
		"weapon":
			if option == "refill_ammo": player.refill_ammo()
			else:
				player.give_weapon(option)
				player.set_weapon(option)
		"wanted":
			if option == "spawn_police": spawn_police()
			else: wanted.set_level(int(option))
		"weather": set_weather(option)
		"time":
			match option:
				"dawn": time_of_day = 6.0
				"noon": time_of_day = 12.0
				"sunset": time_of_day = 19.0
				"night": time_of_day = 1.0
		"utility": _admin_utility(option)
	hud.notify("Benchmark: " + option.replace("_", " "), 1.5)

func _admin_utility(option: String) -> void:
	match option:
		"cash": player.cash += 10000
		"heal": player.health = 100.0
		"armor": player.armor = 100.0
		"repair":
			if player.current_vehicle != null: player.current_vehicle.repair()
		"customize": hud.open_shop("garage")
		"parachute": player.has_parachute = true
		"scuba": player.scuba = not player.scuba
		"max_skills":
			for key in skills: skills[key] = 100.0
		"reset_skills":
			for key in skills: skills[key] = 0.0
		"taxi": spawn_vehicle("car_sedan", player.global_position + Vector3(5.0, 0.6, 0.0))
		"wildlife":
			wildlife_enabled = not wildlife_enabled
			ambient_life.set_enabled(wildlife_enabled)
		"invulnerable": invulnerable = not invulnerable
		"traffic": traffic_enabled = not traffic_enabled
		"pedestrians": pedestrians_enabled = not pedestrians_enabled
		"fps": show_fps = not show_fps
		"coordinates": show_coordinates = not show_coordinates
		"save": save_game()
		"load": load_game()

func current_district_name() -> String:
	var result := "OPEN CITY"
	var nearest := INF
	for key in ["downtown", "residential", "industrial", "waterfront", "green", "airfield"]:
		if locations.has(key):
			var distance: float = player.global_position.distance_to(locations[key])
			if distance < nearest:
				nearest = distance
				result = key.to_upper()
	return result

func save_game() -> void:
	var data := {"version": 2, "position": [player.global_position.x, player.global_position.y, player.global_position.z], "health": player.health, "armor": player.armor, "cash": player.cash, "inventory": player.get_inventory_state(), "time": time_of_day, "weather": weather, "skills": skills}
	if player.current_vehicle != null and is_instance_valid(player.current_vehicle):
		var vehicle := player.current_vehicle
		data["vehicle"] = {"name": vehicle.name, "kind": vehicle.kind, "position": [vehicle.global_position.x, vehicle.global_position.y, vehicle.global_position.z], "rotation_y": vehicle.rotation.y, "health": vehicle.health, "paint": vehicle.paint_color.to_html(false), "engine_upgrade": vehicle.engine_upgrade, "brake_upgrade": vehicle.brake_upgrade}
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file == null:
		hud.notify("Save failed", 2.0)
		return
	file.store_string(JSON.stringify(data))
	file.close()
	hud.notify("Free roam saved", 2.0)

func load_game() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		hud.notify("No save found", 2.0)
		return
	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file == null:
		return
	var parsed = JSON.parse_string(file.get_as_text())
	if not (parsed is Dictionary):
		hud.notify("Save is damaged", 2.0)
		return
	var p: Array = parsed.get("position", [])
	var saved_position := player.global_position
	if p.size() == 3:
		saved_position = Vector3(float(p[0]), float(p[1]), float(p[2]))
	if player.current_vehicle != null and is_instance_valid(player.current_vehicle):
		player.current_vehicle.exit()
	player.respawn(saved_position)
	player.health = clampf(float(parsed.get("health", 100.0)), 1.0, 100.0)
	player.armor = clampf(float(parsed.get("armor", 0.0)), 0.0, 100.0)
	player.cash = int(parsed.get("cash", 2000))
	player.restore_inventory_state(parsed.get("inventory", {}))
	time_of_day = clampf(float(parsed.get("time", 12.0)), 0.0, 24.0)
	set_weather(str(parsed.get("weather", "clear")))
	var saved_skills: Dictionary = parsed.get("skills", {})
	for key in skills:
		skills[key] = clampf(float(saved_skills.get(key, 0.0)), 0.0, 100.0)
	var saved_vehicle = parsed.get("vehicle", {})
	if saved_vehicle is Dictionary and not saved_vehicle.is_empty():
		_restore_saved_vehicle(saved_vehicle)
	wanted.clear()
	hud.notify("Free roam loaded", 2.0)

func _restore_saved_vehicle(data: Dictionary) -> void:
	var kind := str(data.get("kind", ""))
	var spawn_kind := "car_" + kind if "car_" + kind in VEHICLE_TYPES else kind
	if spawn_kind not in VEHICLE_TYPES:
		return
	var position_data: Array = data.get("position", [])
	if position_data.size() != 3:
		return
	var position := Vector3(float(position_data[0]), float(position_data[1]), float(position_data[2]))
	var vehicle := get_node_or_null(str(data.get("name", ""))) as GTAVehicle
	if vehicle == null or vehicle.kind != kind:
		vehicle = spawn_vehicle(spawn_kind, position)
	if vehicle == null:
		return
	vehicle.stop_ai()
	if vehicle.is_destroyed():
		vehicle.repair()
	vehicle.global_position = position
	vehicle.rotation.y = float(data.get("rotation_y", vehicle.rotation.y))
	vehicle.engine_upgrade = clampi(int(data.get("engine_upgrade", 0)), 0, 3)
	vehicle.brake_upgrade = clampi(int(data.get("brake_upgrade", 0)), 0, 3)
	vehicle.paint_color = Color.from_string(str(data.get("paint", vehicle.paint_color.to_html(false))), vehicle.paint_color)
	vehicle._apply_paint()
	vehicle.health = clampf(float(data.get("health", vehicle.max_health)), 1.0, vehicle.max_health)
	vehicle.enter(player)

func set_weather(value: String) -> void:
	weather = value if value in ["clear", "cloudy", "rain", "fog", "storm"] else "clear"
	if rain != null:
		rain.emitting = weather in ["rain", "storm"]
	_apply_lighting()

func _apply_lighting() -> void:
	if world == null:
		return
	var sun: DirectionalLight3D = world.get_node_or_null("Sun")
	var atmosphere: WorldEnvironment = world.get_node_or_null("Atmosphere")
	var solar := maxf(0.06, sin((time_of_day - 6.0) / 24.0 * TAU))
	var night := clampf((0.32 - solar) / 0.26, 0.0, 1.0)
	if sun != null:
		sun.rotation_degrees = Vector3(-25.0 - solar * 50.0, -35.0, 0.0)
		sun.light_energy = (0.025 + solar * 1.34) * (0.65 if weather in ["cloudy", "rain", "storm"] else 1.0)
		sun.light_color = Color(1.0, 0.72 + solar * 0.24, 0.55 + solar * 0.4).lerp(Color(0.42, 0.60, 0.82), night)
	if atmosphere != null and atmosphere.environment != null:
		var env := atmosphere.environment
		env.ambient_light_energy = 0.16 + solar * 0.36
		env.fog_enabled = weather in ["fog", "storm"]
		env.fog_density = 0.015 if weather == "fog" else 0.006
		var sky_material := env.sky.sky_material as ProceduralSkyMaterial
		if sky_material != null:
			sky_material.sky_top_color = Color("#6593a4").lerp(Color("#0b1730"), night)
			sky_material.sky_horizon_color = Color("#d0d7ca").lerp(Color("#222d43"), night)
			sky_material.ground_bottom_color = Color("#5d7772").lerp(Color("#101b2d"), night)
	if world.has_method("set_night_lights"):
		world.call("set_night_lights", night > 0.6)

func _make_rain() -> void:
	rain = CPUParticles3D.new()
	rain.name = "Rain"
	rain.amount = 500
	rain.lifetime = 1.2
	rain.emission_shape = CPUParticles3D.EMISSION_SHAPE_BOX
	rain.emission_box_extents = Vector3(26.0, 1.0, 26.0)
	rain.direction = Vector3.DOWN
	rain.initial_velocity_min = 23.0
	rain.initial_velocity_max = 28.0
	rain.gravity = Vector3(0.0, -10.0, 0.0)
	var drop := SphereMesh.new()
	drop.radius = 0.025
	drop.height = 0.26
	rain.mesh = drop
	rain.emitting = false
	add_child(rain)

func _make_audio() -> void:
	effect_audio = AudioStreamPlayer.new()
	add_child(effect_audio)
	ambient_audio = AudioStreamPlayer.new()
	add_child(ambient_audio)
	if DisplayServer.get_name() == "headless":
		return
	var path := "res://gta/generated/audio/ambience.wav"
	if ResourceLoader.exists(path):
		ambient_audio.stream = load(path)
		ambient_audio.volume_db = -22.0
		ambient_audio.finished.connect(_restart_ambience)
		ambient_audio.play()

func _restart_ambience() -> void:
	if is_inside_tree() and ambient_audio != null and ambient_audio.stream != null:
		ambient_audio.play()

func _play_effect(id: String) -> void:
	var path := "res://gta/generated/audio/%s.wav" % id
	if ResourceLoader.exists(path):
		effect_audio.stream = load(path)
		effect_audio.volume_db = -7.0
		effect_audio.play()

func _spawn_flash(position: Vector3) -> void:
	var light := OmniLight3D.new()
	light.light_color = Color(1.0, 0.75, 0.37)
	light.light_energy = 3.0
	light.omni_range = 3.0
	add_child(light)
	light.global_position = position + Vector3.UP * 1.4
	get_tree().create_timer(0.08).timeout.connect(light.queue_free)

func _spawn_explosion(position: Vector3) -> void:
	_play_effect("explosion")
	var root := Node3D.new()
	add_child(root)
	root.global_position = position
	var mesh := MeshInstance3D.new()
	var sphere := SphereMesh.new()
	sphere.radius = 1.0
	sphere.height = 2.0
	mesh.mesh = sphere
	var material := StandardMaterial3D.new()
	material.albedo_color = Color(1.0, 0.39, 0.10, 0.8)
	material.emission_enabled = true
	material.emission = Color(1.0, 0.23, 0.04)
	material.emission_energy_multiplier = 5.0
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mesh.material_override = material
	root.add_child(mesh)
	var tween := create_tween()
	tween.tween_property(root, "scale", Vector3.ONE * 7.0, 0.45)
	tween.tween_callback(root.queue_free)
