#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "World/SolNPC.h"
#include "SolWorldDirector.generated.h"

class ASolPlayerCharacter;
class ASolVehicle;
class UAudioComponent;

UENUM(BlueprintType)
enum class ESolWeather : uint8 { Clear, Cloudy, Rain, Fog, Storm };

UCLASS()
class UNREAL_GTA_GPT6SOL_API ASolWorldDirector : public AActor
{
	GENERATED_BODY()
public:
	ASolWorldDirector();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;
	static ASolWorldDirector* Find(const UObject* Context);

	UFUNCTION(BlueprintCallable, Category="Wanted") void AddCrime(float Severity, FVector Location);
	UFUNCTION(BlueprintCallable, Category="Wanted") void ClearWanted();
	UFUNCTION(BlueprintPure, Category="Wanted") int32 GetWantedLevel() const { return WantedLevel; }
	UFUNCTION(BlueprintPure, Category="Wanted") bool IsSearching() const { return WantedLevel > 0 && !bPoliceHaveSight; }
	UFUNCTION(BlueprintPure, Category="Wanted") FVector GetLastKnownPosition() const { return LastKnownPosition; }
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SetWantedLevel(int32 Level);
	UFUNCTION(BlueprintCallable, Category="Sandbox") void ToggleAdmin();
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SetWeather(ESolWeather NewWeather);
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SetTime(float NewHours);
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SpawnVehicle(int32 TypeIndex, bool bPolice = false);
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SpawnPolice(int32 Count = 1);
	UFUNCTION(BlueprintCallable, Category="Sandbox") void SaveSandbox();
	UFUNCTION(BlueprintCallable, Category="Sandbox") void LoadSandbox();
	bool IsAdminOpen() const { return bAdminOpen; }
	int32 GetAdminSelection() const { return AdminSelection; }
	const TArray<FString>& GetAdminItems() const { return AdminItems; }
	FString GetWeatherName() const;
	float GetTimeHours() const { return TimeHours; }
	int32 GetLivingPoliceCount() const;
	int32 GetCivilianCount() const;
	int32 GetTrafficCount() const;
	bool IsTrafficEnabled() const { return bTrafficEnabled; }
	bool ArePedestriansEnabled() const { return bPedestriansEnabled; }
	bool IsGodModeEnabled() const { return bGodMode; }
	FString GetNearbyServiceName() const;

private:
	UPROPERTY(VisibleAnywhere, Category="Wanted") int32 WantedLevel = 0;
	UPROPERTY(VisibleAnywhere, Category="Wanted") float WantedPressure = 0.f;
	UPROPERTY(VisibleAnywhere, Category="Wanted") bool bPoliceHaveSight = false;
	UPROPERTY(VisibleAnywhere, Category="Wanted") FVector LastKnownPosition = FVector::ZeroVector;
	UPROPERTY(EditAnywhere, Category="World") float TimeHours = 14.f;
	UPROPERTY(EditAnywhere, Category="World") ESolWeather Weather = ESolWeather::Clear;
	UPROPERTY(EditAnywhere, Category="World") bool bTrafficEnabled = true;
	UPROPERTY(EditAnywhere, Category="World") bool bPedestriansEnabled = true;
	UPROPERTY(EditAnywhere, Category="World") bool bGodMode = false;
	bool bAdminOpen = false;
	bool bHasStoredVehicle = false;
	uint8 StoredVehicleType = 0;
	uint8 StoredCarVariant = 0;
	int32 StoredPaintPreset = 0;
	int32 StoredEngineUpgrade = 0;
	int32 AdminSelection = 0;
	float SightLostAt = 0.f;
	float LastWantedDecayAt = 0.f;
	float NextPopulationUpdateAt = 0.f;
	float NextPoliceSpawnAt = 0.f;
	float NextWeatherUpdateAt = 0.f;
	TArray<FString> AdminItems;
	TArray<TWeakObjectPtr<ASolNPC>> NPCs;
	TArray<TWeakObjectPtr<ASolVehicle>> Vehicles;
	TArray<TArray<FVector>> TrafficRoutes;
	UPROPERTY(VisibleAnywhere) TObjectPtr<UAudioComponent> AmbientAudio;
	UPROPERTY(VisibleAnywhere) TObjectPtr<UAudioComponent> RainAudio;
	void InitializeTrafficRoutes();
	void UpdateWanted(float DeltaSeconds);
	void UpdatePopulation();
	void UpdateEnvironment(float DeltaSeconds);
	void HandleAdminInput();
	void HandleServiceInput();
	void RunAdminAction(int32 Index);
	ASolPlayerCharacter* GetPlayer() const;
	ASolNPC* SpawnNPC(ESolNPCRole SpawnRole, FVector Location);
	void SpawnTrafficCar();
};
