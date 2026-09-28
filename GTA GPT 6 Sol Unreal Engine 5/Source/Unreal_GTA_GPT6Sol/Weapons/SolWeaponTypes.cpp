#include "Weapons/SolWeaponTypes.h"

FSolWeaponDefinition SolGetWeaponDefinition(ESolWeaponType Type)
{
	FSolWeaponDefinition Result;
	switch (Type)
	{
	case ESolWeaponType::Fists:
		Result.DisplayName = FText::FromString(TEXT("Fists")); Result.Damage = 16.f; Result.Range = 180.f; Result.FireInterval = 0.5f; Result.MagazineSize = 0; Result.bMelee = true; break;
	case ESolWeaponType::Knife:
		Result.DisplayName = FText::FromString(TEXT("Knife")); Result.Damage = 34.f; Result.Range = 190.f; Result.FireInterval = 0.42f; Result.MagazineSize = 0; Result.bMelee = true; break;
	case ESolWeaponType::Bat:
		Result.DisplayName = FText::FromString(TEXT("Bat")); Result.Damage = 29.f; Result.Range = 220.f; Result.FireInterval = 0.65f; Result.MagazineSize = 0; Result.bMelee = true; break;
	case ESolWeaponType::Pistol:
		Result.DisplayName = FText::FromString(TEXT("Pistol")); Result.Damage = 28.f; Result.FireInterval = 0.24f; Result.SpreadDegrees = 1.8f; Result.MagazineSize = 12; break;
	case ESolWeaponType::HeavyPistol:
		Result.DisplayName = FText::FromString(TEXT("Heavy Pistol")); Result.Damage = 48.f; Result.FireInterval = 0.43f; Result.SpreadDegrees = 2.2f; Result.MagazineSize = 8; break;
	case ESolWeaponType::SMG:
		Result.DisplayName = FText::FromString(TEXT("Compact SMG")); Result.Damage = 17.f; Result.FireInterval = 0.09f; Result.SpreadDegrees = 4.2f; Result.MagazineSize = 30; Result.bAutomatic = true; break;
	case ESolWeaponType::Shotgun:
		Result.DisplayName = FText::FromString(TEXT("Pump Shotgun")); Result.Damage = 11.f; Result.Range = 4500.f; Result.FireInterval = 0.8f; Result.SpreadDegrees = 6.f; Result.MagazineSize = 8; Result.Pellets = 8; break;
	case ESolWeaponType::Rifle:
		Result.DisplayName = FText::FromString(TEXT("Carbine")); Result.Damage = 32.f; Result.FireInterval = 0.12f; Result.SpreadDegrees = 2.2f; Result.MagazineSize = 30; Result.bAutomatic = true; break;
	case ESolWeaponType::Sniper:
		Result.DisplayName = FText::FromString(TEXT("Precision Rifle")); Result.Damage = 90.f; Result.Range = 25000.f; Result.FireInterval = 1.1f; Result.SpreadDegrees = 0.15f; Result.MagazineSize = 5; break;
	case ESolWeaponType::Grenade:
		Result.DisplayName = FText::FromString(TEXT("Grenade")); Result.Damage = 95.f; Result.Range = 2200.f; Result.FireInterval = 1.f; Result.MagazineSize = 1; break;
	case ESolWeaponType::Launcher:
		Result.DisplayName = FText::FromString(TEXT("Launcher")); Result.Damage = 140.f; Result.Range = 17000.f; Result.FireInterval = 1.35f; Result.MagazineSize = 1; break;
	}
	return Result;
}
