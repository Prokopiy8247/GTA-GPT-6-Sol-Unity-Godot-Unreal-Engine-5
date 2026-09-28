#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "SolVehicle.generated.h"

class ASolPlayerCharacter;
class UAudioComponent;
class UBoxComponent;
class UPointLightComponent;
class USceneComponent;
class USpotLightComponent;
class UStaticMeshComponent;
class UParticleSystem;
class USoundBase;

UENUM(BlueprintType)
enum class ESolVehicleType : uint8
{
	Car,
	Boat,
	Helicopter,
	Plane
};

UENUM(BlueprintType)
enum class ESolCarVariant : uint8
{
	Compact,
	Sedan,
	PoliceCar,
	SUV,
	Pickup,
	Van,
	SportsCar,
	Motorcycle
};

UCLASS(Blueprintable)
class UNREAL_GTA_GPT6SOL_API ASolVehicle : public AActor
{
	GENERATED_BODY()

public:
	ASolVehicle();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;
	virtual float TakeDamage(float DamageAmount, struct FDamageEvent const& DamageEvent, class AController* EventInstigator, AActor* DamageCauser) override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<UBoxComponent> Collision;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<UStaticMeshComponent> BodyMesh;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<USceneComponent> DriverSeat;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<USceneComponent> ExitPoint;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<USpotLightComponent> LeftHeadlight;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<USpotLightComponent> RightHeadlight;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<UPointLightComponent> BrakeLight;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<UPointLightComponent> FireLight;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Vehicle") TObjectPtr<UAudioComponent> EngineAudio;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") ESolVehicleType VehicleType = ESolVehicleType::Car;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") ESolCarVariant CarVariant = ESolCarVariant::Compact;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") float MaxVehicleHealth = 300.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") float VehicleHealth = 300.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") bool bPoliceVehicle = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") bool bNPCDriver = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") bool bLightsOn = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle") bool bSirenOn = false;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|Garage") int32 PaintPresetIndex = 0;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|Garage") int32 EngineUpgrade = 0;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|Audio") TObjectPtr<USoundBase> EngineSound;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|Audio") TObjectPtr<USoundBase> HornSound;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|Audio") TObjectPtr<USoundBase> SirenSound;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Vehicle|VFX") TObjectPtr<UParticleSystem> ExplosionEffect;

	UFUNCTION(BlueprintCallable, Category="Vehicle") void InitializeVehicle();
	UFUNCTION(BlueprintCallable, Category="Vehicle") bool Enter(ASolPlayerCharacter* Character);
	UFUNCTION(BlueprintCallable, Category="Vehicle") void Exit();
	UFUNCTION(BlueprintPure, Category="Vehicle") ASolPlayerCharacter* GetDriver() const { return Driver.Get(); }
	UFUNCTION(BlueprintCallable, Category="Vehicle") void SetDrivingInput(float InThrottle, float InSteering, float InBrake);
	UFUNCTION(BlueprintCallable, Category="Vehicle") void SetHandbrake(bool bEnabled);
	UFUNCTION(BlueprintCallable, Category="Vehicle") void Repair();
	UFUNCTION(BlueprintCallable, Category="Vehicle|Garage") void ApplyPaintPreset(int32 PresetIndex);
	UFUNCTION(BlueprintCallable, Category="Vehicle|Garage") void UpgradeEngine();
	UFUNCTION(BlueprintPure, Category="Vehicle") float GetSpeedKmh() const { return FMath::Abs(ForwardSpeed) * 0.036f; }
	UFUNCTION(BlueprintPure, Category="Vehicle") bool IsDestroyed() const { return bDestroyed; }
	UFUNCTION(BlueprintPure, Category="Vehicle") bool IsMotorcycle() const { return VehicleType == ESolVehicleType::Car && CarVariant == ESolCarVariant::Motorcycle; }
	UFUNCTION(BlueprintCallable, Category="Vehicle") void ToggleLights();
	UFUNCTION(BlueprintCallable, Category="Vehicle") void Horn();
	UFUNCTION(BlueprintCallable, Category="Vehicle") void SetSiren(bool bEnabled);
	UFUNCTION(BlueprintCallable, Category="Vehicle|Traffic") void SetAutopilot(const TArray<FVector>& InRoute, bool bLoop);
	UFUNCTION(BlueprintCallable, Category="Vehicle|Traffic") void SetAutopilotTarget(FVector Target);
	UFUNCTION(BlueprintPure, Category="Vehicle|Traffic") bool IsAutopilot() const { return bAutopilot; }

private:
	UPROPERTY() TWeakObjectPtr<ASolPlayerCharacter> Driver;
	UPROPERTY() TArray<FVector> Route;
	int32 RouteIndex = 0;
	bool bLoopRoute = true;
	bool bAutopilot = false;
	bool bHandbrake = false;
	bool bDestroyed = false;
	bool bPlaneAirborne = false;
	float Throttle = 0.f;
	float Steering = 0.f;
	float Brake = 0.f;
	float ForwardSpeed = 0.f;
	float VerticalSpeed = 0.f;
	float LastImpactTime = -10.f;
	float SirenTimer = 0.f;
	bool bLoggedBlockingHit = false;

	void UpdateAutopilot();
	void UpdateMovement(float DeltaSeconds);
	void UpdateVisuals(float DeltaSeconds);
	void Explode();
};
