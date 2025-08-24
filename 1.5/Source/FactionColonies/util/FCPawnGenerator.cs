using RimWorld;
using System.Reflection.Emit;
using Verse;

namespace FactionColonies.util
{
	
	class FCPawnGenerator
	{
		public Pawn defaultPawn;
		public XenotypeDef xenotype;

		public static PawnGenerationRequest WorkerOrMilitaryRequest(PawnKindDef pawnKindDef = null, XenotypeDef xenotypeDef = null)
        {
			var kindDef = pawnKindDef ?? FactionColonies.getPlayerColonyFaction()?.RandomPawnKind() ?? DefDatabase<FactionDef>.GetNamed("PColony").pawnGroupMakers.RandomElement().options.RandomElement().kind;
			var faction = FactionColonies.getPlayerColonyFaction();
			
			return new PawnGenerationRequest(
				kind: kindDef,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: true,
				colonistRelationChanceFactor: 0,
				forceAddFreeWarmLayerIfNeeded: false,
				allowGay: true,
				allowFood: true,
				allowAddictions: false,
				inhabitant: false,
				certainlyBeenInCryptosleep: false,
				forceRedressWorldPawnIfFormerColonist: false,
				worldPawnFactionDoesntMatter: false,
				biocodeWeaponChance: 0,
				extraPawnForExtraRelationChance: null,
				relationWithExtraPawnChanceFactor: 0,
				validatorPreGear: null,
				validatorPostGear: null,
				forcedTraits: null,
				prohibitedTraits: null,
				forcedXenotype: xenotypeDef,
				fixedBiologicalAge: kindDef.GetReasonableMercenaryAge()
			);
		}

		public static PawnGenerationRequest CivilianRequest(PawnKindDef pawnKindDef = null)
		{
			var kindDef = pawnKindDef ?? (FactionColonies.getPlayerColonyFaction()?.RandomPawnKind());
			var faction = FactionColonies.getPlayerColonyFaction();
			
			return new PawnGenerationRequest(
				kind: kindDef,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: false,
				colonistRelationChanceFactor: 0,
				forceAddFreeWarmLayerIfNeeded: false,
				allowGay: true,
				allowFood: true,
				allowAddictions: false,
				inhabitant: false,
				certainlyBeenInCryptosleep: false,
				forceRedressWorldPawnIfFormerColonist: false,
				worldPawnFactionDoesntMatter: false,
				biocodeWeaponChance: 0,
				extraPawnForExtraRelationChance: null,
				relationWithExtraPawnChanceFactor: 0,
				validatorPreGear: null,
				validatorPostGear: null,
				forcedTraits: null,
				prohibitedTraits: null,
				fixedBiologicalAge: kindDef.GetReasonableMercenaryAge()
			);
		}

		public static PawnGenerationRequest AnimalRequest(PawnKindDef race)
		{
			var faction = FactionColonies.getPlayerColonyFaction();
			
			return new PawnGenerationRequest(
				kind: race,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: false,
				colonistRelationChanceFactor: 0,
				forceAddFreeWarmLayerIfNeeded: false,
				allowGay: true,
				allowFood: true,
				allowAddictions: false,
				inhabitant: false,
				certainlyBeenInCryptosleep: false,
				forceRedressWorldPawnIfFormerColonist: false,
				worldPawnFactionDoesntMatter: false,
				biocodeWeaponChance: 0,
				extraPawnForExtraRelationChance: null,
				relationWithExtraPawnChanceFactor: 0,
				validatorPreGear: null,
				validatorPostGear: null,
				forcedTraits: null,
				prohibitedTraits: null,
				fixedBiologicalAge: race.GetReasonableMercenaryAge()
			);
		}
	}
}

