#pragma once

#include "CoreMinimal.h"
#include "GameFramework/SaveGame.h"
#include "Weapons/SolWeaponTypes.h"
#include "SolSaveGame.generated.h"

UCLASS()
class UNREAL_GTA_GPT6SOL_API USolSaveGame : public USaveGame
{
	GENERATED_BODY()
public:
	UPROPERTY() FVector PlayerPosition = FVector(0.f, 8000.f, 200.f);
	UPROPERTY() float Health = 100.f;
	UPROPERTY() float Armor = 0.f;
	UPROPERTY() int32 Cash = 1500;
	UPROPERTY() TArray<FSolWeaponState> Inventory;
	UPROPERTY() int32 CurrentWeaponIndex = 0;
	UPROPERTY() bool bHasStoredVehicle = false;
	UPROPERTY() uint8 StoredVehicleType = 0;
	UPROPERTY() uint8 StoredCarVariant = 0;
	UPROPERTY() int32 StoredPaintPreset = 0;
	UPROPERTY() int32 StoredEngineUpgrade = 0;
	UPROPERTY() float StaminaSkill = 0.f;
	UPROPERTY() float ShootingSkill = 0.f;
	UPROPERTY() float DrivingSkill = 0.f;
	UPROPERTY() float FlyingSkill = 0.f;
	UPROPERTY() float LungSkill = 0.f;
	UPROPERTY() float TimeHours = 14.f;
	UPROPERTY() uint8 Weather = 0;
};
