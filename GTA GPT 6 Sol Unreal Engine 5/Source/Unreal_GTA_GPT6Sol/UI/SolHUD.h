#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "SolHUD.generated.h"

UCLASS()
class UNREAL_GTA_GPT6SOL_API ASolHUD : public AHUD
{
	GENERATED_BODY()
public:
	virtual void DrawHUD() override;
private:
	void DrawMeter(float X, float Y, float W, float Value, FLinearColor Fill);
	void DrawMinimap(float X, float Y, float Size);
};
