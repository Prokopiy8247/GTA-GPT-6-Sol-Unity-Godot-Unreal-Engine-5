extends SceneTree

func _initialize() -> void:
	call_deferred("_capture")

func _capture() -> void:
	var packed: PackedScene = load("res://gta/scenes/bootstrap/main.tscn")
	var game: GTAGame = packed.instantiate() as GTAGame
	root.add_child(game)
	current_scene = game
	var args := OS.get_cmdline_user_args()
	if args.has("night"):
		game.time_of_day = 1.0
		game.set_weather("rain")
	for i in 45:
		await process_frame
	if args.has("map"):
		game.hud.toggle_map()
	elif args.has("wheel"):
		game.hud.open_weapon_wheel()
	elif not args.has("night"):
		game.hud.toggle_admin()
	for i in 10:
		await process_frame
	var image: Image = root.get_texture().get_image()
	var output_name := "night.png" if args.has("night") else ("map.png" if args.has("map") else ("wheel.png" if args.has("wheel") else "admin.png"))
	var path := ProjectSettings.globalize_path("res://.sol-run/" + output_name)
	var result := image.save_png(path)
	print("VISUAL_QA_PATH: ", path, " RESULT: ", result)
	print("VISUAL_QA_FPS: ", Engine.get_frames_per_second())
	game.queue_free()
	for i in 3:
		await process_frame
	quit(0 if result == OK else 1)
