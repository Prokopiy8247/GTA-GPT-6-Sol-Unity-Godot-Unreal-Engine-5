#include "UI/SolHUD.h"

#include "World/SolWorldDirector.h"
#include "Player/SolPlayerCharacter.h"
#include "Vehicles/SolVehicle.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"

void ASolHUD::DrawMeter(float X, float Y, float W, float Value, FLinearColor Fill)
{
	DrawRect(FLinearColor(0.025f,0.04f,0.06f,0.82f),X,Y,W,12.f);
	DrawRect(Fill,X+2.f,Y+2.f,(W-4.f)*FMath::Clamp(Value,0.f,1.f),8.f);
}

void ASolHUD::DrawMinimap(float X, float Y, float Size)
{
	DrawRect(FLinearColor(0.025f,0.055f,0.07f,0.88f),X,Y,Size,Size);
	const float RoadsX[] = {-24000.f,-12000.f,0.f,12000.f,24000.f};
	const float RoadsY[] = {-16000.f,-4000.f,8000.f,20000.f};
	const auto MX=[&](float WorldX){return X+(WorldX+30000.f)/60000.f*Size;};
	const auto MY=[&](float WorldY){return Y+Size-(WorldY+30000.f)/60000.f*Size;};
	for (float RX : RoadsX) DrawRect(FLinearColor(0.22f,0.31f,0.34f,0.75f),MX(RX)-2.f,Y,4.f,Size);
	for (float RY : RoadsY) DrawRect(FLinearColor(0.22f,0.31f,0.34f,0.75f),X,MY(RY)-2.f,Size,4.f);
	DrawRect(FLinearColor(0.05f,0.45f,0.54f,0.65f),X,MY(-19000.f),Size,Y+Size-MY(-19000.f));
	if (APawn* P=UGameplayStatics::GetPlayerPawn(this,0))
	{
		const FVector Pos=P->GetActorLocation();
		DrawRect(FLinearColor(1.f,0.33f,0.22f,1.f),MX(Pos.X)-4.f,MY(Pos.Y)-4.f,8.f,8.f);
	}
	DrawRect(FLinearColor(0.4f,0.86f,0.8f,1.f),X,Y,Size,2.f);
	DrawRect(FLinearColor(0.4f,0.86f,0.8f,1.f),X,Y+Size-2.f,Size,2.f);
}

void ASolHUD::DrawHUD()
{
	Super::DrawHUD();
	if (!Canvas || !GEngine) return;
	const float W=Canvas->SizeX, H=Canvas->SizeY;
	UFont* Font=GEngine->GetSmallFont();
	const FLinearColor TextColor(0.86f,0.95f,0.93f,1.f);
	const FLinearColor Teal(0.16f,0.83f,0.74f,1.f);
	const FLinearColor Coral(1.f,0.35f,0.24f,1.f);
	ASolPlayerCharacter* Player=Cast<ASolPlayerCharacter>(UGameplayStatics::GetPlayerCharacter(this,0));
	ASolWorldDirector* Director=ASolWorldDirector::Find(this);
	if (!Player) return;
	DrawRect(FLinearColor(0.01f,0.025f,0.04f,0.72f),24.f,H-150.f,315.f,120.f);
	DrawText(TEXT("HARBOR CITY  /  FREE ROAM"),Teal,38.f,H-141.f,Font,1.f);
	DrawText(TEXT("HEALTH"),TextColor,38.f,H-115.f,Font,0.9f);
	DrawMeter(102.f,H-108.f,218.f,Player->Health/100.f,Coral);
	DrawText(TEXT("ARMOR"),TextColor,38.f,H-89.f,Font,0.9f);
	DrawMeter(102.f,H-82.f,218.f,Player->Armor/100.f,Teal);
	DrawText(FString::Printf(TEXT("$ %d     AMMO %d / %d"),Player->Cash,Player->GetCurrentAmmo(),Player->GetReserveAmmo()),TextColor,38.f,H-58.f,Font,0.95f);
	DrawRect(Teal,W*0.5f-8.f,H*0.5f,5.f,2.f);
	DrawRect(Teal,W*0.5f+3.f,H*0.5f,5.f,2.f);
	DrawRect(Teal,W*0.5f-1.f,H*0.5f-7.f,2.f,5.f);
	DrawRect(Teal,W*0.5f-1.f,H*0.5f+3.f,2.f,5.f);
	if (Director)
	{
		DrawMinimap(W-230.f,H-230.f,200.f);
		FString Stars;
		for (int32 I=0; I<Director->GetWantedLevel(); ++I) Stars.AppendChar(static_cast<TCHAR>(0x2605));
		if (Director->GetWantedLevel()>0)
			DrawText(FString::Printf(TEXT("WANTED  %s  %s"),*Stars,Director->IsSearching()?TEXT("SEARCH"):TEXT("PURSUIT")),Coral,W-360.f,28.f,Font,1.3f);
		DrawText(FString::Printf(TEXT("%02d:%02d  %s"),FMath::FloorToInt(Director->GetTimeHours()),FMath::FloorToInt(FMath::Frac(Director->GetTimeHours())*60.f),*Director->GetWeatherName()),TextColor,W-220.f,52.f,Font,1.f);
		const FString Service=Director->GetNearbyServiceName();
		if (!Service.IsEmpty()) DrawText(Service,Teal,W*0.5f-235.f,H-130.f,Font,1.1f);
		if (ASolVehicle* V=Player->GetCurrentVehicle())
			DrawText(FString::Printf(TEXT("%03d KM/H   VEH %d%%"),FMath::RoundToInt(V->GetSpeedKmh()),FMath::RoundToInt(100.f*V->VehicleHealth/FMath::Max(1.f,V->MaxVehicleHealth))),Teal,W*0.5f-100.f,H-76.f,Font,1.35f);
		if (Director->IsAdminOpen())
		{
			const float PanelW=430.f, PanelH=650.f, X=W*0.5f-PanelW*0.5f, Y=H*0.5f-PanelH*0.5f;
			DrawRect(FLinearColor(0.015f,0.035f,0.05f,0.95f),X,Y,PanelW,PanelH);
			DrawRect(Teal,X,Y,PanelW,4.f);
			DrawText(TEXT("HARBOR CITY  /  BENCHMARK"),Teal,X+20.f,Y+17.f,Font,1.25f);
			const FVector Pos=Player->GetActorLocation();
			DrawText(FString::Printf(TEXT("XYZ %.0f %.0f %.0f  /  NPC %d  CAR %d  COP %d"),Pos.X,Pos.Y,Pos.Z,Director->GetCivilianCount(),Director->GetTrafficCount(),Director->GetLivingPoliceCount()),TextColor,X+20.f,Y+42.f,Font,0.83f);
			DrawText(TEXT("UP/DOWN SELECT     ENTER APPLY     F1 CLOSE"),TextColor,X+20.f,Y+60.f,Font,0.8f);
			const TArray<FString>& Items=Director->GetAdminItems();
			const int32 Current=Director->GetAdminSelection();
			const int32 First=FMath::Clamp(Current-10,0,FMath::Max(0,Items.Num()-19));
			for (int32 I=First; I<FMath::Min(First+19,Items.Num()); ++I)
			{
				const float RowY=Y+90.f+(I-First)*28.f;
				if (I==Current) DrawRect(FLinearColor(0.1f,0.36f,0.37f,0.76f),X+12.f,RowY-3.f,PanelW-24.f,25.f);
				DrawText(Items[I],I==Current?Teal:TextColor,X+22.f,RowY,Font,1.f);
			}
		}
	}
	DrawText(TEXT("WASD MOVE  MOUSE LOOK  LMB FIRE  RMB AIM  F VEHICLE  R RELOAD  F1 SANDBOX"),TextColor,24.f,H-20.f,Font,0.78f);
}
