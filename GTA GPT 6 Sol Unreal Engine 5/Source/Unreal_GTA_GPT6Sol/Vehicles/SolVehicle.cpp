#include "Vehicles/SolVehicle.h"

#include "Player/SolPlayerCharacter.h"
#include "World/SolWorldDirector.h"
#include "Components/AudioComponent.h"
#include "Components/BoxComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/SceneComponent.h"
#include "Components/SpotLightComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "Engine/DamageEvents.h"
#include "GameFramework/DamageType.h"
#include "Kismet/GameplayStatics.h"
#include "Particles/ParticleSystem.h"
#include "Sound/SoundBase.h"
#include "Materials/MaterialInterface.h"
#include "TimerManager.h"

ASolVehicle::ASolVehicle()
{
	PrimaryActorTick.bCanEverTick = true;
	SetCanBeDamaged(true);

	Collision = CreateDefaultSubobject<UBoxComponent>(TEXT("VehicleCollision"));
	SetRootComponent(Collision);
	Collision->SetBoxExtent(FVector(210.f, 93.f, 72.f));
	Collision->SetCollisionProfileName(TEXT("BlockAllDynamic"));

	BodyMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("BodyMesh"));
	BodyMesh->SetupAttachment(Collision);
	BodyMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	DriverSeat = CreateDefaultSubobject<USceneComponent>(TEXT("DriverSeat"));
	DriverSeat->SetupAttachment(Collision);
	DriverSeat->SetRelativeLocation(FVector(30.f, -42.f, 35.f));
	ExitPoint = CreateDefaultSubobject<USceneComponent>(TEXT("ExitPoint"));
	ExitPoint->SetupAttachment(Collision);
	ExitPoint->SetRelativeLocation(FVector(0.f, -190.f, 25.f));

	LeftHeadlight = CreateDefaultSubobject<USpotLightComponent>(TEXT("LeftHeadlight"));
	LeftHeadlight->SetupAttachment(Collision);
	LeftHeadlight->SetRelativeLocation(FVector(205.f, -65.f, 0.f));
	LeftHeadlight->SetIntensity(0.f);
	LeftHeadlight->SetAttenuationRadius(1800.f);
	LeftHeadlight->SetOuterConeAngle(42.f);
	RightHeadlight = CreateDefaultSubobject<USpotLightComponent>(TEXT("RightHeadlight"));
	RightHeadlight->SetupAttachment(Collision);
	RightHeadlight->SetRelativeLocation(FVector(205.f, 65.f, 0.f));
	RightHeadlight->SetIntensity(0.f);
	RightHeadlight->SetAttenuationRadius(1800.f);
	RightHeadlight->SetOuterConeAngle(42.f);
	BrakeLight = CreateDefaultSubobject<UPointLightComponent>(TEXT("BrakeLight"));
	BrakeLight->SetupAttachment(Collision);
	BrakeLight->SetRelativeLocation(FVector(-205.f, 0.f, 5.f));
	BrakeLight->SetLightColor(FLinearColor::Red);
	BrakeLight->SetAttenuationRadius(280.f);
	BrakeLight->SetIntensity(0.f);
	FireLight = CreateDefaultSubobject<UPointLightComponent>(TEXT("FireLight"));
	FireLight->SetupAttachment(Collision);
	FireLight->SetRelativeLocation(FVector(80.f, 0.f, 50.f));
	FireLight->SetLightColor(FLinearColor(1.f, 0.15f, 0.01f));
	FireLight->SetAttenuationRadius(550.f);
	FireLight->SetIntensity(0.f);

	EngineAudio = CreateDefaultSubobject<UAudioComponent>(TEXT("EngineAudio"));
	EngineAudio->SetupAttachment(Collision);
	EngineAudio->bAutoActivate = false;
}

void ASolVehicle::BeginPlay()
{
	Super::BeginPlay();
	InitializeVehicle();
	VehicleHealth = FMath::Clamp(VehicleHealth, 0.f, MaxVehicleHealth);
	if (!EngineSound) EngineSound=LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_EngineLoop.SFX_EngineLoop"));
	if (!HornSound) HornSound=LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_Horn.SFX_Horn"));
	if (!SirenSound) SirenSound=LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_SirenLoop.SFX_SirenLoop"));
	if (EngineSound)
	{
		EngineAudio->SetSound(EngineSound);
		EngineAudio->SetVolumeMultiplier(0.18f);
		EngineAudio->Play();
	}
}

void ASolVehicle::InitializeVehicle()
{
	FString AssetName = TEXT("SM_Car");
	FVector Extent(210.f, 93.f, 72.f);
	FVector Seat(30.f, -42.f, 35.f);
	FVector DesiredSize(470.f, 220.f, 170.f);
	float TargetMaxHealth = 300.f;
	switch (VehicleType)
	{
	case ESolVehicleType::Boat:
		AssetName = TEXT("SM_Boat"); Extent = FVector(250.f, 105.f, 75.f); Seat = FVector(-15.f, 0.f, 35.f); DesiredSize = FVector(610.f, 220.f, 180.f); TargetMaxHealth = 260.f; break;
	case ESolVehicleType::Helicopter:
		AssetName = TEXT("SM_Helicopter"); Extent = FVector(310.f, 180.f, 115.f); Seat = FVector(85.f, -48.f, 0.f); DesiredSize = FVector(800.f, 700.f, 275.f); TargetMaxHealth = 400.f; break;
	case ESolVehicleType::Plane:
		AssetName = TEXT("SM_Plane"); Extent = FVector(350.f, 460.f, 130.f); Seat = FVector(130.f, -45.f, 15.f); DesiredSize = FVector(700.f, 890.f, 250.f); TargetMaxHealth = 350.f; break;
	default:
		switch (CarVariant)
		{
		case ESolCarVariant::Sedan: AssetName = TEXT("SM_Sedan"); Extent = FVector(235.f, 98.f, 75.f); Seat = FVector(40.f, -44.f, 35.f); DesiredSize = FVector(500.f, 205.f, 165.f); TargetMaxHealth = 350.f; break;
		case ESolCarVariant::PoliceCar: AssetName = TEXT("SM_PoliceCar"); Extent = FVector(238.f, 100.f, 76.f); Seat = FVector(40.f, -44.f, 35.f); DesiredSize = FVector(500.f, 215.f, 175.f); TargetMaxHealth = 425.f; bPoliceVehicle = true; break;
		case ESolCarVariant::SUV: AssetName = TEXT("SM_SUV"); Extent = FVector(240.f, 110.f, 103.f); Seat = FVector(38.f, -49.f, 48.f); DesiredSize = FVector(490.f, 230.f, 210.f); TargetMaxHealth = 470.f; break;
		case ESolCarVariant::Pickup: AssetName = TEXT("SM_Pickup"); Extent = FVector(255.f, 105.f, 90.f); Seat = FVector(45.f, -47.f, 43.f); DesiredSize = FVector(530.f, 230.f, 195.f); TargetMaxHealth = 420.f; break;
		case ESolCarVariant::Van: AssetName = TEXT("SM_Van"); Extent = FVector(265.f, 115.f, 116.f); Seat = FVector(45.f, -48.f, 51.f); DesiredSize = FVector(530.f, 235.f, 240.f); TargetMaxHealth = 440.f; break;
		case ESolCarVariant::SportsCar: AssetName = TEXT("SM_SportsCar"); Extent = FVector(225.f, 103.f, 60.f); Seat = FVector(35.f, -43.f, 27.f); DesiredSize = FVector(455.f, 215.f, 135.f); TargetMaxHealth = 280.f; break;
		case ESolCarVariant::Motorcycle: AssetName = TEXT("SM_Motorcycle"); Extent = FVector(125.f, 44.f, 72.f); Seat = FVector(-5.f, 0.f, 52.f); DesiredSize = FVector(270.f, 90.f, 150.f); TargetMaxHealth = 160.f; break;
		default: break;
		}
		break;
	}
	const float HealthFraction = MaxVehicleHealth > 0.f ? VehicleHealth / MaxVehicleHealth : 1.f;
	MaxVehicleHealth = TargetMaxHealth;
	VehicleHealth = FMath::Clamp(HealthFraction, 0.f, 1.f) * MaxVehicleHealth;
	Collision->SetBoxExtent(Extent);
	if (VehicleType == ESolVehicleType::Plane && HasActorBegunPlay() && GetWorld())
	{
		FHitResult GroundHit;
		FCollisionQueryParams Query(SCENE_QUERY_STAT(SolPlaneInitialClearance), false, this);
		const FVector Start = GetActorLocation() + FVector(0.f, 0.f, 400.f);
		if (GetWorld()->LineTraceSingleByChannel(GroundHit, Start, Start - FVector(0.f, 0.f, 1200.f), ECC_Visibility, Query))
		{
			const float MinCenterZ = GroundHit.Location.Z + Extent.Z + 15.f;
			if (GetActorLocation().Z < MinCenterZ)
			{
				SetActorLocation(FVector(GetActorLocation().X, GetActorLocation().Y, MinCenterZ), false);
			}
		}
	}
	DriverSeat->SetRelativeLocation(Seat);
	ExitPoint->SetRelativeLocation(FVector(0.f, -(Extent.Y + 105.f), 30.f));
	LeftHeadlight->SetRelativeLocation(FVector(Extent.X - 5.f, -Extent.Y * 0.72f, -Extent.Z * 0.25f));
	RightHeadlight->SetRelativeLocation(FVector(Extent.X - 5.f, Extent.Y * 0.72f, -Extent.Z * 0.25f));
	BrakeLight->SetRelativeLocation(FVector(-Extent.X, 0.f, -Extent.Z * 0.2f));

	const FString AssetPath = FString::Printf(TEXT("/Game/GTA/Generated/%s.%s"), *AssetName, *AssetName);
	UStaticMesh* Mesh = LoadObject<UStaticMesh>(nullptr, *AssetPath);
	const bool bUsingFallback = Mesh == nullptr;
	if (bUsingFallback)
	{
		Mesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
	}
	BodyMesh->SetStaticMesh(Mesh);
	if (bUsingFallback)
	{
		BodyMesh->SetRelativeLocation(FVector::ZeroVector);
		BodyMesh->SetRelativeScale3D(Extent * 0.02f);
	}
	else
	{
		const FBoxSphereBounds Bounds = Mesh->GetBounds();
		const FVector FullSize = Bounds.BoxExtent * 2.f;
		const float RawMajor = FMath::Max3(FullSize.X, FullSize.Y, FullSize.Z);
		const float TargetMajor = FMath::Max3(DesiredSize.X, DesiredSize.Y, DesiredSize.Z);
		const float UniformScale = FMath::Clamp(TargetMajor / FMath::Max(1.f, RawMajor), 0.01f, 100.f);
		BodyMesh->SetRelativeScale3D(FVector(UniformScale));
		BodyMesh->SetRelativeLocation(-Bounds.Origin * UniformScale);
	}
	ApplyPaintPreset(PaintPresetIndex);
}

void ASolVehicle::ApplyPaintPreset(int32 PresetIndex)
{
	PaintPresetIndex=FMath::Clamp(PresetIndex,0,5);
	if (VehicleType!=ESolVehicleType::Car || CarVariant==ESolCarVariant::PoliceCar || !BodyMesh || !BodyMesh->GetStaticMesh()) return;
	const TCHAR* Paths[] = {
		nullptr,
		TEXT("/Game/GTA/Materials/M_Teal.M_Teal"),
		TEXT("/Game/GTA/Materials/M_Coral.M_Coral"),
		TEXT("/Game/GTA/Materials/M_Cream.M_Cream"),
		TEXT("/Game/GTA/Materials/M_Graphite.M_Graphite"),
		TEXT("/Game/GTA/Materials/M_Sand.M_Sand")
	};
	UMaterialInterface* Paint=PaintPresetIndex==0 ? BodyMesh->GetStaticMesh()->GetMaterial(0)
		: LoadObject<UMaterialInterface>(nullptr,Paths[PaintPresetIndex]);
	if (Paint) BodyMesh->SetMaterial(0,Paint);
}

void ASolVehicle::UpgradeEngine()
{
	if (VehicleType==ESolVehicleType::Car) EngineUpgrade=FMath::Clamp(EngineUpgrade+1,0,3);
}

bool ASolVehicle::Enter(ASolPlayerCharacter* Character)
{
	if (!IsValid(Character) || IsValid(Driver.Get()) || bDestroyed || Character->GetCurrentVehicle())
	{
		return false;
	}
	const bool bWasOccupied = bNPCDriver || bAutopilot;
	bAutopilot = false;
	bNPCDriver = false;
	Driver = Character;
	Character->OnEnteredVehicle(this);
	if (ASolWorldDirector* Director = ASolWorldDirector::Find(this))
	{
		if (bWasOccupied || bPoliceVehicle)
		{
			Director->AddCrime(bPoliceVehicle ? 4.f : 2.f, GetActorLocation());
		}
	}
	return true;
}

void ASolVehicle::Exit()
{
	ASolPlayerCharacter* OldDriver = Driver.Get();
	if (!IsValid(OldDriver))
	{
		Driver.Reset();
		return;
	}
	Driver.Reset();
	SetDrivingInput(0.f, 0.f, 0.5f);
	OldDriver->OnExitedVehicle(this, ExitPoint->GetComponentLocation() + FVector(0.f, 0.f, 70.f));
}

void ASolVehicle::SetDrivingInput(float InThrottle, float InSteering, float InBrake)
{
	Throttle = FMath::Clamp(InThrottle, -1.f, 1.f);
	Steering = FMath::Clamp(InSteering, -1.f, 1.f);
	Brake = FMath::Clamp(InBrake, 0.f, 1.f);
}

void ASolVehicle::SetHandbrake(bool bEnabled)
{
	bHandbrake = bEnabled;
}

void ASolVehicle::Repair()
{
	bDestroyed = false;
	VehicleHealth = MaxVehicleHealth;
	FireLight->SetIntensity(0.f);
	Collision->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
	if (EngineSound && !EngineAudio->IsPlaying())
	{
		EngineAudio->Play();
	}
}

void ASolVehicle::ToggleLights()
{
	bLightsOn = !bLightsOn;
	LeftHeadlight->SetIntensity(bLightsOn ? 26000.f : 0.f);
	RightHeadlight->SetIntensity(bLightsOn ? 26000.f : 0.f);
}

void ASolVehicle::Horn()
{
	if (HornSound && GetWorld())
	{
		UGameplayStatics::PlaySoundAtLocation(this, HornSound, GetActorLocation());
	}
}

void ASolVehicle::SetSiren(bool bEnabled)
{
	bSirenOn = bEnabled && bPoliceVehicle;
	if (bSirenOn && SirenSound)
	{
		UGameplayStatics::PlaySoundAtLocation(this, SirenSound, GetActorLocation());
	}
}

void ASolVehicle::SetAutopilot(const TArray<FVector>& InRoute, bool bLoop)
{
	Route = InRoute;
	RouteIndex = 0;
	bLoopRoute = bLoop;
	bAutopilot = Route.Num() > 0;
	bNPCDriver = bAutopilot;
}

void ASolVehicle::SetAutopilotTarget(FVector Target)
{
	Route.Reset();
	Route.Add(Target);
	RouteIndex = 0;
	bLoopRoute = false;
	bAutopilot = true;
	bNPCDriver = true;
}

void ASolVehicle::UpdateAutopilot()
{
	if (!bAutopilot || Route.IsEmpty() || IsValid(Driver.Get()) || bDestroyed)
	{
		return;
	}
	FVector ToTarget = Route[RouteIndex] - GetActorLocation();
	ToTarget.Z = 0.f;
	if (ToTarget.SizeSquared() < FMath::Square(340.f))
	{
		if (RouteIndex + 1 < Route.Num())
		{
			++RouteIndex;
		}
		else if (bLoopRoute)
		{
			RouteIndex = 0;
		}
		else
		{
			bAutopilot = false;
			bNPCDriver = false;
			SetDrivingInput(0.f, 0.f, 0.8f);
			return;
		}
		ToTarget = Route[RouteIndex] - GetActorLocation();
	}
	const float DesiredYaw = ToTarget.Rotation().Yaw;
	const float YawDelta = FMath::FindDeltaAngleDegrees(GetActorRotation().Yaw, DesiredYaw);
	const float Steer = FMath::Clamp(YawDelta / 42.f, -1.f, 1.f);
	float CruiseThrottle = FMath::Abs(YawDelta) > 75.f ? 0.28f : 0.72f;
	float CornerBrake = 0.f;
	const int32 NextIndex = RouteIndex + 1 < Route.Num() ? RouteIndex + 1 : bLoopRoute ? 0 : RouteIndex;
	if (NextIndex != RouteIndex && ToTarget.SizeSquared() < FMath::Square(2200.f))
	{
		const FVector NextLeg = Route[NextIndex] - Route[RouteIndex];
		const float Bend = FMath::Abs(FMath::FindDeltaAngleDegrees(DesiredYaw, NextLeg.Rotation().Yaw));
		if (Bend > 45.f)
		{
			CruiseThrottle = FMath::Min(CruiseThrottle, 0.23f);
			CornerBrake = FMath::Abs(ForwardSpeed) > 950.f ? 0.6f : 0.f;
		}
	}

	FHitResult Obstacle;
	FCollisionQueryParams Query(SCENE_QUERY_STAT(SolTrafficObstacle), false, this);
	if (Driver.IsValid()) Query.AddIgnoredActor(Driver.Get());
	const FVector ProbeStart = GetActorLocation() + FVector(0.f, 0.f, 25.f);
	const FVector ProbeEnd = ProbeStart + GetActorForwardVector() * 850.f;
	const bool bBlocked = GetWorld()->SweepSingleByChannel(Obstacle, ProbeStart, ProbeEnd, FQuat::Identity,
		ECC_Visibility, FCollisionShape::MakeSphere(95.f), Query) && Obstacle.GetActor() != nullptr;
	SetDrivingInput(bBlocked ? 0.f : CruiseThrottle, Steer, bBlocked ? 1.f : CornerBrake);
}

void ASolVehicle::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (bAutopilot) UpdateAutopilot();
	UpdateMovement(DeltaSeconds);
	UpdateVisuals(DeltaSeconds);
}

void ASolVehicle::UpdateMovement(float DeltaSeconds)
{
	if (bDestroyed || !GetWorld()) return;
	const bool bAir = VehicleType == ESolVehicleType::Helicopter || VehicleType == ESolVehicleType::Plane;
	float MaxForward = VehicleType == ESolVehicleType::Plane ? 5400.f : VehicleType == ESolVehicleType::Helicopter ? 3000.f : VehicleType == ESolVehicleType::Boat ? 2600.f : 3900.f;
	float Acceleration = bAir ? 950.f : VehicleType == ESolVehicleType::Boat ? 800.f : 1350.f;
	if (VehicleType == ESolVehicleType::Car)
	{
		switch (CarVariant)
		{
		case ESolCarVariant::SportsCar: MaxForward = 5100.f; Acceleration = 1900.f; break;
		case ESolCarVariant::Motorcycle: MaxForward = 4500.f; Acceleration = 1700.f; break;
		case ESolCarVariant::PoliceCar: MaxForward = 4400.f; Acceleration = 1550.f; break;
		case ESolCarVariant::Van: MaxForward = 3100.f; Acceleration = 950.f; break;
		case ESolCarVariant::SUV: MaxForward = 3500.f; Acceleration = 1100.f; break;
		case ESolCarVariant::Pickup: MaxForward = 3450.f; Acceleration = 1150.f; break;
		default: break;
		}
		MaxForward*=1.f+0.08f*EngineUpgrade;
		Acceleration*=1.f+0.15f*EngineUpgrade;
	}
	const float TargetSpeed = Throttle >= 0.f ? Throttle * MaxForward : Throttle * (VehicleType == ESolVehicleType::Car ? 1350.f : 750.f);
	ForwardSpeed = FMath::FInterpConstantTo(ForwardSpeed, TargetSpeed, DeltaSeconds, Acceleration);
	if (FMath::IsNearlyZero(Throttle)) ForwardSpeed = FMath::FInterpTo(ForwardSpeed, 0.f, DeltaSeconds, bAir ? 0.35f : 0.9f);
	if (Brake > 0.f || bHandbrake) ForwardSpeed = FMath::FInterpConstantTo(ForwardSpeed, 0.f, DeltaSeconds, (bHandbrake ? 3200.f : 4600.f) * FMath::Max(Brake, bHandbrake ? 1.f : 0.f));

	const float SpeedFactor = FMath::Clamp(FMath::Abs(ForwardSpeed) / 400.f, 0.f, 1.f);
	float TurnRate = VehicleType == ESolVehicleType::Boat ? 34.f : VehicleType == ESolVehicleType::Helicopter ? 65.f : VehicleType == ESolVehicleType::Plane ? 27.f : 67.f;
	if (VehicleType == ESolVehicleType::Car)
	{
		if (CarVariant == ESolCarVariant::Motorcycle) TurnRate = 88.f;
		else if (CarVariant == ESolCarVariant::SportsCar) TurnRate = 75.f;
		else if (CarVariant == ESolCarVariant::Van || CarVariant == ESolCarVariant::SUV) TurnRate = 54.f;
	}
	const float ReverseSign = ForwardSpeed < -30.f ? -1.f : 1.f;
	FHitResult GroundHit;
	FCollisionQueryParams GroundQuery(SCENE_QUERY_STAT(SolVehicleGround), false, this);
	const FVector GroundStart = GetActorLocation() + FVector(0.f, 0.f, 180.f);
	const bool bHasGround = GetWorld()->LineTraceSingleByChannel(GroundHit, GroundStart,
		GroundStart - FVector(0.f, 0.f, 1000.f), ECC_Visibility, GroundQuery);
	const float PlaneGroundCenterZ = bHasGround ? GroundHit.Location.Z + Collision->GetScaledBoxExtent().Z + 15.f : 0.f;
	if (VehicleType == ESolVehicleType::Plane && bHasGround && GetActorLocation().Z < PlaneGroundCenterZ)
	{
		SetActorLocation(FVector(GetActorLocation().X, GetActorLocation().Y, PlaneGroundCenterZ), false);
	}
	const float PlaneAltitude = bHasGround ? GetActorLocation().Z - PlaneGroundCenterZ : 1000.f;
	if (VehicleType == ESolVehicleType::Plane && !bPlaneAirborne &&
		(!bHasGround || PlaneAltitude > 140.f || (Throttle > 0.65f && ForwardSpeed > 2200.f)))
	{
		bPlaneAirborne = true;
		if (bHasGround && PlaneAltitude < 140.f) VerticalSpeed = FMath::Max(VerticalSpeed, 210.f);
	}
	FRotator Rotation = GetActorRotation();
	const float SkillControl = Driver.IsValid()
		? 1.f + (bAir ? Driver->FlyingSkill : Driver->DrivingSkill) * 0.001f : 1.f;
	Rotation.Yaw += Steering * TurnRate * SkillControl * SpeedFactor * ReverseSign * DeltaSeconds;
	if (VehicleType == ESolVehicleType::Plane)
	{
		const bool bNearRunway = PlaneAltitude < 140.f;
		const float PitchTarget = bNearRunway ? 0.f : FMath::Clamp(VerticalSpeed * 0.015f, -4.f, 6.f);
		Rotation.Pitch = FMath::FInterpTo(Rotation.Pitch, PitchTarget, DeltaSeconds, bNearRunway ? 3.5f : 1.1f);
		Rotation.Roll = FMath::FInterpTo(Rotation.Roll, bNearRunway ? 0.f : -Steering * 9.f, DeltaSeconds, bNearRunway ? 3.5f : 1.2f);
	}
	else if (bAir)
	{
		const float PitchTarget = FMath::Clamp(Throttle * -4.f, -6.f, 2.f);
		Rotation.Pitch = FMath::FInterpTo(Rotation.Pitch, PitchTarget, DeltaSeconds, 1.1f);
		Rotation.Roll = FMath::FInterpTo(Rotation.Roll, -Steering * 12.f, DeltaSeconds, 1.2f);
	}
	SetActorRotation(Rotation);
	FVector Delta = GetActorForwardVector() * ForwardSpeed * DeltaSeconds;
	if (VehicleType == ESolVehicleType::Helicopter)
	{
		const float Lift = bHandbrake ? -350.f : (Throttle > 0.08f ? 280.f : -90.f);
		VerticalSpeed = FMath::Clamp(VerticalSpeed + Lift * DeltaSeconds, -500.f, 520.f);
		if (bHasGround && GetActorLocation().Z < GroundHit.Location.Z + 150.f && VerticalSpeed < 0.f) VerticalSpeed = 0.f;
		Delta.Z = VerticalSpeed * DeltaSeconds;
	}
	else if (VehicleType == ESolVehicleType::Plane)
	{
		Delta.Z = 0.f;
		if (bPlaneAirborne)
		{
			const float Lift = (ForwardSpeed - 1800.f) * 0.23f - 115.f;
			VerticalSpeed = FMath::Clamp(VerticalSpeed + Lift * DeltaSeconds, -470.f, 430.f);
			Delta.Z = VerticalSpeed * DeltaSeconds;
			if (bHasGround && Delta.Z < -PlaneAltitude)
			{
				Delta.Z = -FMath::Max(0.f, PlaneAltitude);
				VerticalSpeed = 0.f;
				bPlaneAirborne = false;
			}
		}
		else
		{
			VerticalSpeed = 0.f;
			if (bHasGround) Delta.Z = PlaneGroundCenterZ - GetActorLocation().Z;
		}
	}
	else if (VehicleType == ESolVehicleType::Boat && GetActorLocation().Y < -19000.f)
	{
		const float WaterlineZ = -30.f + Collision->GetScaledBoxExtent().Z * 0.62f;
		Delta.Z = FMath::Clamp((WaterlineZ - GetActorLocation().Z) * 3.2f * DeltaSeconds,
			-260.f * DeltaSeconds, 260.f * DeltaSeconds);
		VerticalSpeed = 0.f;
	}
	else if (bHasGround)
	{
		const float RideHeight = Collision->GetScaledBoxExtent().Z + (VehicleType == ESolVehicleType::Boat ? 25.f : 8.f);
		const float TargetZ = GroundHit.Location.Z + RideHeight;
		Delta.Z = FMath::Clamp((TargetZ - GetActorLocation().Z) * 4.f * DeltaSeconds, -100.f * DeltaSeconds, 180.f * DeltaSeconds);
		VerticalSpeed = 0.f;
	}
	else
	{
		VerticalSpeed = FMath::Max(VerticalSpeed - 980.f * DeltaSeconds, -1500.f);
		Delta.Z = VerticalSpeed * DeltaSeconds;
	}

	FHitResult Hit;
	AddActorWorldOffset(Delta, true, &Hit);
	if (Hit.bBlockingHit)
	{
		if (!bLoggedBlockingHit && (VehicleType == ESolVehicleType::Boat || VehicleType == ESolVehicleType::Plane))
		{
			bLoggedBlockingHit = true;
			UE_LOG(LogTemp,Warning,TEXT("SOL_VEHICLE_BLOCKED type=%d other=%s location=%s impact=%s"),
				static_cast<int32>(VehicleType),*GetNameSafe(Hit.GetActor()),*GetActorLocation().ToString(),*Hit.ImpactPoint.ToString());
		}
		const float ImpactSpeed = FMath::Abs(ForwardSpeed);
		ForwardSpeed *= -0.17f;
		if (ImpactSpeed > 950.f && GetWorld()->GetTimeSeconds() - LastImpactTime > 0.5f)
		{
			LastImpactTime = GetWorld()->GetTimeSeconds();
			TakeDamage((ImpactSpeed - 900.f) * 0.018f, FDamageEvent(), nullptr, Hit.GetActor());
			if (AActor* HitActor = Hit.GetActor())
			{
				UGameplayStatics::ApplyDamage(HitActor, (ImpactSpeed - 900.f) * 0.016f, nullptr, this, UDamageType::StaticClass());
			}
		}
	}
}

void ASolVehicle::UpdateVisuals(float DeltaSeconds)
{
	const float DesiredLean = IsMotorcycle() ? -Steering * FMath::Clamp(FMath::Abs(ForwardSpeed) / 1700.f, 0.f, 1.f) * 10.f : 0.f;
	BodyMesh->SetRelativeRotation(FMath::RInterpTo(BodyMesh->GetRelativeRotation(), FRotator(0.f, 0.f, DesiredLean), DeltaSeconds, 4.f));
	BrakeLight->SetIntensity((Brake > 0.f || bHandbrake) && !bDestroyed ? 9000.f : 0.f);
	if (bDestroyed) FireLight->SetIntensity(9000.f + FMath::Sin(GetWorld()->GetTimeSeconds() * 17.f) * 4000.f);
	if (EngineAudio && EngineAudio->IsPlaying())
	{
		EngineAudio->SetPitchMultiplier(0.75f + FMath::Clamp(FMath::Abs(ForwardSpeed) / 3200.f, 0.f, 1.f) * 0.65f);
	}
	if (bSirenOn)
	{
		SirenTimer += DeltaSeconds;
		FireLight->SetLightColor((FMath::FloorToInt(SirenTimer * 5.f) % 2) ? FLinearColor::Blue : FLinearColor::Red);
		FireLight->SetIntensity(12000.f);
	}
}

float ASolVehicle::TakeDamage(float DamageAmount, FDamageEvent const& DamageEvent, AController* EventInstigator, AActor* DamageCauser)
{
	if (bDestroyed || DamageAmount <= 0.f) return 0.f;
	VehicleHealth = FMath::Max(0.f, VehicleHealth - DamageAmount);
	if (VehicleHealth <= 0.f) Explode();
	else if (VehicleHealth < MaxVehicleHealth * 0.25f) FireLight->SetIntensity(2500.f);
	return DamageAmount;
}

void ASolVehicle::Explode()
{
	if (bDestroyed) return;
	bDestroyed = true;
	bAutopilot = false;
	bSirenOn = false;
	SetDrivingInput(0.f, 0.f, 1.f);
	if (Driver.IsValid()) Exit();
	if (EngineAudio) EngineAudio->Stop();
	if (ExplosionEffect) UGameplayStatics::SpawnEmitterAtLocation(GetWorld(), ExplosionEffect, GetActorTransform());
	TArray<AActor*> IgnoreActors;
	IgnoreActors.Add(this);
	UGameplayStatics::ApplyRadialDamage(this, 75.f, GetActorLocation(), 480.f, UDamageType::StaticClass(), IgnoreActors, this, nullptr, true);
	FireLight->SetIntensity(13000.f);
	if (ASolWorldDirector* Director = ASolWorldDirector::Find(this))
	{
		if (bPoliceVehicle) Director->AddCrime(8.f, GetActorLocation());
	}
}
