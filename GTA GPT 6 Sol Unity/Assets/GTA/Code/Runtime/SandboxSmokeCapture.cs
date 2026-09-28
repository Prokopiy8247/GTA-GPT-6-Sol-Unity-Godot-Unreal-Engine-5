using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Opt-in automated verification for an actual standalone game run.</summary>
public class SandboxSmokeCapture : MonoBehaviour
{
    string capturePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallIfRequested()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "-smokecapture" && i + 1 < args.Length)
            {
                var go = new GameObject("Automated Smoke Capture");
                go.AddComponent<SandboxSmokeCapture>().capturePath = args[i + 1];
                return;
            }
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3f);
        // Batch player runs have no physical keyboard or mouse. Virtual devices let
        // this check exercise the same Input System paths as an interactive run.
        if (Keyboard.current == null) { InputSystem.AddDevice<Keyboard>(); Debug.Log("SMOKE virtual keyboard installed"); }
        if (Mouse.current == null) { InputSystem.AddDevice<Mouse>(); Debug.Log("SMOKE virtual mouse installed"); }
        var player = FindFirstObjectByType<SandboxPlayer>();
        var director = SandboxDirector.Instance;
        var arsenal = player != null ? player.GetComponent<SandboxArsenal>() : null;
        var cam = Camera.main;
        var vehicles = FindObjectsByType<SandboxVehicle>(FindObjectsSortMode.None);
        var civilians = FindObjectsByType<SandboxPedestrian>(FindObjectsSortMode.None);
        var places = FindObjectsByType<SandboxLocation>(FindObjectsSortMode.None);
        Debug.Log("SMOKE initial player=" + (player != null) + " director=" + (director != null) + " camera=" + (cam != null)
            + " arsenal=" + (arsenal != null) + " vehicles=" + vehicles.Length + " actors=" + civilians.Length + " services=" + places.Length);
        Debug.Log("SMOKE initial-positions player=" + (player != null ? player.transform.position.ToString("F2") : "null")
            + " camera=" + (cam != null ? cam.transform.position.ToString("F2") : "null"));

        // Observe the clean scene before teleports, virtual input, police, or weather checks.
        var pickup = GameObject.Find("Traffic Pickup 04")?.GetComponent<SandboxVehicle>();
        var sportsCar = GameObject.Find("Traffic SportsCar 02")?.GetComponent<SandboxVehicle>();
        var portVan = GameObject.Find("Traffic Van 08")?.GetComponent<SandboxVehicle>();
        var pickupAgent = pickup != null ? pickup.GetComponent<SandboxTrafficAgent>() : null;
        var sportsAgent = sportsCar != null ? sportsCar.GetComponent<SandboxTrafficAgent>() : null;
        bool trafficReady = player != null && director != null && director.TrafficEnabled
            && pickupAgent != null && pickupAgent.Waypoints != null && pickupAgent.Waypoints.Length > 1
            && sportsAgent != null && sportsAgent.Waypoints != null && sportsAgent.Waypoints.Length > 1
            && Vector3.Distance(pickup.transform.position, player.transform.position) < 210f
            && Vector3.Distance(sportsCar.transform.position, player.transform.position) < 210f;
        Vector3 pickupStart = pickup != null ? pickup.transform.position : Vector3.zero;
        Vector3 sportsStart = sportsCar != null ? sportsCar.transform.position : Vector3.zero;
        Vector3 portVanStart = portVan != null ? portVan.transform.position : Vector3.zero;
        SandboxPedestrian watchedCivilian = null;
        float civilianDistance = 90f * 90f;
        if (player != null)
            foreach (var actor in civilians)
            {
                if (actor == null || actor.IsDead || actor.Role != SandboxPedestrian.ActorRole.Civilian) continue;
                float distance = (actor.transform.position - player.transform.position).sqrMagnitude;
                if (distance >= civilianDistance) continue;
                civilianDistance = distance;
                watchedCivilian = actor;
            }
        Vector3 civilianStart = watchedCivilian != null ? watchedCivilian.transform.position : Vector3.zero;
        bool hadCivilian = watchedCivilian != null;
        yield return new WaitForSecondsRealtime(5f);
        if (!trafficReady)
            Debug.Log("SMOKE initial-traffic-movement=INCONCLUSIVE: expected Pickup 04 and SportsCar 02 with routes, traffic enabled, and both within 210m of player");
        else if (pickup == null || sportsCar == null)
            Debug.Log("SMOKE initial-traffic-movement=FAIL: an observed traffic vehicle disappeared during the five-second window");
        else
        {
            float pickupMeters = Vector3.Distance(pickupStart, pickup.transform.position);
            float sportsMeters = Vector3.Distance(sportsStart, sportsCar.transform.position);
            bool moving = pickupMeters > 5f && sportsMeters > 5f;
            string reason = moving ? "both named vehicles moved"
                : !pickup.IsGrounded || !sportsCar.IsGrounded ? "wheel contact lost; inspect suspension and road colliders"
                : pickup.SpeedKph < 3f && sportsCar.SpeedKph < 3f ? "both stayed nearly stopped; inspect route or blocking traffic"
                : "at least one vehicle moved under 5m; inspect steering and collisions";
            Debug.Log("SMOKE initial-traffic-movement=" + (moving ? "PASS" : "FAIL")
                + " pickup-m=" + pickupMeters.ToString("F2") + " sports-m=" + sportsMeters.ToString("F2")
                + " pickup-kph=" + pickup.SpeedKph.ToString("F1") + " sports-kph=" + sportsCar.SpeedKph.ToString("F1")
                + " grounded=" + pickup.IsGrounded + "/" + sportsCar.IsGrounded
                + " sports-start=" + sportsStart.ToString("F2")
                + " sports-end=" + sportsCar.transform.position.ToString("F2")
                + " sports-yaw=" + sportsCar.transform.eulerAngles.y.ToString("F1")
                + " sports-health=" + sportsCar.Health.ToString("F1")
                + " sports-next=" + (sportsAgent != null ? sportsAgent.NextWaypoint.ToString() : "none")
                + " reason=" + reason);
        }
        if (portVan == null)
            Debug.Log("SMOKE port-service-van=INCONCLUSIVE: Traffic Van 08 is absent or disappeared");
        else
            Debug.Log("SMOKE port-service-van-m=" + Vector3.Distance(portVanStart, portVan.transform.position).ToString("F2")
                + " speed-kph=" + portVan.SpeedKph.ToString("F1") + " grounded=" + portVan.IsGrounded);
        if (!hadCivilian)
            Debug.Log("SMOKE civilian-movement=INCONCLUSIVE: no living civilian within 90m of the clean player spawn");
        else if (watchedCivilian == null)
            Debug.Log("SMOKE civilian-movement=INCONCLUSIVE: observed civilian disappeared during the five-second window");
        else
        {
            float civilianMeters = Vector3.Distance(civilianStart, watchedCivilian.transform.position);
            Debug.Log("SMOKE civilian-movement-m=" + civilianMeters.ToString("F2")
                + " over-0.5m=" + (civilianMeters > .5f) + " actor=" + watchedCivilian.name);
        }
        if (cam != null && !string.IsNullOrEmpty(capturePath))
        {
            yield return new WaitForEndOfFrame();
            CaptureCamera(cam, capturePath);
            if (!Application.isBatchMode)
            {
                string hudPath = Path.Combine(Path.GetDirectoryName(capturePath),
                    Path.GetFileNameWithoutExtension(capturePath) + "_hud.png");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(hudPath));
                yield return new WaitForSecondsRealtime(.25f);
            }
            else Debug.Log("SMOKE hud-ui-image=UNAVAILABLE_BATCH");
            if (player != null && player.CameraRig != null)
            {
                Vector3 origin = player.transform.position;
                Vector3[] probePositions =
                {
                    new Vector3(0f, .30f, -147f),
                    new Vector3(-215f, .30f, -155f),
                    new Vector3(-120f, .30f, 172f),
                    new Vector3(162f, .30f, 205f)
                };
                string[] probeNames = { "boulevard", "beach", "park", "airfield" };
                for (int i = 0; i < probePositions.Length; i++)
                {
                    player.Teleport(probePositions[i]);
                    yield return new WaitForEndOfFrame();
                    string probePath = Path.Combine(Path.GetDirectoryName(capturePath),
                        Path.GetFileNameWithoutExtension(capturePath) + "_" + probeNames[i] + ".png");
                    CaptureCamera(cam, probePath);
                }
                player.Teleport(origin);
                yield return new WaitForEndOfFrame();
            }
        }
        if (player != null && Keyboard.current != null)
        {
            var keyboard = Keyboard.current;
            Vector3 before = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
            yield return new WaitForSecondsRealtime(0.75f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Debug.Log("SMOKE keyboard-move-meters=" + Vector3.Distance(before, player.transform.position).ToString("F2"));

            var hud = FindAnyObjectByType<SandboxHUD>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F1));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Debug.Log("SMOKE admin-menu-open=" + (hud != null && hud.AdminOpen));
            if (hud != null && hud.AdminOpen && !string.IsNullOrEmpty(capturePath) && !Application.isBatchMode)
            {
                string adminPath = Path.Combine(Path.GetDirectoryName(capturePath),
                    Path.GetFileNameWithoutExtension(capturePath) + "_admin.png");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(adminPath));
                yield return new WaitForSecondsRealtime(.25f);
            }
            else if (hud != null && hud.AdminOpen && Application.isBatchMode)
                Debug.Log("SMOKE admin-ui-image=UNAVAILABLE_BATCH");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
        if (arsenal != null && Mouse.current != null)
        {
            int ammoBefore = arsenal.CurrentAmmo;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            yield return null;
            Debug.Log("SMOKE pistol-shot-ammo=" + ammoBefore + "->" + arsenal.CurrentAmmo);
        }
        bool saveTest = System.Array.Exists(System.Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-smokesave", System.StringComparison.OrdinalIgnoreCase));
        if (saveTest && player != null && director != null)
        {
            int savedCash = player.Cash;
            int savedAmmo = arsenal != null ? arsenal.CurrentAmmo : -1;
            director.SaveGame();
            player.Cash = savedCash + 17;
            if (arsenal != null) arsenal.Loaded[arsenal.SelectedIndex] = 0;
            director.LoadGame(false);
            Debug.Log("SMOKE save-load-cash=" + savedCash + "->" + player.Cash
                + " ammo=" + savedAmmo + "->" + (arsenal != null ? arsenal.CurrentAmmo : -1)
                + " restored=" + (player.Cash == savedCash && (arsenal == null || arsenal.CurrentAmmo == savedAmmo)));
        }
        if (player != null && director != null)
        {
            director.SetWantedLevel(3);
            Debug.Log("SMOKE wanted-three=" + director.WantedLevel + " pursuit=" + director.InPursuit);
            director.SetWantedLevel(0);
            SandboxVehicle template = null;
            foreach (var candidate in vehicles)
                if (candidate.Kind == SandboxVehicle.VehicleKind.Car && !candidate.IsPolice
                    && candidate.name.Contains("SportsCar"))
                { template = candidate; break; }
            if (template == null)
            {
                foreach (var candidate in vehicles)
                    if (candidate.Kind == SandboxVehicle.VehicleKind.Car && !candidate.IsPolice)
                    { template = candidate; break; }
            }
            if (template != null)
            {
                Debug.Log("SMOKE vehicle-template=" + template.name + " chassis=" + template.ChassisSize.ToString("F2"));
                var vehicle = director.SpawnVehicle(template, new Vector3(0f, .55f, -54f));
                Quaternion south = Quaternion.Euler(0f, 180f, 0f);
                vehicle.transform.rotation = south;
                if (vehicle.Body != null) vehicle.Body.rotation = south;
                bool entered = player.EnterVehicle(vehicle);
                Debug.Log("SMOKE vehicle-enter=" + entered + " occupied=" + player.IsInVehicle + " class=" + vehicle.Kind);
                if (entered)
                {
                    Vector3 beforeDrive = vehicle.transform.position;
                    if (Keyboard.current != null)
                    {
                        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
                        yield return new WaitForSecondsRealtime(.45f);
                        Debug.Log("SMOKE drive-input W=" + Keyboard.current.wKey.isPressed
                            + " player-input=" + player.InputEnabled
                            + " grounded=" + vehicle.IsGrounded
                            + " kinematic=" + (vehicle.Body != null && vehicle.Body.isKinematic)
                            + " speed-mps=" + (vehicle.Body != null ? vehicle.Body.linearVelocity.magnitude.ToString("F2") : "unknown"));
                        yield return new WaitForSecondsRealtime(3.55f);
                        Debug.Log("SMOKE drive-end-held W=" + Keyboard.current.wKey.isPressed
                            + " grounded=" + vehicle.IsGrounded
                            + " destroyed=" + vehicle.IsDestroyed + " health=" + vehicle.Health.ToString("F1")
                            + " position=" + vehicle.transform.position.ToString("F2")
                            + " yaw=" + vehicle.transform.eulerAngles.y.ToString("F1")
                            + " speed-mps=" + (vehicle.Body != null ? vehicle.Body.linearVelocity.magnitude.ToString("F2") : "unknown"));
                        if (cam != null && !string.IsNullOrEmpty(capturePath))
                        {
                            string drivePath = Path.Combine(Path.GetDirectoryName(capturePath),
                                Path.GetFileNameWithoutExtension(capturePath) + "_driving.png");
                            CaptureCamera(cam, drivePath);
                        }
                        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                        yield return null;
                    }
                    Debug.Log("SMOKE car-drive-meters=" + Vector3.Distance(beforeDrive, vehicle.transform.position).ToString("F2")
                        + " speed-mps=" + (vehicle.Body != null ? vehicle.Body.linearVelocity.magnitude.ToString("F2") : "unknown")
                        + " crossed-z-minus-80=" + (vehicle.transform.position.z < -84f));
                    player.ExitVehicle();
                    player.CameraRig?.SnapToTarget();
                }
            }
            director.SetWeather(SandboxDirector.WeatherState.Rain);
            Debug.Log("SMOKE weather=" + director.Weather + " health=" + player.Health + " free-roam=" + !player.IsInVehicle);
            director.SetWeather(SandboxDirector.WeatherState.Clear);
        }
        if (cam != null && !string.IsNullOrEmpty(capturePath))
        {
            yield return new WaitForEndOfFrame();
            Debug.Log("SMOKE post-actions positions player=" + player.transform.position.ToString("F2")
                + " camera=" + cam.transform.position.ToString("F2"));
            string postPath = Path.Combine(Path.GetDirectoryName(capturePath),
                Path.GetFileNameWithoutExtension(capturePath) + "_after.png");
            CaptureCamera(cam, postPath);
        }
        yield return new WaitForSecondsRealtime(2f);
        Application.Quit(0);
    }

    static void CaptureCamera(Camera cam, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        RenderTexture target = null;
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = cam.targetTexture;
        try
        {
            target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = target;
            cam.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            Debug.Log("SMOKE render-target screenshot=" + path);
        }
        catch (System.Exception e) { Debug.LogError("SMOKE screenshot failed: " + e); }
        finally
        {
            cam.targetTexture = previousTarget;
            RenderTexture.active = previous;
            if (target != null) RenderTexture.ReleaseTemporary(target);
        }
    }
}
