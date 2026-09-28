#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "SolGameMode.generated.h"

UCLASS()
class UNREAL_GTA_GPT6SOL_API ASolGameMode : public AGameModeBase
{
	GENERATED_BODY()
public:
	ASolGameMode();
	virtual void StartPlay() override;
private:
	void RunSmokePhaseOne();
	void RunSmokePhaseTwo();
	TWeakObjectPtr<class ASolVehicle> SmokeVehicle;
	TWeakObjectPtr<class ASolVehicle> SmokeBoat;
	TWeakObjectPtr<class ASolVehicle> SmokeHelicopter;
	TWeakObjectPtr<class ASolVehicle> SmokePlane;
	FVector SmokeVehicleStart = FVector::ZeroVector;
	FVector SmokeBoatStart = FVector::ZeroVector;
	FVector SmokeHelicopterStart = FVector::ZeroVector;
	FVector SmokePlaneStart = FVector::ZeroVector;
	bool bSmokePhaseOnePassed = false;
};
