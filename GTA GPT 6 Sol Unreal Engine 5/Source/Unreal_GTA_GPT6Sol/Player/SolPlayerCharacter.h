#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "Weapons/SolWeaponTypes.h"
#include "SolPlayerCharacter.generated.h"

class ASolVehicle;
class UCameraComponent;
class UInputAction;
class UInputMappingContext;
class UPointLightComponent;
class USpringArmComponent;
class UStaticMeshComponent;
class USoundBase;
struct FInputActionValue;

UCLASS(Blueprintable)
class UNREAL_GTA_GPT6SOL_API ASolPlayerCharacter : public ACharacter
{
	GENERATED_BODY()

public:
	ASolPlayerCharacter();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;
	virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;
	virtual float TakeDamage(float DamageAmount, struct FDamageEvent const& DamageEvent, class AController* EventInstigator, AActor* DamageCauser) override;

	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player") TObjectPtr<USpringArmComponent> CameraBoom;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player") TObjectPtr<UCameraComponent> FollowCamera;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player") TObjectPtr<UStaticMeshComponent> BodyVisual;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player") TObjectPtr<UStaticMeshComponent> WeaponVisual;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player") TObjectPtr<UPointLightComponent> MuzzleFlash;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Vitals") float MaxHealth = 100.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Vitals") float Health = 100.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Vitals") float Armor = 30.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Economy") int32 Cash = 1500;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Vitals") bool bGodMode = false;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Combat") TArray<FSolWeaponState> Inventory;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Combat") int32 CurrentWeaponIndex = 0;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Movement") bool bAiming = false;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Movement") bool bStealth = false;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Movement") bool bInCover = false;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Movement") bool bInWater = false;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Vitals") float Stamina = 100.f;
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Player|Vitals") float Breath = 100.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Skills", meta=(ClampMin="0", ClampMax="100")) float StaminaSkill = 0.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Skills", meta=(ClampMin="0", ClampMax="100")) float ShootingSkill = 0.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Skills", meta=(ClampMin="0", ClampMax="100")) float DrivingSkill = 0.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Skills", meta=(ClampMin="0", ClampMax="100")) float FlyingSkill = 0.f;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Player|Skills", meta=(ClampMin="0", ClampMax="100")) float LungSkill = 0.f;

	UFUNCTION(BlueprintPure, Category="Player|Vehicle") ASolVehicle* GetCurrentVehicle() const;
	UFUNCTION(BlueprintCallable, Category="Player|Vehicle") bool EnterVehicle(ASolVehicle* Vehicle);
	UFUNCTION(BlueprintCallable, Category="Player|Vehicle") void ExitVehicle();
	void OnEnteredVehicle(ASolVehicle* Vehicle);
	void OnExitedVehicle(ASolVehicle* Vehicle, FVector ExitLocation);

	UFUNCTION(BlueprintCallable, Category="Player|Combat") void GiveWeapon(ESolWeaponType Type, int32 ReserveAmmo = 0);
	UFUNCTION(BlueprintCallable, Category="Player|Combat") void SelectWeaponSlot(int32 Index);
	UFUNCTION(BlueprintCallable, Category="Player|Combat") void Reload();
	UFUNCTION(BlueprintPure, Category="Player|Combat") ESolWeaponType GetCurrentWeaponType() const;
	UFUNCTION(BlueprintPure, Category="Player|Combat") int32 GetCurrentAmmo() const;
	UFUNCTION(BlueprintPure, Category="Player|Combat") int32 GetReserveAmmo() const;
	UFUNCTION(BlueprintPure, Category="Player|Combat") FSolWeaponDefinition GetCurrentWeaponDefinition() const;
	UFUNCTION(BlueprintPure, Category="Player|Movement") bool IsStealth() const { return bStealth; }
	UFUNCTION(BlueprintPure, Category="Player|Movement") bool IsInCover() const { return bInCover; }
	UFUNCTION(BlueprintPure, Category="Player|Movement") bool IsInWater() const { return bInWater; }
	UFUNCTION(BlueprintCallable, Category="Player|Vitals") void Heal(float Amount);
	UFUNCTION(BlueprintCallable, Category="Player|Vitals") void SetGodMode(bool bEnabled);
	UFUNCTION(BlueprintCallable, Category="Player|Economy") void AddCash(int32 Amount);

private:
	UPROPERTY(Transient) TObjectPtr<UInputMappingContext> RuntimeInputContext;
	UPROPERTY(Transient) TObjectPtr<ASolVehicle> CurrentVehicle;
	UPROPERTY(Transient) TObjectPtr<USoundBase> GunshotAudio;
	UPROPERTY(Transient) TObjectPtr<USoundBase> FootstepAudio;
	float LastFootstepTime = -10.f;
	bool bForward = false;
	bool bBackward = false;
	bool bLeft = false;
	bool bRight = false;
	bool bSprinting = false;
	bool bFireHeld = false;
	bool bReloading = false;
	bool bDead = false;
	bool bFirstPerson = false;
	bool bSwimUpHeld = false;
	bool bDiveHeld = false;
	FVector CoverNormal = FVector::ZeroVector;
	FVector CoverAnchor = FVector::ZeroVector;
	float NextDrowningDamageAt = 0.f;
	float LastShotTime = -10.f;
	float LastCrimeTime = -10.f;
	FTimerHandle ReloadTimer;
	FTimerHandle FlashTimer;
	FTimerHandle RespawnTimer;

	void SetForwardOn(); void SetForwardOff();
	void SetBackwardOn(); void SetBackwardOff();
	void SetLeftOn(); void SetLeftOff();
	void SetRightOn(); void SetRightOff();
	void LookX(const FInputActionValue& Value);
	void LookY(const FInputActionValue& Value);
	void SprintOn(); void SprintOff();
	void JumpOn(); void JumpOff();
	void CrouchOrDiveOn(); void CrouchOrDiveOff();
	void ToggleStealth();
	void ToggleCover();
	void LeaveCover();
	void UpdateWater(float DeltaSeconds);
	void UpdateSkills(float DeltaSeconds);
	void AimOn(); void AimOff();
	void FireOn(); void FireOff();
	void TryFire();
	void FinishReload();
	void FinishFlash();
	void TryEnterExitVehicle();
	void Interact();
	void NextWeapon(); void PreviousWeapon();
	void Weapon1(); void Weapon2(); void Weapon3(); void Weapon4(); void Weapon5();
	void Weapon6(); void Weapon7(); void Weapon8(); void Weapon9();
	void ToggleAdmin();
	void ToggleCamera();
	void VehicleHorn();
	void VehicleLights();
	void RespawnAtHospital();
	void UpdateWeaponVisual();
};
