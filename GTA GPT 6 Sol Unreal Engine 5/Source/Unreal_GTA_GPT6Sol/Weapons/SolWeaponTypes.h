#pragma once

#include "CoreMinimal.h"
#include "SolWeaponTypes.generated.h"

UENUM(BlueprintType)
enum class ESolWeaponType : uint8
{
	Fists,
	Knife,
	Bat,
	Pistol,
	HeavyPistol,
	SMG,
	Shotgun,
	Rifle,
	Sniper,
	Grenade,
	Launcher
};

USTRUCT(BlueprintType)
struct FSolWeaponDefinition
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite) FText DisplayName;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) float Damage = 10.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) float Range = 10000.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) float FireInterval = 0.25f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) float SpreadDegrees = 2.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 MagazineSize = 12;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 Pellets = 1;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bAutomatic = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) bool bMelee = false;
};

USTRUCT(BlueprintType)
struct FSolWeaponState
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite) ESolWeaponType Type = ESolWeaponType::Fists;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 MagazineAmmo = 0;
	UPROPERTY(EditAnywhere, BlueprintReadWrite) int32 ReserveAmmo = 0;
};

UNREAL_GTA_GPT6SOL_API FSolWeaponDefinition SolGetWeaponDefinition(ESolWeaponType Type);
