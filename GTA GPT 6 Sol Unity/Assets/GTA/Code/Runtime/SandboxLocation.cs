using UnityEngine;

/// <summary>Walk-up interaction point for services in the missionless world.</summary>
public class SandboxLocation : MonoBehaviour
{
    public enum LocationKind { WeaponShop, Garage, Safehouse, Hospital, Clothing, GasStation, Airfield, Dock, PoliceStation }
    public LocationKind Kind;
    public string DisplayName = "Location";
    public float InteractionRadius = 6f;
}
