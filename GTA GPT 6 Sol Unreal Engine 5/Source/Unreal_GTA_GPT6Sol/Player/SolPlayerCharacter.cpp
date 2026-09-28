#include "Player/SolPlayerCharacter.h"

#include "Vehicles/SolVehicle.h"
#include "World/SolWorldDirector.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/LocalPlayer.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "Engine/DamageEvents.h"
#include "EngineUtils.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/DamageType.h"
#include "GameFramework/PlayerController.h"
#include "GameFramework/SpringArmComponent.h"
#include "InputAction.h"
#include "InputActionValue.h"
#include "InputCoreTypes.h"
#include "InputMappingContext.h"
#include "Kismet/GameplayStatics.h"
#include "Sound/SoundBase.h"
#include "TimerManager.h"

namespace
{
	UInputAction* MapSolKey(UObject* Outer, UInputMappingContext* Context, const FKey& Key, EInputActionValueType ValueType = EInputActionValueType::Boolean)
	{
		UInputAction* Action = NewObject<UInputAction>(Outer);
		Action->ValueType = ValueType;
		Context->MapKey(Action, Key);
		return Action;
	}
}

ASolPlayerCharacter::ASolPlayerCharacter()
{
	PrimaryActorTick.bCanEverTick = true;
	GetCapsuleComponent()->InitCapsuleSize(42.f, 88.f);
	GetCapsuleComponent()->SetCollisionProfileName(TEXT("Pawn"));
	GetCharacterMovement()->MaxWalkSpeed = 450.f;
	GetCharacterMovement()->MaxWalkSpeedCrouched = 235.f;
	GetCharacterMovement()->JumpZVelocity = 530.f;
	GetCharacterMovement()->AirControl = 0.28f;
	GetCharacterMovement()->GetNavAgentPropertiesRef().bCanCrouch = true;
	GetCharacterMovement()->GetNavAgentPropertiesRef().bCanSwim = true;
	GetCharacterMovement()->MaxSwimSpeed = 310.f;
	GetCharacterMovement()->Buoyancy = 1.12f;
	GetCharacterMovement()->bOrientRotationToMovement = true;
	bUseControllerRotationYaw = false;

	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(GetCapsuleComponent());
	CameraBoom->SetRelativeLocation(FVector(0.f, 0.f, 65.f));
	CameraBoom->TargetArmLength = 360.f;
	CameraBoom->bUsePawnControlRotation = true;
	CameraBoom->bDoCollisionTest = true;
	CameraBoom->bEnableCameraLag = true;
	CameraBoom->CameraLagSpeed = 12.f;
	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
	FollowCamera->bUsePawnControlRotation = false;
	FollowCamera->SetFieldOfView(80.f);

	BodyVisual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("BodyVisual"));
	BodyVisual->SetupAttachment(GetCapsuleComponent());
	BodyVisual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	BodyVisual->SetRelativeLocation(FVector(0.f, 0.f, -88.f));
	GetMesh()->SetVisibility(false);
	GetMesh()->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	WeaponVisual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("WeaponVisual"));
	WeaponVisual->SetupAttachment(GetCapsuleComponent());
	WeaponVisual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	WeaponVisual->SetRelativeLocation(FVector(28.f, 32.f, 1.f));
	MuzzleFlash = CreateDefaultSubobject<UPointLightComponent>(TEXT("MuzzleFlash"));
	MuzzleFlash->SetupAttachment(WeaponVisual);
	MuzzleFlash->SetRelativeLocation(FVector(35.f, 0.f, 0.f));
	MuzzleFlash->SetLightColor(FLinearColor(1.f, 0.55f, 0.12f));
	MuzzleFlash->SetAttenuationRadius(275.f);
	MuzzleFlash->SetIntensity(0.f);
}

void ASolPlayerCharacter::BeginPlay()
{
	Super::BeginPlay();
	UStaticMesh* PlayerMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Game/GTA/Generated/SM_Player.SM_Player"));
	const bool bFallback = PlayerMesh == nullptr;
	if (bFallback) PlayerMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cylinder.Cylinder"));
	BodyVisual->SetStaticMesh(PlayerMesh);
	if (bFallback)
	{
		BodyVisual->SetRelativeLocation(FVector(0.f, 0.f, -4.f));
		BodyVisual->SetRelativeScale3D(FVector(0.56f, 0.45f, 1.65f));
	}
	else
	{
		const FBoxSphereBounds Bounds = PlayerMesh->GetBounds();
		const float Height = FMath::Max(1.f, Bounds.BoxExtent.Z * 2.f);
		const float UniformScale = FMath::Clamp(180.f / Height, 0.01f, 100.f);
		BodyVisual->SetRelativeScale3D(FVector(UniformScale));
		BodyVisual->SetRelativeLocation(FVector(-Bounds.Origin.X * UniformScale,
			-Bounds.Origin.Y * UniformScale, -88.f - (Bounds.Origin.Z - Bounds.BoxExtent.Z) * UniformScale));
	}
	GunshotAudio=LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_Gunshot.SFX_Gunshot"));
	FootstepAudio=LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_Footstep.SFX_Footstep"));
	Inventory.Reset();
	GiveWeapon(ESolWeaponType::Fists);
	GiveWeapon(ESolWeaponType::Pistol, 72);
	SelectWeaponSlot(1);
}

void ASolPlayerCharacter::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (bDead) return;
	UpdateWater(DeltaSeconds);
	UpdateSkills(DeltaSeconds);
	if (ASolVehicle* Vehicle = GetCurrentVehicle())
	{
		Vehicle->SetDrivingInput((bForward ? 1.f : 0.f) - (bBackward ? 1.f : 0.f),
			(bRight ? 1.f : 0.f) - (bLeft ? 1.f : 0.f), 0.f);
	}
	else if (Controller)
	{
		const FRotator FlatRotation(0.f, Controller->GetControlRotation().Yaw, 0.f);
		const float ForwardAxis = (bForward ? 1.f : 0.f) - (bBackward ? 1.f : 0.f);
		const float RightAxis = (bRight ? 1.f : 0.f) - (bLeft ? 1.f : 0.f);
		if (bInCover)
		{
			FVector AlongCover = FVector::CrossProduct(FVector::UpVector, CoverNormal).GetSafeNormal();
			if (FVector::DotProduct(AlongCover, FRotationMatrix(FlatRotation).GetUnitAxis(EAxis::Y)) < 0.f) AlongCover *= -1.f;
			if (RightAxis != 0.f) AddMovementInput(AlongCover, RightAxis);
			if (bBackward || FVector::DistSquared2D(GetActorLocation(), CoverAnchor) > FMath::Square(380.f)) LeaveCover();
		}
		else
		{
			if (ForwardAxis != 0.f) AddMovementInput(FRotationMatrix(FlatRotation).GetUnitAxis(EAxis::X), ForwardAxis);
			if (RightAxis != 0.f) AddMovementInput(FRotationMatrix(FlatRotation).GetUnitAxis(EAxis::Y), RightAxis);
		}
		const bool bCanSprint = bSprinting && Stamina > 2.f && !bStealth && !bInCover && !bAiming && !bInWater;
		GetCharacterMovement()->MaxWalkSpeed = bInWater ? 310.f : bInCover ? 185.f : bStealth ? 205.f : bAiming ? 255.f : bCanSprint ? 720.f + StaminaSkill * 1.4f : 450.f;
	}
	const float DesiredArm = bFirstPerson ? 0.f : GetCurrentVehicle() ? (GetCurrentVehicle()->IsMotorcycle() ? 390.f : 515.f) : bAiming ? 235.f : 360.f;
	CameraBoom->TargetArmLength = FMath::FInterpTo(CameraBoom->TargetArmLength, DesiredArm, DeltaSeconds, 9.f);
	CameraBoom->SocketOffset = FMath::VInterpTo(CameraBoom->SocketOffset,
		bAiming && !bFirstPerson ? FVector(0.f, 65.f, 25.f) : FVector::ZeroVector, DeltaSeconds, 8.f);
	FollowCamera->SetFieldOfView(FMath::FInterpTo(FollowCamera->FieldOfView,
		bAiming ? GetCurrentWeaponType() == ESolWeaponType::Sniper ? 28.f : 62.f : 80.f, DeltaSeconds, 8.f));
	if (bFireHeld && SolGetWeaponDefinition(GetCurrentWeaponType()).bAutomatic) TryFire();
	if (!GetCurrentVehicle() && !IsInWater() && GetVelocity().SizeSquared2D()>FMath::Square(80.f) && FootstepAudio)
	{
		const float Now=GetWorld()->GetTimeSeconds();
		const float Interval=IsStealth() ? 0.85f : bSprinting ? 0.34f : 0.53f;
		if (Now-LastFootstepTime>Interval)
		{
			LastFootstepTime=Now;
			UGameplayStatics::PlaySoundAtLocation(this,FootstepAudio,GetActorLocation(),IsStealth() ? 0.14f : 0.24f);
		}
	}
}

void ASolPlayerCharacter::UpdateWater(float DeltaSeconds)
{
	constexpr float SeaSurfaceZ = -30.f;
	const bool bNowInWater = !GetCurrentVehicle() && GetActorLocation().Y < -19000.f &&
		GetActorLocation().Z < (bInWater ? SeaSurfaceZ + 165.f : SeaSurfaceZ + 110.f);
	if (bNowInWater && !bInWater)
	{
		bInWater = true;
		bStealth = false;
		LeaveCover();
		UnCrouch();
		GetCharacterMovement()->SetMovementMode(MOVE_Swimming);
	}
	else if (!bNowInWater && bInWater)
	{
		bInWater = false;
		bSwimUpHeld = false;
		bDiveHeld = false;
		if (GetCharacterMovement()->MovementMode == MOVE_Swimming) GetCharacterMovement()->SetMovementMode(MOVE_Falling);
	}
	if (!bInWater) return;
	if (GetCharacterMovement()->MovementMode != MOVE_Swimming) GetCharacterMovement()->SetMovementMode(MOVE_Swimming);
	if (bSwimUpHeld) AddMovementInput(FVector::UpVector, 1.f);
	else if (bDiveHeld) AddMovementInput(FVector::DownVector, 1.f);
	else if (GetActorLocation().Z < SeaSurfaceZ + 34.f) AddMovementInput(FVector::UpVector, 0.85f);
	else if (GetActorLocation().Z > SeaSurfaceZ + 70.f) AddMovementInput(FVector::DownVector, 0.4f);

	const bool bHeadSubmerged = GetActorLocation().Z + 72.f < SeaSurfaceZ;
	const float MaxBreath = 100.f + LungSkill * 0.5f;
	if (bHeadSubmerged)
	{
		Breath = FMath::Max(0.f, Breath - 9.f * DeltaSeconds / (1.f + LungSkill * 0.008f));
		LungSkill = FMath::Min(100.f, LungSkill + DeltaSeconds * 0.05f);
		if (Breath <= 0.f && GetWorld()->GetTimeSeconds() >= NextDrowningDamageAt)
		{
			NextDrowningDamageAt = GetWorld()->GetTimeSeconds() + 1.8f;
			TakeDamage(8.f, FDamageEvent(), nullptr, nullptr);
		}
	}
	else Breath = FMath::Min(MaxBreath, Breath + 23.f * DeltaSeconds);
}

void ASolPlayerCharacter::UpdateSkills(float DeltaSeconds)
{
	const float MaxStamina = 100.f + StaminaSkill * 0.5f;
	const bool bWorkingSprint = bSprinting && !bStealth && !bInCover && !bInWater && !GetCurrentVehicle() &&
		GetVelocity().SizeSquared2D() > FMath::Square(180.f) && Stamina > 2.f;
	if (bWorkingSprint)
	{
		Stamina = FMath::Max(0.f, Stamina - 15.f * DeltaSeconds / (1.f + StaminaSkill * 0.006f));
		StaminaSkill = FMath::Min(100.f, StaminaSkill + DeltaSeconds * 0.035f);
	}
	else Stamina = FMath::Min(MaxStamina, Stamina + 13.f * DeltaSeconds);
	if (const ASolVehicle* Vehicle = GetCurrentVehicle())
	{
		if (Vehicle->GetSpeedKmh() > 15.f)
		{
			if (Vehicle->VehicleType == ESolVehicleType::Helicopter || Vehicle->VehicleType == ESolVehicleType::Plane)
				FlyingSkill = FMath::Min(100.f, FlyingSkill + DeltaSeconds * 0.035f);
			else DrivingSkill = FMath::Min(100.f, DrivingSkill + DeltaSeconds * 0.045f);
		}
	}
}

void ASolPlayerCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);
	UEnhancedInputComponent* Input = Cast<UEnhancedInputComponent>(PlayerInputComponent);
	if (!Input) return;
	RuntimeInputContext = NewObject<UInputMappingContext>(this);

	auto BindButton = [this, Input](const FKey& Key, void (ASolPlayerCharacter::*OnPress)(), void (ASolPlayerCharacter::*OnRelease)() = nullptr)
	{
		UInputAction* Action = MapSolKey(this, RuntimeInputContext, Key);
		Input->BindAction(Action, ETriggerEvent::Started, this, OnPress);
		if (OnRelease)
		{
			Input->BindAction(Action, ETriggerEvent::Completed, this, OnRelease);
			Input->BindAction(Action, ETriggerEvent::Canceled, this, OnRelease);
		}
	};
	BindButton(EKeys::W, &ASolPlayerCharacter::SetForwardOn, &ASolPlayerCharacter::SetForwardOff);
	BindButton(EKeys::S, &ASolPlayerCharacter::SetBackwardOn, &ASolPlayerCharacter::SetBackwardOff);
	BindButton(EKeys::A, &ASolPlayerCharacter::SetLeftOn, &ASolPlayerCharacter::SetLeftOff);
	BindButton(EKeys::D, &ASolPlayerCharacter::SetRightOn, &ASolPlayerCharacter::SetRightOff);
	BindButton(EKeys::LeftShift, &ASolPlayerCharacter::SprintOn, &ASolPlayerCharacter::SprintOff);
	BindButton(EKeys::SpaceBar, &ASolPlayerCharacter::JumpOn, &ASolPlayerCharacter::JumpOff);
	BindButton(EKeys::LeftControl, &ASolPlayerCharacter::CrouchOrDiveOn, &ASolPlayerCharacter::CrouchOrDiveOff);
	BindButton(EKeys::C, &ASolPlayerCharacter::ToggleStealth);
	BindButton(EKeys::X, &ASolPlayerCharacter::ToggleCover);
	BindButton(EKeys::RightMouseButton, &ASolPlayerCharacter::AimOn, &ASolPlayerCharacter::AimOff);
	BindButton(EKeys::LeftMouseButton, &ASolPlayerCharacter::FireOn, &ASolPlayerCharacter::FireOff);
	BindButton(EKeys::F, &ASolPlayerCharacter::TryEnterExitVehicle);
	BindButton(EKeys::E, &ASolPlayerCharacter::Interact);
	BindButton(EKeys::R, &ASolPlayerCharacter::Reload);
	BindButton(EKeys::Tab, &ASolPlayerCharacter::ToggleAdmin);
	BindButton(EKeys::V, &ASolPlayerCharacter::ToggleCamera);
	BindButton(EKeys::H, &ASolPlayerCharacter::VehicleHorn);
	BindButton(EKeys::L, &ASolPlayerCharacter::VehicleLights);
	BindButton(EKeys::Q, &ASolPlayerCharacter::PreviousWeapon);
	BindButton(EKeys::MouseScrollUp, &ASolPlayerCharacter::NextWeapon);
	BindButton(EKeys::MouseScrollDown, &ASolPlayerCharacter::PreviousWeapon);
	BindButton(EKeys::One, &ASolPlayerCharacter::Weapon1);
	BindButton(EKeys::Two, &ASolPlayerCharacter::Weapon2);
	BindButton(EKeys::Three, &ASolPlayerCharacter::Weapon3);
	BindButton(EKeys::Four, &ASolPlayerCharacter::Weapon4);
	BindButton(EKeys::Five, &ASolPlayerCharacter::Weapon5);
	BindButton(EKeys::Six, &ASolPlayerCharacter::Weapon6);
	BindButton(EKeys::Seven, &ASolPlayerCharacter::Weapon7);
	BindButton(EKeys::Eight, &ASolPlayerCharacter::Weapon8);
	BindButton(EKeys::Nine, &ASolPlayerCharacter::Weapon9);
	Input->BindAction(MapSolKey(this, RuntimeInputContext, EKeys::MouseX, EInputActionValueType::Axis1D),
		ETriggerEvent::Triggered, this, &ASolPlayerCharacter::LookX);
	Input->BindAction(MapSolKey(this, RuntimeInputContext, EKeys::MouseY, EInputActionValueType::Axis1D),
		ETriggerEvent::Triggered, this, &ASolPlayerCharacter::LookY);
	if (APlayerController* PC = Cast<APlayerController>(GetController()))
	{
		if (UEnhancedInputLocalPlayerSubsystem* Subsystem = ULocalPlayer::GetSubsystem<UEnhancedInputLocalPlayerSubsystem>(PC->GetLocalPlayer()))
		{
			Subsystem->AddMappingContext(RuntimeInputContext, 0);
		}
	}
}

void ASolPlayerCharacter::SetForwardOn() { bForward = true; }
void ASolPlayerCharacter::SetForwardOff() { bForward = false; }
void ASolPlayerCharacter::SetBackwardOn() { bBackward = true; }
void ASolPlayerCharacter::SetBackwardOff() { bBackward = false; }
void ASolPlayerCharacter::SetLeftOn() { bLeft = true; }
void ASolPlayerCharacter::SetLeftOff() { bLeft = false; }
void ASolPlayerCharacter::SetRightOn() { bRight = true; }
void ASolPlayerCharacter::SetRightOff() { bRight = false; }
void ASolPlayerCharacter::LookX(const FInputActionValue& Value) { AddControllerYawInput(Value.Get<float>() * 1.1f); }
void ASolPlayerCharacter::LookY(const FInputActionValue& Value) { AddControllerPitchInput(-Value.Get<float>() * 1.1f); }
void ASolPlayerCharacter::SprintOn() { bSprinting = true; if (bStealth) ToggleStealth(); }
void ASolPlayerCharacter::SprintOff() { bSprinting = false; }
void ASolPlayerCharacter::JumpOn()
{
	if (ASolVehicle* Vehicle = GetCurrentVehicle()) Vehicle->SetHandbrake(true);
	else if (bInWater) bSwimUpHeld = true;
	else { if (bInCover) LeaveCover(); if (bStealth) ToggleStealth(); Jump(); }
}
void ASolPlayerCharacter::JumpOff()
{
	if (ASolVehicle* Vehicle = GetCurrentVehicle()) Vehicle->SetHandbrake(false);
	else if (bInWater) bSwimUpHeld = false;
	else StopJumping();
}
void ASolPlayerCharacter::CrouchOrDiveOn()
{
	if (GetCurrentVehicle()) return;
	if (bInWater) { bDiveHeld = true; return; }
	if (bInCover || bStealth) return;
	if (bIsCrouched) UnCrouch(); else Crouch();
}
void ASolPlayerCharacter::CrouchOrDiveOff() { bDiveHeld = false; }
void ASolPlayerCharacter::ToggleStealth()
{
	if (GetCurrentVehicle() || bInWater) return;
	bStealth = !bStealth;
	if (bStealth) { bSprinting = false; Crouch(); }
	else if (!bInCover) UnCrouch();
}
void ASolPlayerCharacter::ToggleCover()
{
	if (bInCover) { LeaveCover(); return; }
	if (bInWater || GetCurrentVehicle() || !GetWorld()) return;
	FVector Forward = Controller ? Controller->GetControlRotation().Vector() : GetActorForwardVector();
	Forward.Z = 0.f;
	Forward.Normalize();
	const FVector LowStart = GetActorLocation() - FVector(0.f, 0.f, 38.f);
	FCollisionQueryParams Query(SCENE_QUERY_STAT(SolLowCover), false, this);
	FHitResult LowHit, HighHit;
	if (!GetWorld()->LineTraceSingleByChannel(LowHit, LowStart, LowStart + Forward * 165.f, ECC_Visibility, Query)) return;
	const FVector HighStart = GetActorLocation() + FVector(0.f, 0.f, 78.f);
	if (GetWorld()->LineTraceSingleByChannel(HighHit, HighStart, HighStart + Forward * 165.f, ECC_Visibility, Query)) return;
	if (FMath::Abs(LowHit.ImpactNormal.Z) > 0.4f) return;
	CoverNormal = FVector(LowHit.ImpactNormal.X, LowHit.ImpactNormal.Y, 0.f).GetSafeNormal();
	if (CoverNormal.IsNearlyZero()) return;
	CoverAnchor = GetActorLocation();
	bInCover = true;
	Crouch();
}
void ASolPlayerCharacter::LeaveCover()
{
	bInCover = false;
	if (!bStealth && !bInWater) UnCrouch();
}
void ASolPlayerCharacter::AimOn() { bAiming = true; if (!GetCurrentVehicle()) { bUseControllerRotationYaw = true; GetCharacterMovement()->bOrientRotationToMovement = false; } }
void ASolPlayerCharacter::AimOff() { bAiming = false; bUseControllerRotationYaw = false; GetCharacterMovement()->bOrientRotationToMovement = true; }
void ASolPlayerCharacter::FireOn() { bFireHeld = true; TryFire(); }
void ASolPlayerCharacter::FireOff() { bFireHeld = false; }
void ASolPlayerCharacter::FinishFlash() { MuzzleFlash->SetIntensity(0.f); }

void ASolPlayerCharacter::TryFire()
{
	if (bDead || bReloading || Inventory.IsEmpty() || !GetWorld()) return;
	const ESolWeaponType Type = GetCurrentWeaponType();
	const FSolWeaponDefinition Def = SolGetWeaponDefinition(Type);
	if (GetCurrentVehicle() && !(Type == ESolWeaponType::Fists || Type == ESolWeaponType::Pistol || Type == ESolWeaponType::HeavyPistol || Type == ESolWeaponType::SMG)) return;
	const float Now = GetWorld()->GetTimeSeconds();
	if (Now - LastShotTime < Def.FireInterval) return;
	FSolWeaponState& State = Inventory[CurrentWeaponIndex];
	if (!Def.bMelee && State.MagazineAmmo <= 0) { Reload(); return; }
	LastShotTime = Now;
	if (!Def.bMelee) --State.MagazineAmmo;

	const FVector CameraStart = FollowCamera->GetComponentLocation();
	FVector Direction = FollowCamera->GetForwardVector();
	if (Def.bMelee && Controller) Direction = Controller->GetControlRotation().Vector();
	const FVector Start = Def.bMelee ? GetActorLocation() + FVector(0.f, 0.f, 45.f) : CameraStart;
	const float Spread = Def.SpreadDegrees * (bAiming ? 0.52f : 1.f) * (GetCurrentVehicle() ? 1.7f : 1.f) *
		(bInCover && !bAiming ? 1.6f : 1.f) * (1.f - ShootingSkill * 0.0035f);
	FCollisionQueryParams Query(SCENE_QUERY_STAT(SolWeaponTrace), true, this);
	if (GetCurrentVehicle()) Query.AddIgnoredActor(GetCurrentVehicle());
	FVector LastImpact = Start + Direction * Def.Range;
	for (int32 Pellet = 0; Pellet < Def.Pellets; ++Pellet)
	{
		const FVector ShotDirection = FMath::VRandCone(Direction, FMath::DegreesToRadians(Spread));
		FHitResult Hit;
		const FVector End = Start + ShotDirection * Def.Range;
		const bool bHit = Def.bMelee
			? GetWorld()->SweepSingleByChannel(Hit, Start, End, FQuat::Identity, ECC_Visibility, FCollisionShape::MakeSphere(55.f), Query)
			: GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Query);
		if (bHit)
		{
			LastImpact = Hit.ImpactPoint;
			if (AActor* Victim = Hit.GetActor())
			{
				float Damage = Def.Damage;
				if (Hit.BoneName == TEXT("head")) Damage *= 2.f;
				UGameplayStatics::ApplyPointDamage(Victim, Damage, ShotDirection, Hit, GetController(), this, UDamageType::StaticClass());
				if (!Def.bMelee) ShootingSkill = FMath::Min(100.f, ShootingSkill + 0.13f);
				if (Victim->IsA<ACharacter>() && Now - LastCrimeTime > 1.5f)
				{
					if (ASolWorldDirector* Director = ASolWorldDirector::Find(this)) Director->AddCrime(3.f, Hit.ImpactPoint);
					LastCrimeTime = Now;
				}
			}
		}
	}
	if (Type == ESolWeaponType::Grenade || Type == ESolWeaponType::Launcher)
	{
		TArray<AActor*> IgnoreActors;
		IgnoreActors.Add(this);
		UGameplayStatics::ApplyRadialDamage(this, Def.Damage, LastImpact, Type == ESolWeaponType::Launcher ? 550.f : 450.f,
			UDamageType::StaticClass(), IgnoreActors, this, GetController(), true);
		if (ASolWorldDirector* Director = ASolWorldDirector::Find(this)) Director->AddCrime(8.f, LastImpact);
	}
	else if (!Def.bMelee && Now - LastCrimeTime > 2.f)
	{
		if (ASolWorldDirector* Director = ASolWorldDirector::Find(this)) Director->AddCrime(1.f, GetActorLocation());
		LastCrimeTime = Now;
	}
	if (!Def.bMelee)
	{
		ShootingSkill = FMath::Min(100.f, ShootingSkill + 0.025f);
		if (GunshotAudio) UGameplayStatics::PlaySoundAtLocation(this,GunshotAudio,GetActorLocation(),0.55f);
		MuzzleFlash->SetIntensity(12000.f);
		GetWorldTimerManager().SetTimer(FlashTimer, this, &ASolPlayerCharacter::FinishFlash, 0.055f, false);
	}
}

void ASolPlayerCharacter::GiveWeapon(ESolWeaponType Type, int32 ReserveAmmo)
{
	for (FSolWeaponState& State : Inventory)
	{
		if (State.Type == Type)
		{
			State.ReserveAmmo += FMath::Max(0, ReserveAmmo);
			return;
		}
	}
	FSolWeaponState State;
	State.Type = Type;
	State.MagazineAmmo = SolGetWeaponDefinition(Type).MagazineSize;
	State.ReserveAmmo = FMath::Max(0, ReserveAmmo);
	Inventory.Add(State);
}

void ASolPlayerCharacter::SelectWeaponSlot(int32 Index)
{
	if (!Inventory.IsValidIndex(Index)) return;
	bReloading = false;
	GetWorldTimerManager().ClearTimer(ReloadTimer);
	CurrentWeaponIndex = Index;
	UpdateWeaponVisual();
}

void ASolPlayerCharacter::NextWeapon() { if (!Inventory.IsEmpty()) SelectWeaponSlot((CurrentWeaponIndex + 1) % Inventory.Num()); }
void ASolPlayerCharacter::PreviousWeapon() { if (!Inventory.IsEmpty()) SelectWeaponSlot((CurrentWeaponIndex + Inventory.Num() - 1) % Inventory.Num()); }
void ASolPlayerCharacter::Weapon1() { SelectWeaponSlot(0); }
void ASolPlayerCharacter::Weapon2() { SelectWeaponSlot(1); }
void ASolPlayerCharacter::Weapon3() { SelectWeaponSlot(2); }
void ASolPlayerCharacter::Weapon4() { SelectWeaponSlot(3); }
void ASolPlayerCharacter::Weapon5() { SelectWeaponSlot(4); }
void ASolPlayerCharacter::Weapon6() { SelectWeaponSlot(5); }
void ASolPlayerCharacter::Weapon7() { SelectWeaponSlot(6); }
void ASolPlayerCharacter::Weapon8() { SelectWeaponSlot(7); }
void ASolPlayerCharacter::Weapon9() { SelectWeaponSlot(8); }

void ASolPlayerCharacter::Reload()
{
	if (bDead || bReloading || !Inventory.IsValidIndex(CurrentWeaponIndex)) return;
	const int32 MagSize = SolGetWeaponDefinition(GetCurrentWeaponType()).MagazineSize;
	FSolWeaponState& State = Inventory[CurrentWeaponIndex];
	if (MagSize <= 0 || State.MagazineAmmo >= MagSize || State.ReserveAmmo <= 0) return;
	bReloading = true;
	GetWorldTimerManager().SetTimer(ReloadTimer, this, &ASolPlayerCharacter::FinishReload, GetCurrentWeaponType() == ESolWeaponType::Shotgun ? 2.2f : 1.35f, false);
}

void ASolPlayerCharacter::FinishReload()
{
	bReloading = false;
	if (!Inventory.IsValidIndex(CurrentWeaponIndex)) return;
	FSolWeaponState& State = Inventory[CurrentWeaponIndex];
	const int32 Needed = FMath::Max(0, SolGetWeaponDefinition(State.Type).MagazineSize - State.MagazineAmmo);
	const int32 Transfer = FMath::Min(Needed, State.ReserveAmmo);
	State.MagazineAmmo += Transfer;
	State.ReserveAmmo -= Transfer;
}

ESolWeaponType ASolPlayerCharacter::GetCurrentWeaponType() const
{
	return Inventory.IsValidIndex(CurrentWeaponIndex) ? Inventory[CurrentWeaponIndex].Type : ESolWeaponType::Fists;
}

int32 ASolPlayerCharacter::GetCurrentAmmo() const
{
	return Inventory.IsValidIndex(CurrentWeaponIndex) ? Inventory[CurrentWeaponIndex].MagazineAmmo : 0;
}

int32 ASolPlayerCharacter::GetReserveAmmo() const
{
	return Inventory.IsValidIndex(CurrentWeaponIndex) ? Inventory[CurrentWeaponIndex].ReserveAmmo : 0;
}

FSolWeaponDefinition ASolPlayerCharacter::GetCurrentWeaponDefinition() const { return SolGetWeaponDefinition(GetCurrentWeaponType()); }

void ASolPlayerCharacter::UpdateWeaponVisual()
{
	const ESolWeaponType Type = GetCurrentWeaponType();
	if (Type == ESolWeaponType::Fists)
	{
		WeaponVisual->SetVisibility(false);
		return;
	}
	const TCHAR* AssetPath = (Type == ESolWeaponType::Rifle || Type == ESolWeaponType::Sniper || Type == ESolWeaponType::Shotgun)
		? TEXT("/Game/GTA/Generated/SM_Rifle.SM_Rifle") : TEXT("/Game/GTA/Generated/SM_Pistol.SM_Pistol");
	UStaticMesh* VisualMesh = LoadObject<UStaticMesh>(nullptr, AssetPath);
	if (!VisualMesh) VisualMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
	WeaponVisual->SetStaticMesh(VisualMesh);
	WeaponVisual->SetRelativeScale3D(VisualMesh && VisualMesh->GetName() == TEXT("Cube") ? FVector(0.45f, 0.12f, 0.1f) : FVector::OneVector);
	WeaponVisual->SetVisibility(!GetCurrentVehicle() || GetCurrentVehicle()->IsMotorcycle());
}

ASolVehicle* ASolPlayerCharacter::GetCurrentVehicle() const { return IsValid(CurrentVehicle) ? CurrentVehicle : nullptr; }

bool ASolPlayerCharacter::EnterVehicle(ASolVehicle* Vehicle)
{
	return IsValid(Vehicle) && Vehicle->Enter(this);
}

void ASolPlayerCharacter::ExitVehicle()
{
	if (ASolVehicle* Vehicle = GetCurrentVehicle()) Vehicle->Exit();
}

void ASolPlayerCharacter::OnEnteredVehicle(ASolVehicle* Vehicle)
{
	if (bInCover) LeaveCover();
	bStealth = false;
	bInWater = false;
	bDiveHeld = false;
	bSwimUpHeld = false;
	CurrentVehicle = Vehicle;
	UnCrouch();
	AimOff();
	GetCharacterMovement()->DisableMovement();
	GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	AttachToComponent(Vehicle->DriverSeat, FAttachmentTransformRules::SnapToTargetNotIncludingScale);
	CameraBoom->bDoCollisionTest = false;
	BodyVisual->SetRelativeRotation(Vehicle->IsMotorcycle() ? FRotator(16.f, 0.f, 0.f) : FRotator::ZeroRotator);
	BodyVisual->SetVisibility(Vehicle->IsMotorcycle() && !bFirstPerson);
	WeaponVisual->SetVisibility(Vehicle->IsMotorcycle() && GetCurrentWeaponType() != ESolWeaponType::Fists);
	if (Controller) Controller->SetControlRotation(FRotator(-6.f, Vehicle->GetActorRotation().Yaw, 0.f));
}

void ASolPlayerCharacter::OnExitedVehicle(ASolVehicle* Vehicle, FVector ExitLocation)
{
	if (CurrentVehicle != Vehicle) return;
	DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
	CurrentVehicle = nullptr;
	SetActorLocation(ExitLocation, false);
	GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
	GetCharacterMovement()->SetMovementMode(MOVE_Walking);
	CameraBoom->bDoCollisionTest = !bFirstPerson;
	BodyVisual->SetRelativeRotation(FRotator::ZeroRotator);
	BodyVisual->SetVisibility(!bFirstPerson);
	UpdateWeaponVisual();
}

void ASolPlayerCharacter::TryEnterExitVehicle()
{
	if (GetCurrentVehicle()) { ExitVehicle(); return; }
	ASolVehicle* Nearest = nullptr;
	float BestDistSq = FMath::Square(420.f);
	for (TActorIterator<ASolVehicle> It(GetWorld()); It; ++It)
	{
		ASolVehicle* Vehicle = *It;
		if (Vehicle->IsDestroyed() || Vehicle->GetDriver()) continue;
		const float DistSq = FVector::DistSquared(GetActorLocation(), Vehicle->GetActorLocation());
		if (DistSq < BestDistSq) { BestDistSq = DistSq; Nearest = Vehicle; }
	}
	if (Nearest) EnterVehicle(Nearest);
}

void ASolPlayerCharacter::Interact()
{
	if (GetCurrentVehicle()) return;
	FHitResult Hit;
	FCollisionQueryParams Query(SCENE_QUERY_STAT(SolInteract), false, this);
	const FVector Start = FollowCamera->GetComponentLocation();
	if (GetWorld()->LineTraceSingleByChannel(Hit, Start, Start + FollowCamera->GetForwardVector() * 550.f, ECC_Visibility, Query))
	{
		if (ASolVehicle* Vehicle = Cast<ASolVehicle>(Hit.GetActor())) { EnterVehicle(Vehicle); return; }
		if (AActor* Target = Hit.GetActor())
		{
			if (UFunction* Function = Target->FindFunction(FName(TEXT("Interact"))))
			{
				if (Function->ParmsSize == 0) Target->ProcessEvent(Function, nullptr);
			}
		}
	}
}

void ASolPlayerCharacter::ToggleAdmin()
{
	if (ASolWorldDirector* Director = ASolWorldDirector::Find(this)) Director->ToggleAdmin();
}
void ASolPlayerCharacter::ToggleCamera()
{
	bFirstPerson = !bFirstPerson;
	CameraBoom->bDoCollisionTest = !bFirstPerson && !GetCurrentVehicle();
	BodyVisual->SetVisibility(!bFirstPerson && (!GetCurrentVehicle() || GetCurrentVehicle()->IsMotorcycle()));
}
void ASolPlayerCharacter::VehicleHorn() { if (ASolVehicle* Vehicle = GetCurrentVehicle()) Vehicle->Horn(); }
void ASolPlayerCharacter::VehicleLights() { if (ASolVehicle* Vehicle = GetCurrentVehicle()) Vehicle->ToggleLights(); }
void ASolPlayerCharacter::Heal(float Amount) { Health = FMath::Clamp(Health + FMath::Max(Amount, 0.f), 0.f, MaxHealth); }
void ASolPlayerCharacter::SetGodMode(bool bEnabled) { bGodMode = bEnabled; }
void ASolPlayerCharacter::AddCash(int32 Amount) { Cash = FMath::Max(0, Cash + Amount); }

float ASolPlayerCharacter::TakeDamage(float DamageAmount, FDamageEvent const& DamageEvent, AController* EventInstigator, AActor* DamageCauser)
{
	if (bGodMode || bDead || DamageAmount <= 0.f) return 0.f;
	const float Absorbed = FMath::Min(Armor, DamageAmount);
	Armor -= Absorbed;
	Health = FMath::Max(0.f, Health - (DamageAmount - Absorbed));
	if (Health <= 0.f)
	{
		bDead = true;
		if (GetCurrentVehicle()) ExitVehicle();
		GetCharacterMovement()->DisableMovement();
		if (ASolWorldDirector* Director = ASolWorldDirector::Find(this)) Director->ClearWanted();
		GetWorldTimerManager().SetTimer(RespawnTimer, this, &ASolPlayerCharacter::RespawnAtHospital, 2.8f, false);
	}
	return DamageAmount;
}

void ASolPlayerCharacter::RespawnAtHospital()
{
	SetActorLocation(FVector(-22000.f, 20000.f, 200.f), false);
	Health = MaxHealth;
	Armor = 25.f;
	Stamina = 100.f + StaminaSkill * 0.5f;
	Breath = 100.f + LungSkill * 0.5f;
	bInWater = false;
	bStealth = false;
	LeaveCover();
	bDead = false;
	GetCharacterMovement()->SetMovementMode(MOVE_Walking);
}
