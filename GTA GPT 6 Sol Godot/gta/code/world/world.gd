class_name GTAWorld
extends Node3D

## A deterministic, 600 m coastal city.  X is east and Z is south.
## Runtime generation keeps the scene small while making the road graph reusable.

const MODEL_ROOT := "res://gta/generated/models/"
const ROAD_X := [-240.0, -160.0, -80.0, 0.0, 80.0, 160.0, 240.0]
const ROAD_Z := [-240.0, -160.0, -80.0, 0.0, 80.0, 160.0, 210.0]
const WATER_EDGE := 220.0

var _rng := RandomNumberGenerator.new()
var _materials: Dictionary = {}
var _assets: Dictionary = {}
var _batches: Dictionary = {}
var _unit_box: BoxMesh
var _roads: Node3D
var _structures: Node3D
var _props: Node3D
var _road_lanes: Array[PackedVector3Array] = []
var _pedestrian_points: Array[Vector3] = []
var _district_points: Dictionary = {}
var _location_points: Dictionary = {}
var _night_lights: Array[OmniLight3D] = []
var _night_lights_active := false
var _traffic_indicators: Array[Dictionary] = []
var _east_west_green := true
var _signal_on_red: StandardMaterial3D
var _signal_on_green: StandardMaterial3D
var _signal_off_red: StandardMaterial3D
var _signal_off_green: StandardMaterial3D


func _ready() -> void:
	_rng.seed = 60260923
	_unit_box = BoxMesh.new()
	_unit_box.size = Vector3.ONE
	_create_palette()
	_load_optional_assets()
	_create_containers()
	_create_atmosphere()
	_build_terrain()
	_build_roads()
	_build_neighborhoods()
	_build_airfield()
	_build_waterfront()
	_build_street_furniture()
	_build_route_data()
	_flush_batches()


func get_spawn_position() -> Vector3:
	return Vector3(22.0, 0.35, -22.0)


func get_district_points() -> Dictionary:
	return _district_points.duplicate()


func get_road_lanes() -> Array[PackedVector3Array]:
	return _road_lanes.duplicate()


func get_pedestrian_points() -> Array[Vector3]:
	return _pedestrian_points.duplicate()


func get_location_points() -> Dictionary:
	return _location_points.duplicate()


func is_water_position(pos: Vector3) -> bool:
	return pos.z >= WATER_EDGE and pos.x >= -302.0 and pos.x <= 302.0


func set_night_lights(active: bool) -> void:
	_night_lights_active = active
	for lamp in _night_lights:
		lamp.visible = active


func set_traffic_signal_state(east_west_green: bool) -> void:
	if _east_west_green == east_west_green:
		return
	_east_west_green = east_west_green
	for indicator in _traffic_indicators:
		_update_traffic_indicator(indicator)


func get_night_light_count() -> int:
	return _night_lights.size()


func _create_containers() -> void:
	_roads = Node3D.new()
	_roads.name = "Roads"
	add_child(_roads)
	_structures = Node3D.new()
	_structures.name = "Structures"
	add_child(_structures)
	_props = Node3D.new()
	_props.name = "StreetFurniture"
	add_child(_props)


func _create_palette() -> void:
	_materials["asphalt"] = _mat(Color("#252f39"), 0.0, 0.94)
	_materials["sidewalk"] = _mat(Color("#929d99"), 0.0, 0.88)
	_materials["white"] = _mat(Color("#cbc8b4"), 0.0, 0.8)
	_materials["yellow"] = _mat(Color("#efbb5c"), 0.0, 0.8)
	_materials["soil"] = _mat(Color("#9fb69b"), 0.0, 0.96)
	_materials["lawn"] = _mat(Color("#7caa85"), 0.0, 0.98)
	_materials["sand"] = _mat(Color("#d9c69f"), 0.0, 0.96)
	_materials["seabed"] = _mat(Color("#456b71"), 0.0, 1.0)
	_materials["sea_wall"] = _mat(Color("#818f90"), 0.0, 0.94)
	_materials["wood"] = _mat(Color("#8c6952"), 0.0, 0.85)
	_materials["glass"] = _mat(Color("#406873"), 0.18, 0.26)
	_materials["lit_glass"] = _mat(Color("#d3a976"), 0.1, 0.35, 0.8)
	_materials["ink"] = _mat(Color("#263b43"), 0.0, 0.85)
	_materials["steel"] = _mat(Color("#57717a"), 0.2, 0.48)
	_materials["warm"] = _mat(Color("#e3966d"), 0.0, 0.82)
	_materials["coral"] = _mat(Color("#c9635b"), 0.0, 0.8)
	_materials["cream"] = _mat(Color("#b9b09d"), 0.0, 0.9)
	_materials["blue"] = _mat(Color("#7297a7"), 0.0, 0.86)
	_materials["tower"] = _mat(Color("#9eb2b2"), 0.02, 0.78)
	_materials["leaf"] = _mat(Color("#416e65"), 0.0, 0.96)
	_materials["leaf_light"] = _mat(Color("#679685"), 0.0, 0.96)
	_materials["tree_trunk"] = _mat(Color("#67554d"), 0.0, 0.94)
	_materials["lamp"] = _mat(Color("#ffe1a0"), 0.0, 0.48, 2.1)
	_materials["runway"] = _mat(Color("#37424a"), 0.0, 0.9)
	_materials["red"] = _mat(Color("#da5c56"), 0.0, 0.48, 1.4)
	_materials["green"] = _mat(Color("#8aba8b"), 0.0, 0.48, 1.4)
	_signal_on_red = _mat(Color("#f24c46"), 0.0, 0.35, 3.0)
	_signal_on_green = _mat(Color("#52e99b"), 0.0, 0.35, 3.0)
	_signal_off_red = _mat(Color("#542b2b"), 0.0, 0.85)
	_signal_off_green = _mat(Color("#28483c"), 0.0, 0.85)
	var water := _mat(Color(0.12, 0.43, 0.51, 0.78), 0.14, 0.22)
	water.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	water.cull_mode = BaseMaterial3D.CULL_DISABLED
	_materials["water"] = water


func _mat(color: Color, metal: float, rough: float, emission_strength: float = 0.0) -> StandardMaterial3D:
	var result := StandardMaterial3D.new()
	result.albedo_color = color
	result.metallic = metal
	result.roughness = rough
	if emission_strength > 0.0:
		result.emission_enabled = true
		result.emission = color
		result.emission_energy_multiplier = emission_strength
	return result


func _load_optional_assets() -> void:
	for asset_name in ["office", "house", "warehouse", "streetlamp", "trafficlight", "tree", "bench", "trash_bin", "firehydrant"]:
		var path: String = MODEL_ROOT + asset_name + ".glb"
		if ResourceLoader.exists(path):
			var scene := ResourceLoader.load(path)
			if scene is PackedScene:
				_assets[asset_name] = scene


func _create_atmosphere() -> void:
	var environment := Environment.new()
	environment.background_mode = Environment.BG_SKY
	var sky := Sky.new()
	var sky_material := ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color("#6593a4")
	sky_material.sky_horizon_color = Color("#d0d7ca")
	sky_material.ground_bottom_color = Color("#5d7772")
	sky.sky_material = sky_material
	environment.sky = sky
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	environment.ambient_light_energy = 0.52
	environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	var atmosphere := WorldEnvironment.new()
	atmosphere.name = "Atmosphere"
	atmosphere.environment = environment
	add_child(atmosphere)
	var sun := DirectionalLight3D.new()
	sun.name = "Sun"
	sun.rotation_degrees = Vector3(-49.0, -28.0, 0.0)
	sun.light_color = Color("#ffe6be")
	sun.light_energy = 1.03
	sun.shadow_enabled = true
	add_child(sun)


func _build_terrain() -> void:
	# Land and sea floor have only two physics bodies; the small color tiles have none.
	_add_box(self, Vector3(0.0, -0.35, -40.0), Vector3(600.0, 0.7, 520.0), "soil", true, "Mainland")
	_add_box(self, Vector3(0.0, -4.55, 260.0), Vector3(600.0, 0.9, 80.0), "seabed", true, "Seabed")
	_add_box(self, Vector3(0.0, -4.6, 570.0), Vector3(600.0, 0.8, 540.0), "seabed")
	_add_box(self, Vector3(0.0, 0.07, 259.0), Vector3(600.0, 0.05, 82.0), "water")
	_add_box(self, Vector3(0.0, 0.06, 570.0), Vector3(600.0, 0.04, 540.0), "water")
	_add_box(self, Vector3(-195.0, 0.015, -195.0), Vector3(84.0, 0.04, 84.0), "lawn")
	_add_box(self, Vector3(-195.0, 0.018, -116.0), Vector3(84.0, 0.04, 60.0), "lawn")
	_add_box(self, Vector3(-114.0, 0.018, -195.0), Vector3(60.0, 0.04, 84.0), "lawn")
	_add_box(self, Vector3(-208.0, 0.025, 180.0), Vector3(58.0, 0.05, 54.0), "sand")
	_add_box(self, Vector3(-145.0, 0.025, 180.0), Vector3(48.0, 0.05, 54.0), "sand")
	_add_box(self, Vector3(22.0, 0.028, -22.0), Vector3(56.0, 0.05, 56.0), "sidewalk")
	_add_box(self, Vector3(22.0, 0.067, -22.0), Vector3(42.0, 0.02, 42.0), "cream")
	# The sea wall is low enough to jump, and the docks give direct water access.
	_add_box(_props, Vector3(0.0, 0.48, 219.4), Vector3(600.0, 0.95, 1.1), "sea_wall", true, "SeaWall")


func _build_roads() -> void:
	for x in ROAD_X:
		var start_z := 0.0 if x == 240.0 else -300.0
		var end_z := 220.0
		_add_box(_roads, Vector3(x, 0.01, (start_z + end_z) * 0.5), Vector3(16.0, 0.09, end_z - start_z), "asphalt")
		for z in range(int(start_z) + 14, int(end_z) - 12, 14):
			if _near_road_z(float(z), 12.0):
				continue
			_batch_box("white", Vector3(x, 0.067, float(z)), Vector3(0.22, 0.02, 3.7))
	for z in ROAD_Z:
		var end_x := 160.0 if z < 0.0 else 300.0
		_add_box(_roads, Vector3((-300.0 + end_x) * 0.5, 0.011, z), Vector3(end_x + 300.0, 0.09, 16.0), "asphalt")
		for x in range(-286, int(end_x) - 12, 14):
			if _near_road_x(float(x), 12.0):
				continue
			_batch_box("white", Vector3(float(x), 0.068, z), Vector3(3.7, 0.02, 0.22))
	# Sidewalks stop at each junction instead of crossing the carriageway.
	for ix in range(ROAD_X.size() - 1):
		for iz in range(ROAD_Z.size() - 1):
			var left: float = ROAD_X[ix]
			var right: float = ROAD_X[ix + 1]
			var top: float = ROAD_Z[iz]
			var bottom: float = ROAD_Z[iz + 1]
			if _is_airfield_block(ix, iz):
				continue
			var middle_x := (left + right) * 0.5
			var middle_z := (top + bottom) * 0.5
			var span_x := right - left - 18.0
			var span_z := bottom - top - 18.0
			for side in [-1.0, 1.0]:
				_batch_box("sidewalk", Vector3(middle_x, 0.071, middle_z + side * (bottom - top) * 0.5 - side * 10.0), Vector3(span_x, 0.10, 4.0))
				_batch_box("sidewalk", Vector3(middle_x + side * (right - left) * 0.5 - side * 10.0, 0.071, middle_z), Vector3(4.0, 0.10, span_z))
	for x in ROAD_X:
		for z in ROAD_Z:
			if x == 240.0 and z < 0.0:
				continue
			for side in [-1.0, 1.0]:
				for stripe in range(-3, 4):
					_batch_box("white", Vector3(x + float(stripe) * 1.55, 0.071, z + side * 11.0), Vector3(0.86, 0.02, 3.2))
					_batch_box("white", Vector3(x + side * 11.0, 0.072, z + float(stripe) * 1.55), Vector3(3.2, 0.02, 0.86))
	# The southern arterial is an easy high-speed pursuit loop.
	for x in range(-290, 290, 14):
		if not _near_road_x(float(x), 11.0):
			_batch_box("yellow", Vector3(float(x), 0.073, 160.0), Vector3(6.0, 0.02, 0.15))


func _near_road_x(x: float, distance: float) -> bool:
	for road_x in ROAD_X:
		if absf(x - road_x) < distance:
			return true
	return false


func _near_road_z(z: float, distance: float) -> bool:
	for road_z in ROAD_Z:
		if absf(z - road_z) < distance:
			return true
	return false


func _is_airfield_block(ix: int, iz: int) -> bool:
	return ix == 5 and iz <= 2


func _build_neighborhoods() -> void:
	_district_points = {
		"downtown": Vector3(-16.0, 0.2, -42.0),
		"residential": Vector3(-190.0, 0.2, 45.0),
		"industrial": Vector3(120.0, 0.2, 114.0),
		"waterfront": Vector3(-60.0, 0.2, 190.0),
		"green": Vector3(-194.0, 0.2, -194.0),
		"airfield": Vector3(209.0, 0.2, -130.0),
	}
	_location_points = {
		"spawn": get_spawn_position(),
		"police": Vector3(40.0, 0.25, -91.0),
		"hospital": Vector3(-40.0, 0.25, -91.0),
		"weapon_shop": Vector3(-40.0, 0.25, -11.5),
		"shop": Vector3(-40.0, 0.25, -11.5),
		"safehouse": Vector3(-120.0, 0.25, 68.5),
		"garage": Vector3(120.0, 0.25, 146.0),
		"gas_station": Vector3(48.0, 0.25, 137.0),
		"marina": Vector3(-80.0, 0.25, 211.0),
		"boat_spawn": Vector3(-80.0, 0.8, 258.0),
		"helipad": Vector3(183.0, 0.2, -36.0),
		"runway": Vector3(219.0, 0.2, -152.0),
		"airfield": Vector3(219.0, 0.2, -152.0),
	}
	for ix in range(ROAD_X.size() - 1):
		for iz in range(ROAD_Z.size() - 1):
			var left: float = ROAD_X[ix]
			var right: float = ROAD_X[ix + 1]
			var top: float = ROAD_Z[iz]
			var bottom: float = ROAD_Z[iz + 1]
			if _is_airfield_block(ix, iz) or iz == 5:
				continue
			if ix <= 1 and iz <= 1:
				_make_park_block(left, right, top, bottom)
			elif ix == 3 and iz == 2:
				_make_civic_plaza()
			elif _make_location_block(ix, iz, left, right, top, bottom):
				pass
			elif ix <= 1 and iz >= 2:
				_make_residential_block(left, right, top, bottom)
			elif ix >= 4 and iz >= 3:
				_make_industrial_block(left, right, top, bottom)
			else:
				_make_downtown_block(left, right, top, bottom)


func _make_location_block(ix: int, iz: int, left: float, right: float, top: float, bottom: float) -> bool:
	var x := (left + right) * 0.5
	var z := (top + bottom) * 0.5
	if ix == 3 and iz == 1:
		_make_building("office", Vector3(x, 0.0, z - 5.0), Vector3(52.0, 22.0, 35.0), "blue", "Harbor Police", "POLICE")
		_make_parking_lot(Vector3(x, 0.0, z + 28.0), 50.0, 14.0)
		return true
	if ix == 2 and iz == 1:
		_make_building("office", Vector3(x, 0.0, z - 4.0), Vector3(49.0, 20.0, 36.0), "cream", "Coast Clinic", "HOSPITAL")
		_make_parking_lot(Vector3(x, 0.0, z + 27.0), 49.0, 14.0)
		return true
	if ix == 2 and iz == 2:
		_make_building("office", Vector3(x, 0.0, z - 6.0), Vector3(48.0, 13.0, 31.0), "coral", "Supply Shop", "ARMORY")
		_make_parking_lot(Vector3(x, 0.0, z + 26.0), 46.0, 14.0)
		return true
	if ix == 1 and iz == 3:
		_make_building("house", Vector3(x, 0.0, z - 9.0), Vector3(31.0, 9.0, 29.0), "cream", "Safehouse", "SAFEHOUSE")
		_make_parking_lot(Vector3(x, 0.0, z + 26.0), 36.0, 13.0)
		return true
	if ix == 4 and iz == 4:
		_make_building("warehouse", Vector3(x, 0.0, z - 8.0), Vector3(51.0, 13.0, 31.0), "steel", "Service Garage", "GARAGE")
		_make_parking_lot(Vector3(x, 0.0, z + 26.0), 50.0, 15.0)
		return true
	if ix == 3 and iz == 4:
		_make_gas_station(Vector3(x, 0.0, z))
		return true
	return false


func _make_downtown_block(left: float, right: float, top: float, bottom: float) -> void:
	var center_x := (left + right) * 0.5
	var center_z := (top + bottom) * 0.5
	var urban := absf(center_x) <= 85.0 and center_z < 90.0
	for dx in [-13.0, 13.0]:
		for dz in [-13.0, 13.0]:
			var height := _rng.randf_range(15.0, 34.0)
			if urban:
				height = _rng.randf_range(29.0, 72.0)
			var color: String = ["tower", "blue", "cream", "warm", "steel"][_rng.randi_range(0, 4)]
			_make_building("office", Vector3(center_x + dx, 0.0, center_z + dz), Vector3(_rng.randf_range(19.0, 23.0), height, _rng.randf_range(19.0, 23.0)), color)
	_batch_box("sidewalk", Vector3(center_x, 0.08, center_z), Vector3(5.2, 0.08, 63.0))
	_batch_box("sidewalk", Vector3(center_x, 0.08, center_z), Vector3(63.0, 0.08, 5.2))


func _make_residential_block(left: float, right: float, top: float, bottom: float) -> void:
	var center_x := (left + right) * 0.5
	var center_z := (top + bottom) * 0.5
	_add_box(_structures, Vector3(center_x, 0.025, center_z), Vector3(61.0, 0.05, 61.0), "lawn")
	for dx in [-16.0, 16.0]:
		for dz in [-16.0, 16.0]:
			var color: String = ["cream", "warm", "blue", "coral"][_rng.randi_range(0, 3)]
			_make_building("house", Vector3(center_x + dx, 0.0, center_z + dz), Vector3(_rng.randf_range(16.0, 21.0), _rng.randf_range(7.0, 11.0), _rng.randf_range(17.0, 21.0)), color)
	for n in 6:
		var x := _rng.randf_range(left + 13.0, right - 13.0)
		var z := _rng.randf_range(top + 13.0, bottom - 13.0)
		if absf(x - center_x) < 10.0 or absf(z - center_z) < 10.0:
			continue
		_make_tree(Vector3(x, 0.08, z), _rng.randf_range(0.78, 1.2))


func _make_industrial_block(left: float, right: float, top: float, bottom: float) -> void:
	var center := Vector3((left + right) * 0.5, 0.0, (top + bottom) * 0.5)
	_add_box(_structures, Vector3(center.x, 0.026, center.z), Vector3(59.0, 0.05, 59.0), "sidewalk")
	_make_building("warehouse", center + Vector3(0.0, 0.0, -8.0), Vector3(51.0, _rng.randf_range(12.0, 18.0), 36.0), "steel")
	for index in 4:
		var cx := center.x - 20.0 + float(index % 2) * 18.0
		var cz := center.z + 21.0 + float(index / 2) * 7.0
		_batch_box("coral" if index % 2 == 0 else "blue", Vector3(cx, 1.4, cz), Vector3(12.0, 2.8, 5.0))


func _make_park_block(left: float, right: float, top: float, bottom: float) -> void:
	var center := Vector3((left + right) * 0.5, 0.0, (top + bottom) * 0.5)
	_add_box(_structures, Vector3(center.x, 0.03, center.z), Vector3(60.0, 0.06, 60.0), "lawn")
	_batch_box("sand", Vector3(center.x, 0.073, center.z), Vector3(4.0, 0.03, 58.0))
	_batch_box("sand", Vector3(center.x, 0.073, center.z), Vector3(58.0, 0.03, 4.0))
	for n in 18:
		var dx := _rng.randf_range(-27.0, 27.0)
		var dz := _rng.randf_range(-27.0, 27.0)
		if absf(dx) < 5.0 or absf(dz) < 5.0:
			continue
		_make_tree(center + Vector3(dx, 0.1, dz), _rng.randf_range(0.8, 1.45))
	for offset in [-14.0, 14.0]:
		_make_bench(center + Vector3(offset, 0.0, 3.5))


func _make_civic_plaza() -> void:
	# Kept clear for the initial player spawn and optional vehicle display.
	_batch_box("warm", Vector3(22.0, 0.085, -22.0), Vector3(7.5, 0.03, 7.5))
	var fountain := CylinderMesh.new()
	fountain.top_radius = 3.2
	fountain.bottom_radius = 3.5
	fountain.height = 0.55
	var mesh := MeshInstance3D.new()
	mesh.mesh = fountain
	mesh.material_override = _materials["blue"]
	mesh.position = Vector3(53.0, 0.32, -52.0)
	_structures.add_child(mesh)
	_batch_box("water", Vector3(53.0, 0.64, -52.0), Vector3(5.5, 0.03, 5.5))
	for x in [11.0, 67.0]:
		for z in [-67.0, -11.0]:
			_make_tree(Vector3(x, 0.1, z), 0.75)
	_make_bench(Vector3(36.0, 0.0, -12.0))
	_make_bench(Vector3(60.0, 0.0, -39.0))


func _make_gas_station(center: Vector3) -> void:
	_add_box(_structures, Vector3(center.x, 0.03, center.z), Vector3(58.0, 0.06, 58.0), "sidewalk")
	_make_building("house", center + Vector3(0.0, 0.0, -20.0), Vector3(40.0, 6.0, 15.0), "cream", "Fuel Stop", "FUEL")
	_batch_box("coral", center + Vector3(0.0, 5.7, 13.0), Vector3(44.0, 0.7, 19.0))
	for x in [-19.0, 19.0]:
		for z in [5.0, 22.0]:
			_batch_box("steel", center + Vector3(x, 2.8, z), Vector3(0.5, 5.5, 0.5))
	for x in [-9.0, 9.0]:
		for z in [8.0, 19.0]:
			_batch_box("coral", center + Vector3(x, 0.95, z), Vector3(1.3, 1.9, 0.8))


func _make_parking_lot(center: Vector3, width: float, depth: float) -> void:
	_add_box(_structures, Vector3(center.x, 0.045, center.z), Vector3(width, 0.07, depth), "asphalt")
	for x in range(int(center.x - width * 0.5) + 5, int(center.x + width * 0.5) - 3, 5):
		_batch_box("white", Vector3(float(x), 0.09, center.z), Vector3(0.13, 0.02, depth - 2.0))


func _make_building(kind: String, center: Vector3, dimensions: Vector3, color: String, title: String = "", sign_text: String = "") -> void:
	var body := StaticBody3D.new()
	body.name = title if title != "" else kind.capitalize()
	body.position = Vector3(center.x, 0.0, center.z)
	_structures.add_child(body)
	var collision := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = dimensions
	collision.shape = shape
	collision.position.y = dimensions.y * 0.5 + 0.08
	body.add_child(collision)
	var used_import := false
	if _assets.has(kind) and (title != "" or _rng.randf() < 0.26):
		used_import = _attach_sized_asset(body, kind, dimensions)
	if not used_import:
		_add_box(body, Vector3(0.0, dimensions.y * 0.5 + 0.08, 0.0), dimensions, color)
		_batch_box("ink", Vector3(center.x, dimensions.y + 0.32, center.z), Vector3(dimensions.x + 1.3, 0.45, dimensions.z + 1.3))
		if kind == "office":
			_add_facade_windows(center, dimensions)
		elif kind == "house":
			_add_house_detail(center, dimensions)
		else:
			_add_warehouse_detail(center, dimensions)
	if sign_text != "":
		_add_sign(sign_text, Vector3(center.x, minf(dimensions.y - 1.5, 5.5), center.z + dimensions.z * 0.5 + 0.3), "ink")


func _attach_sized_asset(parent: Node3D, asset_name: String, target: Vector3) -> bool:
	var scene: PackedScene = _assets[asset_name]
	var instance := scene.instantiate()
	if not instance is Node3D:
		instance.free()
		return false
	var corners: Array[Vector3] = []
	_collect_mesh_corners(instance, Transform3D.IDENTITY, corners)
	if corners.is_empty():
		instance.free()
		return false
	var min_corner: Vector3 = corners[0]
	var max_corner: Vector3 = corners[0]
	for point in corners:
		min_corner = min_corner.min(point)
		max_corner = max_corner.max(point)
	var size := max_corner - min_corner
	if size.x < 0.01 or size.y < 0.01 or size.z < 0.01:
		instance.free()
		return false
	var visual: Node3D = instance
	visual.scale = Vector3(target.x / size.x, target.y / size.y, target.z / size.z)
	visual.position = Vector3(-(min_corner.x + max_corner.x) * 0.5 * visual.scale.x, 0.08 - min_corner.y * visual.scale.y, -(min_corner.z + max_corner.z) * 0.5 * visual.scale.z)
	parent.add_child(visual)
	return true


func _collect_mesh_corners(node: Node, parent_transform: Transform3D, corners: Array[Vector3]) -> void:
	var transform := parent_transform
	if node is Node3D:
		transform = parent_transform * (node as Node3D).transform
	if node is MeshInstance3D:
		var box: AABB = (node as MeshInstance3D).get_aabb()
		for i in 8:
			corners.append(transform * box.get_endpoint(i))
	for child in node.get_children():
		_collect_mesh_corners(child, transform, corners)


func _add_facade_windows(center: Vector3, dimensions: Vector3) -> void:
	var floors := mini(int(dimensions.y / 3.8), 14)
	var x_count := maxi(int(dimensions.x / 4.8), 2)
	var z_count := maxi(int(dimensions.z / 4.8), 2)
	for floor_index in floors:
		var y := 2.8 + float(floor_index) * 3.8
		if y > dimensions.y - 1.0:
			break
		for i in x_count:
			var x := center.x + (float(i) + 0.5) * dimensions.x / float(x_count) - dimensions.x * 0.5
			var material := "lit_glass" if _rng.randf() < 0.1 else "glass"
			_batch_box(material, Vector3(x, y, center.z + dimensions.z * 0.5 + 0.02), Vector3(minf(2.4, dimensions.x / float(x_count) * 0.7), 2.2, 0.06))
			_batch_box("glass", Vector3(x, y, center.z - dimensions.z * 0.5 - 0.02), Vector3(minf(2.4, dimensions.x / float(x_count) * 0.7), 2.2, 0.06))
		for i in z_count:
			var z := center.z + (float(i) + 0.5) * dimensions.z / float(z_count) - dimensions.z * 0.5
			_batch_box("glass", Vector3(center.x + dimensions.x * 0.5 + 0.02, y, z), Vector3(0.06, 2.2, minf(2.4, dimensions.z / float(z_count) * 0.7)))
			_batch_box("glass", Vector3(center.x - dimensions.x * 0.5 - 0.02, y, z), Vector3(0.06, 2.2, minf(2.4, dimensions.z / float(z_count) * 0.7)))
	_batch_box("ink", Vector3(center.x, 2.0, center.z + dimensions.z * 0.5 + 0.06), Vector3(3.5, 3.4, 0.1))


func _add_house_detail(center: Vector3, dimensions: Vector3) -> void:
	_batch_box("ink", Vector3(center.x, 1.45, center.z + dimensions.z * 0.5 + 0.05), Vector3(2.3, 2.7, 0.1))
	for x_offset in [-dimensions.x * 0.29, dimensions.x * 0.29]:
		for z_side in [-1.0, 1.0]:
			_batch_box("glass", Vector3(center.x + x_offset, dimensions.y * 0.5, center.z + z_side * (dimensions.z * 0.5 + 0.04)), Vector3(2.3, 2.5, 0.08))


func _add_warehouse_detail(center: Vector3, dimensions: Vector3) -> void:
	for x_offset in [-dimensions.x * 0.24, dimensions.x * 0.24]:
		_batch_box("ink", Vector3(center.x + x_offset, 3.1, center.z + dimensions.z * 0.5 + 0.05), Vector3(dimensions.x * 0.31, 5.5, 0.11))
	for x_offset in [-dimensions.x * 0.34, 0.0, dimensions.x * 0.34]:
		_batch_box("glass", Vector3(center.x + x_offset, dimensions.y - 2.0, center.z + dimensions.z * 0.5 + 0.05), Vector3(3.0, 1.8, 0.1))


func _add_sign(label_text: String, position: Vector3, backing: String) -> void:
	_batch_box(backing, position + Vector3(0.0, 0.0, -0.1), Vector3(maxf(9.0, float(label_text.length()) * 0.75), 1.45, 0.16))
	var label := Label3D.new()
	label.name = label_text
	label.text = label_text
	label.font_size = 50
	label.pixel_size = 0.019
	label.modulate = Color("#ffe3ae")
	label.outline_size = 3
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.position = position + Vector3(0.0, 0.0, 0.1)
	_props.add_child(label)


func _build_airfield() -> void:
	# Parallel to the long east edge.  The runway shares the large land collider.
	_add_box(_roads, Vector3(219.0, 0.045, -150.0), Vector3(36.0, 0.07, 200.0), "runway")
	for z in range(-240, -61, 16):
		_batch_box("white", Vector3(219.0, 0.09, float(z)), Vector3(1.0, 0.02, 7.0))
	for x in [203.5, 234.5]:
		_batch_box("yellow", Vector3(x, 0.092, -150.0), Vector3(0.24, 0.02, 188.0))
	_add_box(_roads, Vector3(183.0, 0.048, -36.0), Vector3(29.0, 0.07, 29.0), "runway")
	_batch_box("white", Vector3(183.0, 0.092, -36.0), Vector3(17.0, 0.02, 1.0))
	_batch_box("white", Vector3(183.0, 0.092, -36.0), Vector3(1.0, 0.02, 17.0))
	_make_building("warehouse", Vector3(267.0, 0.0, -34.0), Vector3(43.0, 14.0, 34.0), "blue", "Harbor Hangar", "HANGAR")
	_add_box(_roads, Vector3(219.0, 0.045, -18.0), Vector3(77.0, 0.07, 14.0), "runway")
	for z in [-247.0, -53.0]:
		for x in range(206, 234, 5):
			_batch_box("white", Vector3(float(x), 0.094, z), Vector3(3.0, 0.02, 1.5))
	for z in range(-240, -41, 14):
		_batch_box("lamp", Vector3(196.5, 0.21, float(z)), Vector3(0.35, 0.3, 0.35))
		_batch_box("lamp", Vector3(241.5, 0.21, float(z)), Vector3(0.35, 0.3, 0.35))


func _build_waterfront() -> void:
	# A broad promenade, three walkable piers and a modest beach.
	_add_box(_props, Vector3(0.0, 0.08, 214.3), Vector3(580.0, 0.12, 10.0), "sidewalk")
	for x in [-138.0, -80.0, 16.0, 122.0]:
		_add_box(_props, Vector3(x, 0.71, 235.0), Vector3(8.0, 0.22, 42.0), "wood", true, "Pier")
		for z in [222.0, 239.0, 252.0]:
			for dx in [-3.3, 3.3]:
				_batch_box("tree_trunk", Vector3(x + dx, -0.15, z), Vector3(0.45, 1.9, 0.45))
	for x in range(-260, 261, 20):
		if abs(x + 138) < 12 or abs(x + 80) < 12 or abs(x - 16) < 12 or abs(x - 122) < 12:
			continue
		_make_bench(Vector3(float(x), 0.0, 208.0))
	for x in [-188.0, -172.0, -151.0, -119.0, -100.0]:
		_batch_box("cream", Vector3(x, 1.2, 199.0), Vector3(0.15, 2.4, 0.15))
		_batch_box("coral", Vector3(x, 2.4, 199.0), Vector3(3.6, 0.12, 3.6))
	for x in [-48.0, 49.0, 174.0]:
		_make_building("house", Vector3(x, 0.0, 187.0), Vector3(17.0, 6.0, 14.0), "warm")


func _build_street_furniture() -> void:
	for x in ROAD_X:
		for z in ROAD_Z:
			if x == 240.0 and z < 0.0:
				continue
			if z >= 210.0:
				continue
			_make_traffic_light(Vector3(x + 10.7, 0.0, z + 10.7), true)
			_make_traffic_light(Vector3(x - 10.7, 0.0, z - 10.7), false)
			if (int(x + z) / 80) % 2 == 0:
				var light_here := int(x) in [-160, 0, 160] and int(z) in [-160, 0, 160]
				_make_streetlamp(Vector3(x + 11.0, 0.0, z - 11.0), light_here)
				_make_streetlamp(Vector3(x - 11.0, 0.0, z + 11.0), light_here)
			if (int(x + z) / 80) % 3 == 0:
				_make_small_asset("firehydrant", Vector3(x + 12.3, 0.06, z - 12.4), Vector3(0.65, 0.8, 0.65))
				_make_small_asset("trash_bin", Vector3(x - 12.0, 0.06, z + 12.1), Vector3(0.8, 1.1, 0.8))
	for x in [-230.0, -170.0, -94.0, -24.0, 60.0, 143.0]:
		for z in [197.0, 212.0]:
			_make_streetlamp(Vector3(x, 0.0, z), z == 212.0 and int(x) in [-170, -24, 60, 143])
	for z in [-125.0, -45.0, 35.0, 115.0]:
		for x in [-234.0, -167.0, -87.0]:
			_make_tree(Vector3(x, 0.09, z), 0.9)


func _make_tree(position: Vector3, tree_scale: float) -> void:
	if _assets.has("tree") and _rng.randf() < 0.24:
		var tree := Node3D.new()
		tree.name = "CoastalTree"
		tree.position = position
		_props.add_child(tree)
		if _attach_sized_asset(tree, "tree", Vector3(3.8, 6.4, 3.8) * tree_scale):
			return
	_batch_box("tree_trunk", position + Vector3(0.0, 1.15 * tree_scale, 0.0), Vector3(0.5, 2.3, 0.5) * tree_scale)
	_batch_box("leaf", position + Vector3(0.0, 3.25 * tree_scale, 0.0), Vector3(3.6, 2.9, 3.6) * tree_scale)
	_batch_box("leaf_light", position + Vector3(0.0, 4.75 * tree_scale, 0.0), Vector3(2.5, 2.0, 2.5) * tree_scale)


func _make_streetlamp(position: Vector3, with_light: bool = false) -> void:
	if with_light:
		_add_night_light(position)
	if _assets.has("streetlamp") and _rng.randf() < 0.28:
		var holder := Node3D.new()
		holder.name = "Streetlamp"
		holder.position = position
		_props.add_child(holder)
		if _attach_sized_asset(holder, "streetlamp", Vector3(2.5, 7.0, 1.6)):
			return
	_batch_box("steel", position + Vector3(0.0, 3.4, 0.0), Vector3(0.19, 6.8, 0.19))
	_batch_box("steel", position + Vector3(0.9, 6.7, 0.0), Vector3(1.9, 0.15, 0.15))
	_batch_box("lamp", position + Vector3(1.7, 6.55, 0.0), Vector3(0.52, 0.15, 0.42))


func _add_night_light(position: Vector3) -> void:
	var lamp := OmniLight3D.new()
	lamp.name = "NightLamp_%02d" % _night_lights.size()
	lamp.position = position + Vector3(1.35, 6.05, 0.0)
	lamp.light_color = Color("#ffc98a")
	lamp.light_energy = 0.68
	lamp.omni_range = 16.0
	lamp.shadow_enabled = false
	lamp.visible = _night_lights_active
	_props.add_child(lamp)
	_night_lights.append(lamp)


func _make_traffic_light(position: Vector3, east_west: bool) -> void:
	if _assets.has("trafficlight") and _rng.randf() < 0.28:
		var holder := Node3D.new()
		holder.name = "TrafficLight"
		holder.position = position
		_props.add_child(holder)
		if _attach_sized_asset(holder, "trafficlight", Vector3(1.6, 5.4, 1.6)):
			_add_traffic_indicator(position, east_west)
			return
	_batch_box("steel", position + Vector3(0.0, 2.55, 0.0), Vector3(0.16, 5.1, 0.16))
	_batch_box("ink", position + Vector3(0.0, 4.8, 0.0), Vector3(0.75, 1.5, 0.45))
	_add_traffic_indicator(position, east_west)


func _add_traffic_indicator(position: Vector3, east_west: bool) -> void:
	var red := MeshInstance3D.new()
	red.name = "RedSignal"
	var lamp_mesh := SphereMesh.new()
	lamp_mesh.radius = 0.20
	lamp_mesh.height = 0.4
	red.mesh = lamp_mesh
	red.position = position + Vector3(0.0, 5.25, 0.32)
	_props.add_child(red)
	var green := MeshInstance3D.new()
	green.name = "GreenSignal"
	green.mesh = lamp_mesh
	green.position = position + Vector3(0.0, 4.37, 0.32)
	_props.add_child(green)
	var indicator := {"red": red, "green": green, "east_west": east_west}
	_traffic_indicators.append(indicator)
	_update_traffic_indicator(indicator)


func _update_traffic_indicator(indicator: Dictionary) -> void:
	var green_now: bool = bool(indicator["east_west"]) == _east_west_green
	(indicator["red"] as MeshInstance3D).material_override = _signal_off_red if green_now else _signal_on_red
	(indicator["green"] as MeshInstance3D).material_override = _signal_on_green if green_now else _signal_off_green


func _make_bench(position: Vector3) -> void:
	if _assets.has("bench") and _rng.randf() < 0.42:
		var holder := Node3D.new()
		holder.name = "Bench"
		holder.position = position
		_props.add_child(holder)
		if _attach_sized_asset(holder, "bench", Vector3(2.4, 1.45, 0.75)):
			return
	_batch_box("wood", position + Vector3(0.0, 0.52, 0.0), Vector3(2.4, 0.16, 0.66))
	_batch_box("wood", position + Vector3(0.0, 1.05, 0.31), Vector3(2.4, 0.9, 0.14))
	for x in [-0.9, 0.9]:
		_batch_box("ink", position + Vector3(x, 0.27, 0.0), Vector3(0.12, 0.53, 0.55))


func _make_small_asset(asset_name: String, position: Vector3, size: Vector3) -> void:
	if _assets.has(asset_name) and _rng.randf() < 0.65:
		var holder := Node3D.new()
		holder.name = asset_name.capitalize()
		holder.position = position
		_props.add_child(holder)
		if _attach_sized_asset(holder, asset_name, size):
			return
	_batch_box("coral" if asset_name == "firehydrant" else "steel", position + Vector3(0.0, size.y * 0.5, 0.0), size)


func _build_route_data() -> void:
	# Each polyline closes on its first point.  All points are lane centres on real roads.
	_add_rect_route(-240.0, 160.0, -240.0, 160.0, true)
	_add_rect_route(-240.0, 160.0, -240.0, 160.0, false)
	_add_rect_route(-160.0, 80.0, -160.0, 80.0, true)
	_add_rect_route(-160.0, 80.0, -160.0, 80.0, false)
	_add_rect_route(-80.0, 160.0, -80.0, 160.0, true)
	_add_rect_route(-80.0, 160.0, -80.0, 160.0, false)
	_add_rect_route(0.0, 240.0, 0.0, 160.0, true)
	_add_rect_route(0.0, 240.0, 0.0, 160.0, false)
	for x in ROAD_X:
		for z in ROAD_Z:
			if x == 240.0 and z < 0.0:
				continue
			for dx in [-11.5, 11.5]:
				for dz in [-11.5, 11.5]:
					var pedestrian := Vector3(x + dx, 0.18, z + dz)
					if pedestrian.z < WATER_EDGE - 2.0 and pedestrian.x > -295.0 and pedestrian.x < 295.0:
						_pedestrian_points.append(pedestrian)
	for x in range(-225, -85, 20):
		_pedestrian_points.append(Vector3(float(x), 0.16, -194.0))
	for x in range(-230, 230, 24):
		_pedestrian_points.append(Vector3(float(x), 0.16, 207.0))


func _add_rect_route(left: float, right: float, top: float, bottom: float, clockwise: bool) -> void:
	var lane := PackedVector3Array()
	var offset := 4.0
	if clockwise:
		_append_straight(lane, Vector3(left + offset, 0.13, top + offset), Vector3(right - offset, 0.13, top + offset))
		_append_straight(lane, Vector3(right - offset, 0.13, top + offset), Vector3(right - offset, 0.13, bottom - offset))
		_append_straight(lane, Vector3(right - offset, 0.13, bottom - offset), Vector3(left + offset, 0.13, bottom - offset))
		_append_straight(lane, Vector3(left + offset, 0.13, bottom - offset), Vector3(left + offset, 0.13, top + offset))
	else:
		_append_straight(lane, Vector3(left - offset, 0.13, top - offset), Vector3(left - offset, 0.13, bottom + offset))
		_append_straight(lane, Vector3(left - offset, 0.13, bottom + offset), Vector3(right + offset, 0.13, bottom + offset))
		_append_straight(lane, Vector3(right + offset, 0.13, bottom + offset), Vector3(right + offset, 0.13, top - offset))
		_append_straight(lane, Vector3(right + offset, 0.13, top - offset), Vector3(left - offset, 0.13, top - offset))
	lane.append(lane[0])
	_road_lanes.append(lane)


func _append_straight(route: PackedVector3Array, start: Vector3, end: Vector3) -> void:
	var count := maxi(1, ceili(start.distance_to(end) / 26.0))
	for step in count:
		route.append(start.lerp(end, float(step) / float(count)))


func _add_box(parent: Node3D, center: Vector3, size: Vector3, material_name: String, solid: bool = false, object_name: String = "") -> Node3D:
	var container: Node3D
	if solid:
		var body := StaticBody3D.new()
		body.name = object_name if object_name != "" else "Solid"
		body.position = center
		parent.add_child(body)
		var shape_node := CollisionShape3D.new()
		var shape := BoxShape3D.new()
		shape.size = size
		shape_node.shape = shape
		body.add_child(shape_node)
		container = body
	else:
		container = Node3D.new()
		container.name = object_name if object_name != "" else "Visual"
		container.position = center
		parent.add_child(container)
	var mesh := MeshInstance3D.new()
	mesh.mesh = _unit_box
	mesh.material_override = _materials[material_name]
	mesh.scale = size
	container.add_child(mesh)
	return container


func _batch_box(material_name: String, center: Vector3, size: Vector3) -> void:
	if not _batches.has(material_name):
		_batches[material_name] = []
	var transform := Transform3D(Basis().scaled(size), center)
	_batches[material_name].append(transform)


func _flush_batches() -> void:
	for material_name in _batches.keys():
		var transforms: Array = _batches[material_name]
		if transforms.is_empty():
			continue
		var multi := MultiMesh.new()
		multi.transform_format = MultiMesh.TRANSFORM_3D
		multi.mesh = _unit_box
		multi.instance_count = transforms.size()
		for index in transforms.size():
			multi.set_instance_transform(index, transforms[index])
		var visual := MultiMeshInstance3D.new()
		visual.name = "Batched_" + str(material_name)
		visual.multimesh = multi
		visual.material_override = _materials[material_name]
		_props.add_child(visual)
	_batches.clear()
