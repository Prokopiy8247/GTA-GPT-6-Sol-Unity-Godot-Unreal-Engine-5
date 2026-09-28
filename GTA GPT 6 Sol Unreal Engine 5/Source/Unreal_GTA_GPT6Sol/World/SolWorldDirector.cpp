#include "World/SolWorldDirector.h"

#include "World/SolSaveGame.h"
#include "Player/SolPlayerCharacter.h"
#include "Vehicles/SolVehicle.h"
#include "Weapons/SolWeaponTypes.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/PlayerController.h"
#include "Engine/DirectionalLight.h"
#include "Engine/ExponentialHeightFog.h"
#include "Components/ExponentialHeightFogComponent.h"
#include "Components/AudioComponent.h"
#include "Components/SceneComponent.h"
#include "Sound/SoundBase.h"
#include "EngineUtils.h"
#include "InputCoreTypes.h"

ASolWorldDirector::ASolWorldDirector()
{
	PrimaryActorTick.bCanEverTick = true;
	PrimaryActorTick.TickInterval = 0.1f;
	USceneComponent* SceneRoot=CreateDefaultSubobject<USceneComponent>(TEXT("WorldDirectorRoot"));
	SetRootComponent(SceneRoot);
	AmbientAudio=CreateDefaultSubobject<UAudioComponent>(TEXT("CityAmbient"));
	AmbientAudio->SetupAttachment(SceneRoot);
	AmbientAudio->bAutoActivate=false;
	AmbientAudio->bAllowSpatialization=false;
	RainAudio=CreateDefaultSubobject<UAudioComponent>(TEXT("WeatherRain"));
	RainAudio->SetupAttachment(SceneRoot);
	RainAudio->bAutoActivate=false;
	RainAudio->bAllowSpatialization=false;
	AdminItems = {
		TEXT("Teleport: Downtown"), TEXT("Teleport: Residential"), TEXT("Teleport: Industry"),
		TEXT("Teleport: Waterfront"), TEXT("Teleport: Park"), TEXT("Teleport: Airfield"),
		TEXT("Spawn compact car"), TEXT("Spawn boat"), TEXT("Spawn helicopter"), TEXT("Spawn airplane"),
		TEXT("Give fists"), TEXT("Give knife"), TEXT("Give bat"), TEXT("Give pistol"),
		TEXT("Give heavy pistol"), TEXT("Give SMG"), TEXT("Give shotgun"), TEXT("Give rifle"),
		TEXT("Give sniper"), TEXT("Give grenades"), TEXT("Give launcher"), TEXT("Refill selected ammo"),
		TEXT("Wanted +1"), TEXT("Clear wanted"), TEXT("Spawn police"),
		TEXT("Heal + armor"), TEXT("Cash + $5,000"), TEXT("Repair vehicle"),
		TEXT("Advance time +3h"), TEXT("Cycle weather"), TEXT("Toggle invulnerability"),
		TEXT("Toggle traffic"), TEXT("Toggle pedestrians"), TEXT("Save sandbox"), TEXT("Load sandbox"),
		TEXT("Spawn sedan"), TEXT("Spawn police cruiser"), TEXT("Spawn SUV"),
		TEXT("Spawn pickup"), TEXT("Spawn van"), TEXT("Spawn sports car"), TEXT("Spawn motorcycle"),
		TEXT("Max character skills"), TEXT("Reset character skills"), TEXT("Toggle FPS counter")
	};
}

ASolWorldDirector* ASolWorldDirector::Find(const UObject* Context)
{
	return Context ? Cast<ASolWorldDirector>(UGameplayStatics::GetActorOfClass(Context, StaticClass())) : nullptr;
}

ASolPlayerCharacter* ASolWorldDirector::GetPlayer() const
{
	return Cast<ASolPlayerCharacter>(UGameplayStatics::GetPlayerCharacter(this, 0));
}

void ASolWorldDirector::BeginPlay()
{
	Super::BeginPlay();
	InitializeTrafficRoutes();
	AmbientAudio->SetSound(LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_CityAmbient.SFX_CityAmbient")));
	RainAudio->SetSound(LoadObject<USoundBase>(nullptr,TEXT("/Game/GTA/Audio/SFX_RainLoop.SFX_RainLoop")));
	AmbientAudio->SetVolumeMultiplier(0.12f);
	RainAudio->SetVolumeMultiplier(0.14f);
	if (AmbientAudio->GetSound()) AmbientAudio->Play();
	NextPopulationUpdateAt = 0.f;
	SetWeather(Weather);
	SetTime(TimeHours);
}

void ASolWorldDirector::InitializeTrafficRoutes()
{
	const float Z = 115.f;
	const TArray<float> Xs = {-24000.f, -12000.f, 0.f, 12000.f, 24000.f};
	const TArray<float> Ys = {-16000.f, -4000.f, 8000.f, 20000.f};
	for (int32 Xi=0; Xi<Xs.Num()-1; ++Xi)
	{
		for (int32 Yi=0; Yi<Ys.Num()-1; ++Yi)
		{
			const float X0=Xs[Xi]+320.f, X1=Xs[Xi+1]-320.f;
			const float Y0=Ys[Yi]+320.f, Y1=Ys[Yi+1]-320.f;
			TrafficRoutes.Add({FVector(X0,Y0,Z), FVector(X1,Y0,Z), FVector(X1,Y1,Z), FVector(X0,Y1,Z)});
		}
	}
}

void ASolWorldDirector::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	TimeHours = FMath::Fmod(TimeHours + DeltaSeconds / 180.f, 24.f);
	const float Now = GetWorld()->GetTimeSeconds();
	HandleAdminInput();
	if (!bAdminOpen) HandleServiceInput();
	UpdateWanted(DeltaSeconds);
	if (Now >= NextPopulationUpdateAt)
	{
		NextPopulationUpdateAt = Now + 3.f;
		UpdatePopulation();
	}
	if (Now >= NextWeatherUpdateAt)
	{
		NextWeatherUpdateAt = Now + 2.f;
		UpdateEnvironment(DeltaSeconds);
	}
}

void ASolWorldDirector::AddCrime(float Severity, FVector Location)
{
	if (Severity <= 0.f) return;
	bool bWitness = Severity >= 2.f;
	for (const TWeakObjectPtr<ASolNPC>& Ref : NPCs)
	{
		ASolNPC* NPC = Ref.Get();
		if (!NPC || NPC->bDead) continue;
		if (FVector::DistSquared(NPC->GetActorLocation(), Location) < FMath::Square(Severity >= 4.f ? 4400.f : 2700.f))
		{
			NPC->Panic(Location);
			if (NPC->NPCRole == ESolNPCRole::Police || NPC->CanSeeActor(GetPlayer())) bWitness = true;
		}
	}
	if (!bWitness && Severity < 2.f) return;
	WantedPressure = FMath::Clamp(WantedPressure + FMath::Max(12.f, Severity*11.f), 0.f, 120.f);
	WantedLevel = FMath::Clamp(FMath::CeilToInt(WantedPressure / 24.f), 1, 5);
	LastKnownPosition = Location;
	SightLostAt = GetWorld()->GetTimeSeconds();
	LastWantedDecayAt = SightLostAt;
	NextPoliceSpawnAt = FMath::Min(NextPoliceSpawnAt, SightLostAt + 1.f);
}

void ASolWorldDirector::ClearWanted()
{
	WantedPressure = 0.f;
	WantedLevel = 0;
	bPoliceHaveSight = false;
	SightLostAt = 0.f;
}

void ASolWorldDirector::SetWantedLevel(int32 Level)
{
	WantedLevel = FMath::Clamp(Level, 0, 5);
	WantedPressure = WantedLevel * 24.f;
	if (WantedLevel == 0) ClearWanted();
	else
	{
		ASolPlayerCharacter* P = GetPlayer();
		if (P) LastKnownPosition = P->GetActorLocation();
		SightLostAt = GetWorld()->GetTimeSeconds();
		NextPoliceSpawnAt = SightLostAt;
	}
}

void ASolWorldDirector::UpdateWanted(float DeltaSeconds)
{
	if (WantedLevel == 0) return;
	ASolPlayerCharacter* Player = GetPlayer();
	if (!Player) return;
	const float Now = GetWorld()->GetTimeSeconds();
	bool bSeenNow = false;
	for (const TWeakObjectPtr<ASolNPC>& Ref : NPCs)
	{
		ASolNPC* N = Ref.Get();
		if (N && !N->bDead && (N->NPCRole == ESolNPCRole::Police || N->NPCRole == ESolNPCRole::Tactical) && N->CanSeeActor(Player))
		{
			bSeenNow = true;
			break;
		}
	}
	if (bSeenNow)
	{
		LastKnownPosition = Player->GetActorLocation();
		SightLostAt = Now;
		bPoliceHaveSight = true;
	}
	else
	{
		if (bPoliceHaveSight) SightLostAt = Now;
		bPoliceHaveSight = false;
		const float Delay = 18.f + WantedLevel * 9.f;
		if (Now - SightLostAt > Delay && Now - LastWantedDecayAt > 8.f)
		{
			LastWantedDecayAt = Now;
			SetWantedLevel(WantedLevel - 1);
			if (WantedLevel > 0) SightLostAt = Now - Delay + 8.f;
		}
	}
	if (Now >= NextPoliceSpawnAt && GetLivingPoliceCount() < FMath::Min(2 + WantedLevel * 2, 10))
	{
		SpawnPolice(WantedLevel >= 3 ? 2 : 1);
		NextPoliceSpawnAt = Now + FMath::Max(7.f, 18.f - WantedLevel * 2.f);
	}
}

ASolNPC* ASolWorldDirector::SpawnNPC(ESolNPCRole SpawnRole, FVector Location)
{
	Location.Z = 135.f;
	ASolNPC* NPC = GetWorld()->SpawnActor<ASolNPC>(ASolNPC::StaticClass(), Location, FRotator::ZeroRotator);
	if (NPC) { NPC->SetRole(SpawnRole); NPCs.Add(NPC); }
	return NPC;
}

void ASolWorldDirector::SpawnPolice(int32 Count)
{
	ASolPlayerCharacter* P = GetPlayer();
	if (!P) return;
	const FVector C = P->GetActorLocation();
	for (int32 I=0; I<Count; ++I)
	{
		const float Angle = FMath::FRandRange(0.f, 2.f*PI);
		FVector Spawn = C + FVector(FMath::Cos(Angle), FMath::Sin(Angle), 0.f) * FMath::FRandRange(2200.f, 3300.f);
		Spawn.X = FMath::Clamp(Spawn.X, -28500.f, 28500.f);
		Spawn.Y = FMath::Clamp(Spawn.Y, -17000.f, 28500.f);
		SpawnNPC(WantedLevel >= 4 ? ESolNPCRole::Tactical : ESolNPCRole::Police, Spawn);
	}
	if (WantedLevel >= 3 && GetTrafficCount() < 14) SpawnVehicle(0, true);
}

int32 ASolWorldDirector::GetLivingPoliceCount() const
{
	int32 Count = 0;
	for (const auto& Ref : NPCs) if (const ASolNPC* N=Ref.Get()) if (!N->bDead && (N->NPCRole==ESolNPCRole::Police || N->NPCRole==ESolNPCRole::Tactical)) ++Count;
	return Count;
}

int32 ASolWorldDirector::GetCivilianCount() const
{
	int32 Count = 0;
	for (const auto& Ref : NPCs) if (const ASolNPC* N=Ref.Get()) if (!N->bDead && N->NPCRole==ESolNPCRole::Civilian) ++Count;
	return Count;
}

int32 ASolWorldDirector::GetTrafficCount() const
{
	int32 Count = 0;
	for (const auto& Ref : Vehicles) if (const ASolVehicle* V=Ref.Get()) if (V->IsAutopilot()) ++Count;
	return Count;
}

void ASolWorldDirector::SpawnTrafficCar()
{
	if (TrafficRoutes.IsEmpty()) return;
	const ASolPlayerCharacter* Player=GetPlayer();
	const FVector PlayerPos=Player ? Player->GetActorLocation() : FVector::ZeroVector;
	TArray<TPair<int32,int32>> CandidateStarts;
	for (int32 RouteIndex=0; RouteIndex<TrafficRoutes.Num(); ++RouteIndex)
	{
		for (int32 PointIndex=0; PointIndex<TrafficRoutes[RouteIndex].Num(); ++PointIndex)
		{
			const float DistanceSquared=FVector::DistSquared2D(PlayerPos,TrafficRoutes[RouteIndex][PointIndex]);
			if (DistanceSquared>FMath::Square(2800.f) && DistanceSquared<FMath::Square(14500.f))
				CandidateStarts.Emplace(RouteIndex,PointIndex);
		}
	}
	const TPair<int32,int32> Chosen=CandidateStarts.IsEmpty()
		? TPair<int32,int32>(FMath::RandRange(0,TrafficRoutes.Num()-1),0)
		: CandidateStarts[FMath::RandRange(0,CandidateStarts.Num()-1)];
	TArray<FVector> Route;
	const TArray<FVector>& Source=TrafficRoutes[Chosen.Key];
	for (int32 I=0; I<Source.Num(); ++I) Route.Add(Source[(Chosen.Value+I)%Source.Num()]);
	ASolVehicle* Vehicle = GetWorld()->SpawnActor<ASolVehicle>(ASolVehicle::StaticClass(), Route[0], FRotator::ZeroRotator);
	if (Vehicle)
	{
		Vehicle->VehicleType = ESolVehicleType::Car;
		const int32 VariantIndex=FMath::RandRange(0,7);
		Vehicle->CarVariant=static_cast<ESolCarVariant>(VariantIndex==2 ? 0 : VariantIndex);
		Vehicle->InitializeVehicle();
		Vehicle->bNPCDriver = true;
		Vehicle->SetAutopilot(Route, true);
		Vehicles.Add(Vehicle);
	}
}

void ASolWorldDirector::UpdatePopulation()
{
	ASolPlayerCharacter* Player = GetPlayer();
	if (!Player) return;
	const FVector P = Player->GetActorLocation();
	NPCs.RemoveAll([&](const TWeakObjectPtr<ASolNPC>& Ref)
	{
		ASolNPC* N=Ref.Get();
		if (!N) return true;
		if (N->NPCRole == ESolNPCRole::Civilian && (!bPedestriansEnabled || FVector::DistSquared(N->GetActorLocation(),P)>FMath::Square(18000.f))) { N->Destroy(); return true; }
		if ((N->NPCRole == ESolNPCRole::Police || N->NPCRole == ESolNPCRole::Tactical) && WantedLevel==0 && FVector::DistSquared(N->GetActorLocation(),P)>FMath::Square(11000.f)) { N->Destroy(); return true; }
		return false;
	});
	Vehicles.RemoveAll([&](const TWeakObjectPtr<ASolVehicle>& Ref)
	{
		ASolVehicle* V=Ref.Get();
		if (!V) return true;
		if (V->IsAutopilot() && (!bTrafficEnabled || FVector::DistSquared(V->GetActorLocation(),P)>FMath::Square(24000.f))) { V->Destroy(); return true; }
		return false;
	});
	if (bPedestriansEnabled)
	{
		for (int32 Attempts=0; Attempts<4 && GetCivilianCount()<25; ++Attempts)
		{
			FVector S = P + FVector(FMath::FRandRange(-9000.f,9000.f),FMath::FRandRange(-9000.f,9000.f),0.f);
			S.X = FMath::Clamp(S.X,-28000.f,28000.f);
			S.Y = FMath::Clamp(S.Y,-17000.f,28000.f);
			if (FVector::DistSquared2D(S,P)>FMath::Square(1400.f)) SpawnNPC(ESolNPCRole::Civilian,S);
		}
	}
	if (bTrafficEnabled)
		for (int32 Attempts=0; Attempts<3 && GetTrafficCount()<12; ++Attempts) SpawnTrafficCar();
}

void ASolWorldDirector::SpawnVehicle(int32 TypeIndex, bool bPolice)
{
	ASolPlayerCharacter* P=GetPlayer();
	if (!P) return;
	FVector Pos = P->GetActorLocation() + P->GetActorForwardVector()*650.f + P->GetActorRightVector()*450.f;
	if (TypeIndex==1) Pos=FVector(0.f,-20000.f,120.f);
	if (TypeIndex==2) Pos=FVector(21000.f,18000.f,260.f);
	if (TypeIndex==3) Pos=FVector(14500.f,25500.f,320.f);
	const FRotator SpawnRotation = TypeIndex==1 ? FRotator(0.f,-90.f,0.f) : FRotator::ZeroRotator;
	ASolVehicle* V=GetWorld()->SpawnActor<ASolVehicle>(ASolVehicle::StaticClass(),Pos,SpawnRotation);
	if (V)
	{
		V->VehicleType=TypeIndex>=4 || bPolice ? ESolVehicleType::Car
			: static_cast<ESolVehicleType>(FMath::Clamp(TypeIndex,0,3));
		if (TypeIndex>=4) V->CarVariant=static_cast<ESolCarVariant>(FMath::Clamp(TypeIndex-3,1,7));
		if (bPolice) V->CarVariant=ESolCarVariant::PoliceCar;
		V->bPoliceVehicle=bPolice;
		V->InitializeVehicle();
		if (bPolice) V->SetAutopilotTarget(LastKnownPosition);
		Vehicles.Add(V);
	}
}

void ASolWorldDirector::SetWeather(ESolWeather NewWeather)
{
	Weather=NewWeather;
	if (RainAudio && RainAudio->GetSound())
	{
		if (Weather==ESolWeather::Rain || Weather==ESolWeather::Storm)
		{
			if (!RainAudio->IsPlaying()) RainAudio->Play();
		}
		else RainAudio->Stop();
	}
	UpdateEnvironment(0.f);
}

void ASolWorldDirector::SetTime(float NewHours)
{
	TimeHours=FMath::Fmod(NewHours+24.f,24.f);
	UpdateEnvironment(0.f);
}

FString ASolWorldDirector::GetWeatherName() const
{
	switch (Weather)
	{
	case ESolWeather::Clear: return TEXT("Clear");
	case ESolWeather::Cloudy: return TEXT("Cloudy");
	case ESolWeather::Rain: return TEXT("Rain");
	case ESolWeather::Fog: return TEXT("Fog");
	default: return TEXT("Storm");
	}
}

void ASolWorldDirector::UpdateEnvironment(float DeltaSeconds)
{
	// At noon the directional light points down into the city. Positive pitch
	// from this formula places the sun below the horizon at night.
	const float DayAngle = -65.f * FMath::Sin((TimeHours-6.f)/24.f*2.f*PI);
	for (TActorIterator<ADirectionalLight> It(GetWorld()); It; ++It)
	{
		It->SetActorRotation(FRotator(DayAngle,TimeHours*15.f-120.f,0.f));
		break;
	}
	for (TActorIterator<AExponentialHeightFog> It(GetWorld()); It; ++It)
	{
		float Density=Weather==ESolWeather::Fog ? 0.025f : (Weather==ESolWeather::Rain || Weather==ESolWeather::Storm ? 0.01f : 0.003f);
		It->GetComponent()->SetFogDensity(Density);
		break;
	}
}

void ASolWorldDirector::ToggleAdmin()
{
	bAdminOpen=!bAdminOpen;
	if (APlayerController* PC=UGameplayStatics::GetPlayerController(this,0))
	{
		PC->bShowMouseCursor=bAdminOpen;
		PC->SetIgnoreLookInput(bAdminOpen);
	}
}

void ASolWorldDirector::HandleAdminInput()
{
	APlayerController* PC=UGameplayStatics::GetPlayerController(this,0);
	if (!PC) return;
	if (PC->WasInputKeyJustPressed(EKeys::F1)) ToggleAdmin();
	if (!bAdminOpen) return;
	if (PC->WasInputKeyJustPressed(EKeys::Up)) AdminSelection=(AdminSelection+AdminItems.Num()-1)%AdminItems.Num();
	if (PC->WasInputKeyJustPressed(EKeys::Down)) AdminSelection=(AdminSelection+1)%AdminItems.Num();
	if (PC->WasInputKeyJustPressed(EKeys::Enter)) RunAdminAction(AdminSelection);
	if (PC->WasInputKeyJustPressed(EKeys::Escape)) ToggleAdmin();
}

FString ASolWorldDirector::GetNearbyServiceName() const
{
	const ASolPlayerCharacter* P=GetPlayer();
	if (!P) return FString();
	const FVector Pos=P->GetActorLocation();
	struct FService { FVector Location; const TCHAR* Name; };
	const FService Services[] = {
		{FVector(-5000.f,9000.f,0.f),TEXT("NORTH QUAY ARMS  /  E: BUY NEXT WEAPON OR AMMO")},
		{FVector(16000.f,-5000.f,0.f),TEXT("TIDELINE GARAGE  /  E REPAIR $250  P PAINT $150  U ENGINE $500  G STORE")},
		{FVector(-21000.f,19000.f,0.f),TEXT("HARBOR CLINIC  /  E: HEAL ($150)")},
		{FVector(-18000.f,12000.f,0.f),TEXT("CANAL SAFEHOUSE  /  E: SAVE & REST")},
		{FVector(0.f,-18000.f,0.f),TEXT("PIER  /  E: LAUNCH BOAT")},
		{FVector(21000.f,22000.f,0.f),TEXT("AIRFIELD  /  E: SPAWN HELICOPTER")}
	};
	for (const FService& S : Services)
		if (FVector::DistSquared2D(Pos,S.Location)<FMath::Square(850.f)) return S.Name;
	return FString();
}

void ASolWorldDirector::HandleServiceInput()
{
	APlayerController* PC=UGameplayStatics::GetPlayerController(this,0);
	ASolPlayerCharacter* P=GetPlayer();
	if (!PC || !P) return;
	const bool bUse=PC->WasInputKeyJustPressed(EKeys::E);
	const bool bStore=PC->WasInputKeyJustPressed(EKeys::G);
	const bool bPaint=PC->WasInputKeyJustPressed(EKeys::P);
	const bool bUpgrade=PC->WasInputKeyJustPressed(EKeys::U);
	if (!bUse && !bStore && !bPaint && !bUpgrade) return;
	const FVector Pos=P->GetActorLocation();
	const FVector Spots[] = {FVector(-5000.f,9000.f,0.f),FVector(16000.f,-5000.f,0.f),FVector(-21000.f,19000.f,0.f),FVector(-18000.f,12000.f,0.f),FVector(0.f,-18000.f,0.f),FVector(21000.f,22000.f,0.f)};
	if (FVector::DistSquared2D(Pos,Spots[1])<FMath::Square(850.f) && (bStore || bPaint || bUpgrade))
	{
		if (bPaint)
		{
			if (ASolVehicle* Current=P->GetCurrentVehicle())
				if (!Current->bPoliceVehicle && P->Cash>=150) { P->Cash-=150; Current->ApplyPaintPreset(Current->PaintPresetIndex%5+1); }
		}
		else if (bUpgrade)
		{
			if (ASolVehicle* Current=P->GetCurrentVehicle())
				if (Current->EngineUpgrade<3 && P->Cash>=500) { P->Cash-=500; Current->UpgradeEngine(); }
		}
		else if (ASolVehicle* Current=P->GetCurrentVehicle())
		{
			StoredVehicleType=static_cast<uint8>(Current->VehicleType);
			StoredCarVariant=static_cast<uint8>(Current->CarVariant);
			StoredPaintPreset=Current->PaintPresetIndex;
			StoredEngineUpgrade=Current->EngineUpgrade;
			bHasStoredVehicle=true;
			P->ExitVehicle();
			Current->Destroy();
		}
		else if (bHasStoredVehicle)
		{
			const FVector RetrieveLocation(16600.f,-4400.f,125.f);
			ASolVehicle* Stored=GetWorld()->SpawnActor<ASolVehicle>(ASolVehicle::StaticClass(),RetrieveLocation,FRotator::ZeroRotator);
			if (Stored)
			{
				Stored->VehicleType=static_cast<ESolVehicleType>(FMath::Clamp<int32>(StoredVehicleType,0,3));
				Stored->CarVariant=static_cast<ESolCarVariant>(FMath::Clamp<int32>(StoredCarVariant,0,7));
				Stored->PaintPresetIndex=StoredPaintPreset;
				Stored->EngineUpgrade=StoredEngineUpgrade;
				Stored->InitializeVehicle();
				Vehicles.Add(Stored);
				bHasStoredVehicle=false;
			}
		}
		return;
	}
	if (!bUse) return;
	for (int32 I=0; I<6; ++I)
	{
		if (FVector::DistSquared2D(Pos,Spots[I])>FMath::Square(850.f)) continue;
		switch (I)
		{
		case 0:
		{
			const ESolWeaponType ShopTypes[] = {ESolWeaponType::Pistol,ESolWeaponType::SMG,ESolWeaponType::Shotgun,ESolWeaponType::Rifle};
			const int32 Prices[] = {200,500,750,1100};
			bool bBought=false;
			for (int32 J=0; J<4; ++J)
			{
				bool bOwned=false;
				for (const FSolWeaponState& S : P->Inventory) if (S.Type==ShopTypes[J]) { bOwned=true; break; }
				if (!bOwned && P->Cash>=Prices[J]) { P->Cash-=Prices[J]; P->GiveWeapon(ShopTypes[J],90); bBought=true; break; }
			}
			if (!bBought && P->Cash>=80) { P->Cash-=80; P->GiveWeapon(P->GetCurrentWeaponType(),90); }
			break;
		}
		case 1: if (ASolVehicle* V=P->GetCurrentVehicle()) if (P->Cash>=250) { P->Cash-=250; V->Repair(); } break;
		case 2: if (P->Cash>=150) { P->Cash-=150; P->Heal(100.f); } break;
		case 3: SaveSandbox(); SetTime(TimeHours+8.f); break;
		case 4: SpawnVehicle(1); break;
		case 5: SpawnVehicle(2); break;
		default: break;
		}
		break;
	}
}

void ASolWorldDirector::RunAdminAction(int32 Index)
{
	ASolPlayerCharacter* P=GetPlayer();
	if (!P) return;
	const FVector Locations[] = {
		FVector(0.f,8000.f,250.f), FVector(-20000.f,18000.f,250.f), FVector(17000.f,-8000.f,250.f),
		FVector(0.f,-17000.f,250.f), FVector(-23000.f,-8000.f,250.f), FVector(21000.f,22000.f,250.f)
	};
	if (Index>=0 && Index<6) { if (P->GetCurrentVehicle()) P->ExitVehicle(); P->SetActorLocation(Locations[Index]); return; }
	if (Index>=6 && Index<=9) { SpawnVehicle(Index-6); return; }
	switch (Index)
	{
	case 10: case 11: case 12: case 13: case 14: case 15:
	case 16: case 17: case 18: case 19: case 20:
	{
		const ESolWeaponType Type=static_cast<ESolWeaponType>(Index-10);
		P->GiveWeapon(Type,Type==ESolWeaponType::Fists || Type==ESolWeaponType::Knife || Type==ESolWeaponType::Bat ? 0 : 120);
		for (int32 I=0; I<P->Inventory.Num(); ++I)
			if (P->Inventory[I].Type==Type) { P->SelectWeaponSlot(I); break; }
		break;
	}
	case 21: P->GiveWeapon(P->GetCurrentWeaponType(), 240); break;
	case 22: SetWantedLevel(WantedLevel+1); break;
	case 23: ClearWanted(); break;
	case 24: SpawnPolice(2); break;
	case 25: P->Heal(100.f); P->Armor=100.f; break;
	case 26: P->Cash+=5000; break;
	case 27: if (ASolVehicle* V=P->GetCurrentVehicle()) V->Repair(); break;
	case 28: SetTime(TimeHours+3.f); break;
	case 29: SetWeather(static_cast<ESolWeather>((static_cast<uint8>(Weather)+1)%5)); break;
	case 30: bGodMode=!bGodMode; P->SetGodMode(bGodMode); break;
	case 31: bTrafficEnabled=!bTrafficEnabled; break;
	case 32: bPedestriansEnabled=!bPedestriansEnabled; break;
	case 33: SaveSandbox(); break;
	case 34: LoadSandbox(); break;
	case 35: case 36: case 37: case 38: case 39: case 40: case 41: SpawnVehicle(Index-31); break;
	case 42: P->StaminaSkill=100.f; P->ShootingSkill=100.f; P->DrivingSkill=100.f; P->FlyingSkill=100.f; P->LungSkill=100.f; break;
	case 43: P->StaminaSkill=0.f; P->ShootingSkill=0.f; P->DrivingSkill=0.f; P->FlyingSkill=0.f; P->LungSkill=0.f; break;
	case 44: if (APlayerController* PC=UGameplayStatics::GetPlayerController(this,0)) PC->ConsoleCommand(TEXT("stat fps")); break;
	default: break;
	}
}

void ASolWorldDirector::SaveSandbox()
{
	ASolPlayerCharacter* P=GetPlayer();
	if (!P) return;
	USolSaveGame* Data=Cast<USolSaveGame>(UGameplayStatics::CreateSaveGameObject(USolSaveGame::StaticClass()));
	if (!Data) return;
	Data->PlayerPosition=P->GetActorLocation();
	Data->Health=P->Health;
	Data->Armor=P->Armor;
	Data->Cash=P->Cash;
	Data->Inventory=P->Inventory;
	Data->CurrentWeaponIndex=P->CurrentWeaponIndex;
	Data->bHasStoredVehicle=bHasStoredVehicle;
	Data->StoredVehicleType=StoredVehicleType;
	Data->StoredCarVariant=StoredCarVariant;
	Data->StoredPaintPreset=StoredPaintPreset;
	Data->StoredEngineUpgrade=StoredEngineUpgrade;
	Data->StaminaSkill=P->StaminaSkill;
	Data->ShootingSkill=P->ShootingSkill;
	Data->DrivingSkill=P->DrivingSkill;
	Data->FlyingSkill=P->FlyingSkill;
	Data->LungSkill=P->LungSkill;
	Data->TimeHours=TimeHours;
	Data->Weather=static_cast<uint8>(Weather);
	UGameplayStatics::SaveGameToSlot(Data,TEXT("HarborCitySandbox"),0);
}

void ASolWorldDirector::LoadSandbox()
{
	ASolPlayerCharacter* P=GetPlayer();
	USolSaveGame* Data=Cast<USolSaveGame>(UGameplayStatics::LoadGameFromSlot(TEXT("HarborCitySandbox"),0));
	if (!P || !Data) return;
	if (P->GetCurrentVehicle()) P->ExitVehicle();
	P->SetActorLocation(Data->PlayerPosition);
	P->Health=Data->Health;
	P->Armor=Data->Armor;
	P->Cash=Data->Cash;
	bHasStoredVehicle=Data->bHasStoredVehicle;
	StoredVehicleType=Data->StoredVehicleType;
	StoredCarVariant=Data->StoredCarVariant;
	StoredPaintPreset=Data->StoredPaintPreset;
	StoredEngineUpgrade=Data->StoredEngineUpgrade;
	P->StaminaSkill=Data->StaminaSkill;
	P->ShootingSkill=Data->ShootingSkill;
	P->DrivingSkill=Data->DrivingSkill;
	P->FlyingSkill=Data->FlyingSkill;
	P->LungSkill=Data->LungSkill;
	if (!Data->Inventory.IsEmpty())
	{
		P->Inventory=Data->Inventory;
		P->SelectWeaponSlot(FMath::Clamp(Data->CurrentWeaponIndex,0,P->Inventory.Num()-1));
	}
	SetTime(Data->TimeHours);
	SetWeather(static_cast<ESolWeather>(Data->Weather%5));
	ClearWanted();
}
