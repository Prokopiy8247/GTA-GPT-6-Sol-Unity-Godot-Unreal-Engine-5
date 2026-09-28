#include "Core/SolGameMode.h"
#include "Player/SolPlayerCharacter.h"
#include "World/SolWorldDirector.h"
#include "UI/SolHUD.h"
#include "Vehicles/SolVehicle.h"
#include "World/SolNPC.h"
#include "Kismet/GameplayStatics.h"
#include "EngineUtils.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "TimerManager.h"

ASolGameMode::ASolGameMode()
{
	DefaultPawnClass=ASolPlayerCharacter::StaticClass();
	HUDClass=ASolHUD::StaticClass();
}

void ASolGameMode::StartPlay()
{
	Super::StartPlay();
	if (!ASolWorldDirector::Find(this)) GetWorld()->SpawnActor<ASolWorldDirector>();
	if (FParse::Param(FCommandLine::Get(),TEXT("SolSmoke")))
	{
		FTimerHandle Timer;
		GetWorldTimerManager().SetTimer(Timer,this,&ASolGameMode::RunSmokePhaseOne,5.f,false);
	}
}

void ASolGameMode::RunSmokePhaseOne()
{
	ASolPlayerCharacter* P=Cast<ASolPlayerCharacter>(UGameplayStatics::GetPlayerCharacter(this,0));
	ASolWorldDirector* D=ASolWorldDirector::Find(this);
	if (!P || !D)
	{
		UE_LOG(LogTemp,Error,TEXT("SOL_SMOKE FAIL: player or director missing"));
		return;
	}
	const int32 Pedestrians=D->GetCivilianCount();
	const int32 Traffic=D->GetTrafficCount();
	D->SetWantedLevel(3);
	const bool bWantedWorks=D->GetWantedLevel()==3;
	D->ClearWanted();
	P->GiveWeapon(ESolWeaponType::Rifle,90);
	bool bRifle=false;
	for (const FSolWeaponState& Weapon : P->Inventory) if (Weapon.Type==ESolWeaponType::Rifle) bRifle=true;
	D->SpawnVehicle(0);
	ASolVehicle* Nearest=nullptr;
	float BestDist=FMath::Square(2000.f);
	for (TActorIterator<ASolVehicle> It(GetWorld()); It; ++It)
	{
		ASolVehicle* V=*It;
		if (V->IsAutopilot() || V->GetDriver()) continue;
		const float Dist=FVector::DistSquared(V->GetActorLocation(),P->GetActorLocation());
		if (Dist<BestDist) { Nearest=V; BestDist=Dist; }
	}
	const bool bEntered=Nearest && P->EnterVehicle(Nearest) && P->GetCurrentVehicle()==Nearest;
	if (bEntered) P->ExitVehicle();
	bool bGarage=false;
	if (Nearest)
	{
		Nearest->ApplyPaintPreset(2);
		Nearest->UpgradeEngine();
		bGarage=Nearest->PaintPresetIndex==2 && Nearest->EngineUpgrade==1;
	}
	bSmokePhaseOnePassed=Pedestrians>0 && Traffic>=3 && bWantedWorks && bRifle && bEntered && bGarage;
	UE_LOG(LogTemp,Display,TEXT("SOL_SMOKE PHASE1: player=(%.0f,%.0f,%.0f) peds=%d traffic=%d wanted=%s rifle=%s enter_exit=%s garage=%s"),
		P->GetActorLocation().X,P->GetActorLocation().Y,P->GetActorLocation().Z,Pedestrians,Traffic,
		bWantedWorks?TEXT("yes"):TEXT("no"),bRifle?TEXT("yes"):TEXT("no"),bEntered?TEXT("yes"):TEXT("no"),bGarage?TEXT("yes"):TEXT("no"));
	if (Nearest)
	{
		SmokeVehicle=Nearest;
		SmokeVehicleStart=Nearest->GetActorLocation();
		TArray<FVector> TestRoute;
		TestRoute.Add(SmokeVehicleStart+FVector(3000.f,0.f,0.f));
		Nearest->SetAutopilot(TestRoute,false);
	}
	D->SpawnVehicle(1);
	D->SpawnVehicle(2);
	D->SpawnVehicle(3);
	for (TActorIterator<ASolVehicle> It(GetWorld()); It; ++It)
	{
		ASolVehicle* V=*It;
		if (V->VehicleType==ESolVehicleType::Boat && !SmokeBoat.IsValid()) { SmokeBoat=V; SmokeBoatStart=V->GetActorLocation(); V->SetDrivingInput(1.f,0.f,0.f); }
		if (V->VehicleType==ESolVehicleType::Helicopter && !SmokeHelicopter.IsValid()) { SmokeHelicopter=V; SmokeHelicopterStart=V->GetActorLocation(); V->SetDrivingInput(1.f,0.f,0.f); }
		if (V->VehicleType==ESolVehicleType::Plane && !SmokePlane.IsValid()) { SmokePlane=V; SmokePlaneStart=V->GetActorLocation(); V->SetDrivingInput(1.f,0.f,0.f); }
	}
	FTimerHandle Timer;
	GetWorldTimerManager().SetTimer(Timer,this,&ASolGameMode::RunSmokePhaseTwo,4.f,false);
}

void ASolGameMode::RunSmokePhaseTwo()
{
	ASolWorldDirector* D=ASolWorldDirector::Find(this);
	int32 SedansBefore=0;
	for (TActorIterator<ASolVehicle> It(GetWorld()); It; ++It)
		if (!It->bNPCDriver && It->VehicleType==ESolVehicleType::Car && It->CarVariant==ESolCarVariant::Sedan) ++SedansBefore;
	if (D) D->SpawnVehicle(4);
	int32 SedansAfter=0;
	for (TActorIterator<ASolVehicle> It(GetWorld()); It; ++It)
		if (!It->bNPCDriver && It->VehicleType==ESolVehicleType::Car && It->CarVariant==ESolCarVariant::Sedan) ++SedansAfter;
	const bool bSedanSpawned=SedansAfter>SedansBefore;
	ASolVehicle* V=SmokeVehicle.Get();
	const float Moved=V ? FVector::Dist2D(V->GetActorLocation(),SmokeVehicleStart) : 0.f;
	const bool bVehicleMoved=Moved>80.f;
	const float BoatMoved=SmokeBoat.IsValid() ? FVector::Dist2D(SmokeBoat->GetActorLocation(),SmokeBoatStart) : 0.f;
	const float HelicopterRise=SmokeHelicopter.IsValid() ? SmokeHelicopter->GetActorLocation().Z-SmokeHelicopterStart.Z : 0.f;
	const float PlaneMoved=SmokePlane.IsValid() ? FVector::Dist2D(SmokePlane->GetActorLocation(),SmokePlaneStart) : 0.f;
	UE_LOG(LogTemp,Display,TEXT("SOL_SMOKE VEHICLES: boat_distance=%.0f cm helicopter_rise=%.0f cm plane_distance=%.0f cm sedan_spawn=%s"),BoatMoved,HelicopterRise,PlaneMoved,bSedanSpawned?TEXT("yes"):TEXT("no"));
	if (bSmokePhaseOnePassed && bVehicleMoved && BoatMoved>100.f && HelicopterRise>50.f && PlaneMoved>100.f && bSedanSpawned)
	{
		UE_LOG(LogTemp,Display,TEXT("SOL_SMOKE PASS: vehicle_distance=%.0f cm, phase1=pass"),Moved);
	}
	else
	{
		UE_LOG(LogTemp,Error,TEXT("SOL_SMOKE FAIL: vehicle_distance=%.0f cm, phase1=%s"),Moved,
			bSmokePhaseOnePassed ? TEXT("pass") : TEXT("fail"));
	}
}
