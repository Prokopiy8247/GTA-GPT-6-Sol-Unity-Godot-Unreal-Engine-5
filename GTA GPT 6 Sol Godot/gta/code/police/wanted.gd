class_name GTAWanted
extends Node

signal changed(stars: int, state: String)

var stars: int = 0
var heat: float = 0.0
var state: String = "clear"
var last_known: Vector3 = Vector3.ZERO
var escape_time: float = 0.0
var unseen_time: float = 0.0
var officer_has_sight: bool = false

const THRESHOLDS := [0.0, 10.0, 27.0, 48.0, 74.0, 108.0]
const CRIME_HEAT := {"gunfire": 10.0, "suppressed_gunfire": 4.0, "assault": 19.0, "murder": 34.0, "vehicle_theft": 19.0, "car_theft": 19.0, "police_theft": 45.0, "police_assault": 38.0, "explosion": 38.0, "collision": 8.0, "robbery": 30.0}

func report_crime(kind: String, position: Vector3, witnessed: bool = true) -> void:
	if not witnessed:
		return
	heat = minf(180.0, heat + float(CRIME_HEAT.get(kind, 10.0)))
	last_known = position
	state = "pursuit"
	escape_time = 0.0
	unseen_time = 0.0
	_recompute()

func set_officer_sight(visible: bool, position: Vector3) -> void:
	officer_has_sight = visible
	if stars <= 0:
		return
	if visible:
		last_known = position
		unseen_time = 0.0
		escape_time = 0.0
		if state != "pursuit":
			state = "pursuit"
			changed.emit(stars, state)

func _process(delta: float) -> void:
	if stars <= 0:
		return
	if officer_has_sight:
		unseen_time = 0.0
		return
	unseen_time += delta
	if unseen_time > 2.8 and state == "pursuit":
		state = "search"
		changed.emit(stars, state)
	if state == "search":
		escape_time += delta
		var required := 10.0 + float(stars) * 8.0
		if escape_time >= required:
			heat = maxf(0.0, heat - (14.0 + float(stars) * 8.0))
			escape_time = 0.0
			_recompute()
			if stars == 0:
				state = "clear"
				changed.emit(stars, state)

func set_level(level: int) -> void:
	stars = clampi(level, 0, 5)
	heat = THRESHOLDS[stars]
	state = "pursuit" if stars > 0 else "clear"
	escape_time = 0.0
	unseen_time = 0.0
	changed.emit(stars, state)

func clear() -> void:
	set_level(0)

func _recompute() -> void:
	var previous := stars
	stars = 0
	for level in range(1, 6):
		if heat >= THRESHOLDS[level]:
			stars = level
	if previous != stars:
		changed.emit(stars, state)
