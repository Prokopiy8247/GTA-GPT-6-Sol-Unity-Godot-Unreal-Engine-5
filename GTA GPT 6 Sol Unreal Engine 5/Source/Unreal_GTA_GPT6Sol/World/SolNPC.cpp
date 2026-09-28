#include "World/SolNPC.h"

#include "World/SolWorldDirector.h"
#include "Player/SolPlayerCharacter.h"
#include "Vehicles/SolVehicle.h"
#include "Components/CapsuleComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/World.h"

ASolNPC::ASolNPC()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickInterval = 0.12f;
	GetCapsuleComponent()->InitCapsuleSize(34.f, 90.f);
	GetCharacterMovement()->bRunPhysicsWithNoController = true;
	GetCharacterMovement()->bOrientRotationToMovement = true;
	GetCharacterMovement()->RotationRate = FRotator(0.f, 360.f, 0.f);
	GetCharacterMovement()->MaxWalkSpeed = 180.f;
	Visual = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("AuthoredNPCVisual"));
	Visual->SetupAttachment(GetCapsuleComponent());
	Visual->SetRelativeLocation(FVector(0.f, 0.f, -90.f));
	Visual->SetCollisionEnabled(ECollisionEnabled::NoCollision);
	GetMesh()->SetVisibility(false);
}

void ASolNPC::BeginPlay()
{
	Super::BeginPlay();
	UpdateVisual();
	ChooseDestination();
}

void ASolNPC::SetRole(ESolNPCRole NewRole)
{
	NPCRole = NewRole;
	GetCharacterMovement()->MaxWalkSpeed = NPCRole == ESolNPCRole::Civilian ? 180.f : 280.f;
	if (HasActorBegunPlay()) UpdateVisual();
}

void ASolNPC::UpdateVisual()
{
	const TCHAR* Path = NPCRole == ESolNPCRole::Police || NPCRole == ESolNPCRole::Tactical
		? TEXT("/Game/GTA/Generated/SM_Police.SM_Police")
		: TEXT("/Game/GTA/Generated/SM_Pedestrian.SM_Pedestrian");
	UStaticMesh* VisualMesh = LoadObject<UStaticMesh>(nullptr, Path);
	if (!VisualMesh) VisualMesh = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Capsule.Capsule"));
	Visual->SetStaticMesh(VisualMesh);
	if (VisualMesh && VisualMesh->GetPathName().StartsWith(TEXT("/Engine/"))) Visual->SetWorldScale3D(FVector(0.65f, 0.65f, 1.7f));
}

void ASolNPC::ChooseDestination()
{
	const FVector P = GetActorLocation();
	Destination = P + FVector(FMath::FRandRange(-1600.f, 1600.f), FMath::FRandRange(-1600.f, 1600.f), 0.f);
	Destination.X = FMath::Clamp(Destination.X, -28500.f, 28500.f);
	Destination.Y = FMath::Clamp(Destination.Y, -17000.f, 28500.f);
}

void ASolNPC::Panic(const FVector& ThreatLocation)
{
	if (bDead || NPCRole != ESolNPCRole::Civilian) return;
	bPanicked = true;
	Threat = ThreatLocation;
	PanicEndTime = GetWorld()->GetTimeSeconds() + 12.f;
	GetCharacterMovement()->MaxWalkSpeed = 430.f;
}

bool ASolNPC::CanSeeActor(const AActor* Target) const
{
	if (!Target || bDead) return false;
	const FVector Start = GetActorLocation() + FVector(0.f, 0.f, 60.f);
	const FVector End = Target->GetActorLocation() + FVector(0.f, 0.f, 50.f);
	float SightDistance=NPCRole == ESolNPCRole::Civilian ? 2800.f : 4600.f;
	if (const ASolPlayerCharacter* P=Cast<ASolPlayerCharacter>(Target)) if (P->IsStealth()) SightDistance*=0.55f;
	if (FVector::DistSquared(Start, End) > FMath::Square(SightDistance)) return false;
	FHitResult Hit;
	FCollisionQueryParams Params(SCENE_QUERY_STAT(SolNPCSight), false, this);
	Params.AddIgnoredActor(this);
	if (!GetWorld()->LineTraceSingleByChannel(Hit, Start, End, ECC_Visibility, Params)) return true;
	if (Hit.GetActor() == Target) return true;
	if (const ASolPlayerCharacter* P=Cast<ASolPlayerCharacter>(Target)) return Hit.GetActor() == P->GetCurrentVehicle();
	return false;
}

void ASolNPC::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (bDead) return;
	ASolPlayerCharacter* Player = Cast<ASolPlayerCharacter>(UGameplayStatics::GetPlayerCharacter(this, 0));
	ASolWorldDirector* Director = ASolWorldDirector::Find(this);
	const float Now = GetWorld()->GetTimeSeconds();
	if (NPCRole == ESolNPCRole::Police || NPCRole == ESolNPCRole::Tactical)
	{
		if (Player && Director && Director->GetWantedLevel() > 0)
		{
			Destination = CanSeeActor(Player) ? Player->GetActorLocation() : Director->GetLastKnownPosition();
			if (CanSeeActor(Player) && FVector::DistSquared(GetActorLocation(), Player->GetActorLocation()) < FMath::Square(2600.f) && Now >= AttackTime && Director->GetWantedLevel() >= 2)
			{
				AttackTime = Now + (NPCRole == ESolNPCRole::Tactical ? 0.55f : 1.1f);
				UGameplayStatics::ApplyDamage(Player, NPCRole == ESolNPCRole::Tactical ? 10.f : 6.f, GetController(), this, nullptr);
			}
		}
		else if (Now > DecisionTime) { ChooseDestination(); DecisionTime = Now + 5.f; }
	}
	else if (bPanicked)
	{
		const FVector Away = (GetActorLocation() - Threat).GetSafeNormal2D();
		Destination = GetActorLocation() + Away * 1600.f;
		if (Now > PanicEndTime) { bPanicked = false; GetCharacterMovement()->MaxWalkSpeed = 180.f; ChooseDestination(); }
	}
	else if (Now > DecisionTime || FVector::DistSquared2D(GetActorLocation(), Destination) < FMath::Square(120.f))
	{
		ChooseDestination();
		DecisionTime = Now + FMath::FRandRange(3.f, 8.f);
	}
	FVector Toward = Destination - GetActorLocation();
	Toward.Z = 0.f;
	if (Toward.SizeSquared() > FMath::Square(110.f)) AddMovementInput(Toward.GetSafeNormal(), 1.f);
}

float ASolNPC::TakeDamage(float DamageAmount, FDamageEvent const& DamageEvent, AController* EventInstigator, AActor* DamageCauser)
{
	if (bDead) return 0.f;
	Health -= DamageAmount;
	Panic(DamageCauser ? DamageCauser->GetActorLocation() : GetActorLocation());
	if (Health <= 0.f)
	{
		bDead = true;
		GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
		GetCharacterMovement()->DisableMovement();
		Visual->SetRelativeRotation(FRotator(0.f, 0.f, 80.f));
		SetLifeSpan(18.f);
	}
	return DamageAmount;
}
