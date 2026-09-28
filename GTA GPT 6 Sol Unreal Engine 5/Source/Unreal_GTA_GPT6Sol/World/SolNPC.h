#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "SolNPC.generated.h"

class UStaticMeshComponent;

UENUM(BlueprintType)
enum class ESolNPCRole : uint8
{
	Civilian,
	Police,
	Tactical,
	Shopkeeper
};

UCLASS()
class UNREAL_GTA_GPT6SOL_API ASolNPC : public ACharacter
{
	GENERATED_BODY()

public:
	ASolNPC();
	virtual void BeginPlay() override;
	virtual void Tick(float DeltaSeconds) override;
	virtual float TakeDamage(float DamageAmount, struct FDamageEvent const& DamageEvent, class AController* EventInstigator, AActor* DamageCauser) override;

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="NPC") ESolNPCRole NPCRole = ESolNPCRole::Civilian;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="NPC") float Health = 100.f;
	UPROPERTY(BlueprintReadOnly, Category="NPC") bool bDead = false;
	UPROPERTY(BlueprintReadOnly, Category="NPC") bool bPanicked = false;

	void SetRole(ESolNPCRole NewRole);
	void Panic(const FVector& ThreatLocation);
	bool CanSeeActor(const AActor* Target) const;

private:
	UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Visual;
	FVector Destination = FVector::ZeroVector;
	FVector Threat = FVector::ZeroVector;
	float DecisionTime = 0.f;
	float AttackTime = 0.f;
	float PanicEndTime = 0.f;
	void ChooseDestination();
	void UpdateVisual();
};
