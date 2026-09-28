using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Original Harborline HUD, radar and benchmark controls. Uses IMGUI to avoid scene wiring.</summary>
public class SandboxHUD : MonoBehaviour
{
    public bool AdminOpen { get; private set; }
    bool mapOpen, wheelOpen, helpOpen, pauseOpen;
    Vector2 adminScroll;
    SandboxDirector world;
    SandboxPlayer player;
    SandboxArsenal arsenal;
    SandboxCamera cameraRig;
    Texture2D pixel;
    GUIStyle title, body, small, button, centered;

    void Start()
    {
        world = SandboxDirector.Instance ?? FindFirstObjectByType<SandboxDirector>();
        player = FindFirstObjectByType<SandboxPlayer>();
        arsenal = player != null ? player.GetComponent<SandboxArsenal>() : null;
        cameraRig = FindFirstObjectByType<SandboxCamera>();
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    void EnsureStyles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.87f, .97f, 1f) } };
        body = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(.86f, .92f, .95f) }, wordWrap = true };
        small = new GUIStyle(body) { fontSize = 11 };
        centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
        button = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 29 };
    }

    void Update()
    {
        var key = Keyboard.current;
        if (key == null) return;
        if (key.f1Key.wasPressedThisFrame) AdminOpen = !AdminOpen;
        if (key.mKey.wasPressedThisFrame) mapOpen = !mapOpen;
        if (key.f2Key.wasPressedThisFrame) helpOpen = !helpOpen;
        if (key.escapeKey.wasPressedThisFrame)
        {
            if (AdminOpen) AdminOpen = false;
            else if (mapOpen) mapOpen = false;
            else if (helpOpen) helpOpen = false;
            else if (world != null && world.ActiveLocation != null) world.CloseLocation();
            else pauseOpen = !pauseOpen;
        }
        wheelOpen = key.tabKey.isPressed && !AdminOpen && !mapOpen;
        bool ui = AdminOpen || mapOpen || wheelOpen || helpOpen || pauseOpen || world != null && world.ActiveLocation != null;
        if (player != null) player.InputEnabled = !ui;
        if (cameraRig != null) cameraRig.InputEnabled = !ui;
        Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = ui;
        Time.timeScale = pauseOpen ? 0f : wheelOpen ? .25f : 1f;
    }

    void OnGUI()
    {
        if (world == null || player == null || pixel == null) return;
        EnsureStyles();
        float width = Screen.width, height = Screen.height;
        Rect top = new Rect(18, 16, 300, 100);
        Panel(top, new Color(.055f, .085f, .14f, .83f));
        GUI.Label(new Rect(32, 22, 270, 30), "HARBORLINE", title);
        GUI.Label(new Rect(32, 53, 285, 20), "FREE ROAM   /   " + Mathf.FloorToInt(world.Hour).ToString("00") + ":" + Mathf.FloorToInt((world.Hour % 1f) * 60f).ToString("00") + "   /   " + world.Weather.ToString().ToUpperInvariant(), small);
        GUI.Label(new Rect(32, 76, 285, 25), "CASH  $" + player.Cash.ToString("N0"), body);

        float right = width - 260f;
        Panel(new Rect(right, 16, 242, 114), new Color(.055f, .085f, .14f, .83f));
        GUI.Label(new Rect(right + 13, 20, 220, 25), world.InPursuit ? "POLICE PURSUIT" : world.WantedLevel > 0 ? "SEARCH AREA" : "NO ACTIVE SEARCH", small);
        GUI.Label(new Rect(right + 13, 42, 220, 26), Stars(), new GUIStyle(title) { fontSize = 23, normal = { textColor = world.InPursuit || Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0 ? new Color(1f, .65f, .32f) : new Color(.45f, .47f, .52f) } });
        GUI.Label(new Rect(right + 13, 75, 225, 22), arsenal != null ? arsenal.SelectedName.ToUpperInvariant() + "   " + arsenal.CurrentAmmo + "/" + arsenal.ReserveAmmo : "UNARMED", small);
        if (player.IsInVehicle && player.CurrentVehicle != null)
            GUI.Label(new Rect(right + 13, 96, 220, 20), Mathf.RoundToInt(player.CurrentVehicle.SpeedKph) + " KM/H   •   " + player.CurrentVehicle.Kind, small);

        float bottom = height - 112f;
        Panel(new Rect(18, bottom, 279, 94), new Color(.055f, .085f, .14f, .86f));
        LabelBar("HEALTH", player.Health / 100f, new Color(.9f, .36f, .31f), 31, bottom + 12);
        LabelBar("ARMOR", player.Armor / 100f, new Color(.38f, .7f, .95f), 31, bottom + 39);
        GUI.Label(new Rect(31, bottom + 65, 240, 19), "F1  SANDBOX     F2  CONTROLS     M  MAP", small);
        DrawRadar(new Rect(width - 217, height - 217, 199, 199));

        if (world.ToastUntil > Time.time)
        {
            Panel(new Rect(width / 2f - 235f, 29, 470, 33), new Color(.04f, .12f, .18f, .82f));
            GUI.Label(new Rect(width / 2f - 228f, 33, 456, 25), world.Toast, centered);
        }
        if (!AdminOpen && !wheelOpen && !mapOpen && !pauseOpen && world.ActiveLocation == null)
        {
            GUI.color = new Color(.9f, 1f, 1f, .8f);
            GUI.Label(new Rect(width / 2f - 10f, height / 2f - 13f, 20, 25), "+", centered);
            GUI.color = Color.white;
        }
        if (AdminOpen) DrawAdmin();
        if (wheelOpen) DrawWeaponWheel();
        if (mapOpen) DrawFullMap();
        if (helpOpen) DrawHelp();
        if (pauseOpen) DrawPause();
        if (world.ActiveLocation != null) DrawLocation(world.ActiveLocation);
    }

    string Stars()
    {
        string s = "";
        for (int i = 0; i < 5; i++) s += i < world.WantedLevel ? "★ " : "☆ ";
        return s;
    }

    void Panel(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, pixel); GUI.color = Color.white; }
    void LabelBar(string name, float fraction, Color color, float x, float y)
    {
        GUI.Label(new Rect(x, y, 70, 23), name, small);
        Panel(new Rect(x + 74, y + 5, 169, 12), new Color(.16f, .22f, .27f));
        Panel(new Rect(x + 74, y + 5, 169 * Mathf.Clamp01(fraction), 12), color);
    }
    bool Button(string text) => GUILayout.Button(text, button);

    void DrawRadar(Rect r)
    {
        Panel(r, new Color(.045f, .095f, .14f, .87f));
        GUI.color = new Color(.25f, .44f, .48f, .9f);
        float cx = r.x + r.width / 2f, cy = r.y + r.height / 2f;
        for (int i = -3; i <= 3; i++)
        {
            float px = cx + i * 27f - player.transform.position.x * .22f % 27f;
            float py = cy + i * 27f + player.transform.position.z * .22f % 27f;
            GUI.DrawTexture(new Rect(px, r.y + 4, 2, r.height - 8), pixel);
            GUI.DrawTexture(new Rect(r.x + 4, py, r.width - 8, 2), pixel);
        }
        GUI.color = Color.white;
        foreach (var place in FindObjectsByType<SandboxLocation>(FindObjectsSortMode.None))
        {
            Vector3 delta = place.transform.position - player.transform.position;
            float x = cx + delta.x * .22f, y = cy - delta.z * .22f;
            if (x > r.x + 7 && x < r.xMax - 7 && y > r.y + 7 && y < r.yMax - 7)
                Panel(new Rect(x - 3, y - 3, 6, 6), new Color(.99f, .65f, .32f));
        }
        Panel(new Rect(cx - 5, cy - 5, 10, 10), new Color(.23f, .94f, .93f));
        GUI.Label(new Rect(r.x + 9, r.y + 5, 160, 18), "DISTRICT RADAR", small);
    }

    void DrawAdmin()
    {
        float w = 340f, top = Mathf.Min(148f, Screen.height * .2f);
        float h = Mathf.Min(760f, Screen.height - top - 18f);
        Rect r = new Rect(Screen.width - w - 23f, top, w, h);
        Panel(r, new Color(.045f, .075f, .12f, .96f));
        GUI.Label(new Rect(r.x + 17, r.y + 9, 290, 33), "SANDBOX LAB", title);
        GUILayout.BeginArea(new Rect(r.x + 15, r.y + 47, r.width - 30, r.height - 61));
        adminScroll = GUILayout.BeginScrollView(adminScroll);
        GUILayout.Label("TELEPORT", body);
        if (Button("Downtown / Civic Center")) world.TeleportTo("Downtown", new Vector3(3f, 1f, 82f));
        if (Button("Residential / Westhaven")) world.TeleportTo("Residential", new Vector3(-163f, 1f, 87f));
        if (Button("Industrial / Foundry Quarter")) world.TeleportTo("Industrial", new Vector3(163f, 1f, -80f));
        if (Button("Waterfront / North Quay")) world.TeleportTo("Waterfront", new Vector3(95f, 1f, -240f));
        if (Button("Park / Juniper Park")) world.TeleportTo("Juniper Park", new Vector3(-160f, 1f, 215f));
        if (Button("Airfield / Cape Airfield")) world.TeleportTo("Cape Airfield", new Vector3(124f, 1f, 205f));

        GUILayout.Space(8);
        GUILayout.Label("VEHICLES", body);
        for (int i = 0; i < world.VehicleCatalog.Count; i++)
        {
            var car = world.VehicleCatalog[i];
            if (car != null && Button("Spawn " + car.name + "  [" + car.Kind + "]")) world.SpawnVehicle(i);
        }
        if (player.CurrentVehicle != null && Button("Repair current vehicle")) player.CurrentVehicle.Repair();
        GUILayout.Space(8);

        GUILayout.Label("ARSENAL", body);
        if (arsenal != null)
        {
            for (int i = 1; i < SandboxArsenal.Definitions.Length; i++)
                if (Button("Give " + SandboxArsenal.Definitions[i].Name)) arsenal.Give((SandboxArsenal.Weapon)i);
            if (Button("Refill all ammunition")) arsenal.RefillAll();
        }
        GUILayout.Space(8);
        GUILayout.Label("POLICE / PLAYER", body);
        GUILayout.BeginHorizontal();
        for (int i = 0; i <= 5; i++) { int stars = i; if (Button(i.ToString())) world.SetWantedLevel(stars); }
        GUILayout.EndHorizontal();
        if (Button("Spawn police response")) world.SpawnPolice();
        if (Button("+$10,000 cash")) player.Cash += 10000;
        if (Button("Full health + armor")) { player.Health = 100f; player.Armor = 100f; }
        player.Invulnerable = GUILayout.Toggle(player.Invulnerable, "Invulnerability");
        GUILayout.Space(8);

        GUILayout.Label("ENVIRONMENT", body);
        GUILayout.BeginHorizontal();
        if (Button("Morning")) world.Hour = 7f;
        if (Button("Noon")) world.Hour = 13f;
        if (Button("Night")) world.Hour = 22f;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        foreach (SandboxDirector.WeatherState state in System.Enum.GetValues(typeof(SandboxDirector.WeatherState)))
        { var selected = state; if (Button(state.ToString())) world.SetWeather(selected); }
        GUILayout.EndHorizontal();
        world.TrafficEnabled = GUILayout.Toggle(world.TrafficEnabled, "Civilian traffic");
        world.PedestriansEnabled = GUILayout.Toggle(world.PedestriansEnabled, "Pedestrians");
        world.WildlifeEnabled = GUILayout.Toggle(world.WildlifeEnabled, "Wildlife (if present)");
        if (Button("Max character skills")) for (int i = 0; i < world.Skills.Length; i++) world.Skills[i] = 100f;
        if (Button("Reset character skills")) for (int i = 0; i < world.Skills.Length; i++) world.Skills[i] = 0f;
        if (Button("Give parachute")) player.GiveParachute();
        if (Button("Refill breath")) player.RefillLungs();
        player.Scuba = GUILayout.Toggle(player.Scuba, "Scuba breathing");
        GUILayout.Space(8);

        GUILayout.Label("SAVE / OBSERVE", body);
        if (Button("Save free roam")) world.SaveGame();
        if (Button("Load free roam")) world.LoadGame();
        GUILayout.Label("FPS " + Mathf.RoundToInt(1f / Mathf.Max(.001f, Time.unscaledDeltaTime)) + "   |   " + player.transform.position.ToString("F1"), small);
        GUILayout.Label("Stamina " + Mathf.RoundToInt(world.Skills[0]) + "  Shooting " + Mathf.RoundToInt(world.Skills[1]) + "  Driving " + Mathf.RoundToInt(world.Skills[4]) + "  Flying " + Mathf.RoundToInt(world.Skills[5]), small);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawWeaponWheel()
    {
        if (arsenal == null) return;
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        Panel(new Rect(cx - 245, cy - 245, 490, 490), new Color(.045f, .075f, .11f, .9f));
        GUI.Label(new Rect(cx - 160, cy - 44, 320, 30), "SELECT WEAPON", centered);
        GUI.Label(new Rect(cx - 160, cy - 15, 320, 26), arsenal.SelectedName + "   " + arsenal.CurrentAmmo + "/" + arsenal.ReserveAmmo, centered);
        int count = SandboxArsenal.Definitions.Length;
        for (int i = 0; i < count; i++)
        {
            if (!arsenal.Owned[i]) continue;
            float angle = (i / (float)count) * Mathf.PI * 2f - Mathf.PI / 2f;
            Rect item = new Rect(cx + Mathf.Cos(angle) * 186f - 58f, cy + Mathf.Sin(angle) * 186f - 16f, 116, 32);
            if (GUI.Button(item, SandboxArsenal.Definitions[i].Name, button)) arsenal.Select(i);
        }
    }

    void DrawFullMap()
    {
        float size = Mathf.Min(Screen.height - 90, 650f);
        Rect r = new Rect((Screen.width - size) / 2f, (Screen.height - size) / 2f, size, size);
        Panel(r, new Color(.045f, .09f, .13f, .97f));
        GUI.Label(new Rect(r.x + 18, r.y + 14, 330, 32), "HARBORLINE / FREE ROAM", title);
        float scale = (size - 70f) / 600f;
        Vector2 center = new Vector2(r.center.x, r.center.y + 17f);
        for (int i = -3; i <= 3; i++)
        {
            float p = center.x + i * 80f * scale;
            Panel(new Rect(p - 2f, r.y + 52, 4f, size - 66), new Color(.29f, .43f, .47f));
            p = center.y + i * 80f * scale;
            Panel(new Rect(r.x + 14, p - 2f, size - 28, 4f), new Color(.29f, .43f, .47f));
        }
        foreach (var place in FindObjectsByType<SandboxLocation>(FindObjectsSortMode.None))
        {
            Vector2 p = new Vector2(center.x + place.transform.position.x * scale, center.y - place.transform.position.z * scale);
            Panel(new Rect(p.x - 4, p.y - 4, 8, 8), new Color(1f, .63f, .3f));
            GUI.Label(new Rect(p.x + 7, p.y - 7, 135, 19), place.DisplayName, small);
        }
        Vector2 playerP = new Vector2(center.x + player.transform.position.x * scale, center.y - player.transform.position.z * scale);
        Panel(new Rect(playerP.x - 7, playerP.y - 7, 14, 14), new Color(.17f, .94f, .87f));
        GUI.Label(new Rect(r.x + 18, r.yMax - 30, 440, 23), "TEAL  YOU     AMBER  SERVICES    •    M TO CLOSE", small);
    }

    void DrawHelp()
    {
        Rect r = new Rect(Screen.width / 2f - 280, Screen.height / 2f - 220, 560, 440);
        Panel(r, new Color(.045f, .075f, .12f, .97f));
        GUI.Label(new Rect(r.x + 20, r.y + 16, 490, 34), "CONTROLS", title);
        GUI.Label(new Rect(r.x + 20, r.y + 56, 520, 360),
            "WASD — Move / steer / aircraft controls\nMouse — Camera / aim     V — Camera mode\nShift — Sprint     Space — Jump / handbrake / aircraft climb\nZ — Stealth     Q — Cover (on foot) / aircraft yaw\nP — Deploy parachute in free fall\nF — Enter or exit vehicle     E — Interact\nLeft click — Fire / attack     Right click — Aim\nR — Reload     1–8 / wheel — Switch weapon\nTab — Weapon selection     M — Map\nL — Headlights     J — Police siren\nPlane: Up/Down — Pitch; Q/E — Yaw\nF1 — Sandbox lab     F2 — Controls     Esc — Pause\n\nThere are no missions. Everything is available in free roam.", body);
    }

    void DrawPause()
    {
        Rect r = new Rect(Screen.width / 2f - 180, Screen.height / 2f - 95, 360, 190);
        Panel(r, new Color(.045f, .075f, .12f, .97f));
        GUI.Label(new Rect(r.x + 24, r.y + 20, 300, 34), "PAUSED", title);
        GUI.Label(new Rect(r.x + 24, r.y + 62, 310, 60), "Harborline is an open-world sandbox.\nExplore, drive, fly, swim and experiment.", body);
        if (GUI.Button(new Rect(r.x + 24, r.y + 137, 310, 30), "RESUME", button)) pauseOpen = false;
    }

    void DrawLocation(SandboxLocation location)
    {
        Rect r = new Rect(Screen.width / 2f - 225, Screen.height / 2f - 230, 450, 460);
        Panel(r, new Color(.045f, .075f, .12f, .97f));
        GUI.Label(new Rect(r.x + 18, r.y + 12, 400, 34), location.DisplayName.ToUpperInvariant(), title);
        GUILayout.BeginArea(new Rect(r.x + 18, r.y + 55, 415, 390));
        if (location.Kind == SandboxLocation.LocationKind.WeaponShop && arsenal != null)
        {
            GUILayout.Label("Original arms and field supplies", body);
            for (int i = 2; i < SandboxArsenal.Definitions.Length; i++)
            {
                int cost = i * 110;
                if (Button(SandboxArsenal.Definitions[i].Name + "  •  $" + cost) && world.Purchase(cost)) arsenal.Give((SandboxArsenal.Weapon)i, 80);
            }
            if (Button("Ammunition pack  •  $80") && world.Purchase(80)) arsenal.RefillAll();
            if (Button("Armor  •  $250") && world.Purchase(250)) player.Armor = 100f;
        }
        else if (location.Kind == SandboxLocation.LocationKind.Garage)
        {
            var car = player.CurrentVehicle;
            GUILayout.Label(car == null ? "Enter with a vehicle to modify it." : car.name + "  •  " + Mathf.RoundToInt(car.Health) + "%", body);
            if (car != null)
            {
                if (Button("Repair  •  $150") && world.Purchase(150)) car.Repair();
                if (Button("Engine tuning  •  $500") && world.Purchase(500)) car.EngineUpgrade = Mathf.Min(3, car.EngineUpgrade + 1);
                if (Button("Brake package  •  $350") && world.Purchase(350)) car.BrakeUpgrade = Mathf.Min(3, car.BrakeUpgrade + 1);
                if (Button("Armor package  •  $650") && world.Purchase(650)) car.ArmorUpgrade = Mathf.Min(3, car.ArmorUpgrade + 1);
                if (Button("Coral paint  •  $180") && world.Purchase(180)) Paint(car, new Color(.86f, .34f, .26f));
                if (Button("Cobalt paint  •  $180") && world.Purchase(180)) Paint(car, new Color(.18f, .39f, .78f));
                if (Button("Pearl paint  •  $180") && world.Purchase(180)) Paint(car, new Color(.88f, .86f, .72f));
            }
        }
        else if (location.Kind == SandboxLocation.LocationKind.Safehouse)
        {
            GUILayout.Label("Your quiet corner of the bay.", body);
            if (Button("Save game")) world.SaveGame();
            if (Button("Sleep until morning")) world.Hour = 7f;
        }
        else if (location.Kind == SandboxLocation.LocationKind.Hospital)
        {
            GUILayout.Label("Medical services", body);
            if (Button("Restore health  •  $75") && world.Purchase(75)) player.Health = 100f;
        }
        else GUILayout.Label("This location is part of the Harborline free-roam network.", body);
        GUILayout.FlexibleSpace();
        if (Button("CLOSE")) world.CloseLocation();
        GUILayout.EndArea();
    }

    void Paint(SandboxVehicle car, Color color)
    {
        foreach (var renderer in car.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.name.ToLowerInvariant().Contains("body")) continue;
            foreach (var mat in renderer.materials) if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        }
    }
}
