using System;
using RimWorld;
using System.Linq;
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
			var kindDef = pawnKindDef;
			if (kindDef == null)
			{
				try
				{
					kindDef = FactionColonies.getPlayerColonyFaction()?.RandomPawnKind();
				}
				catch (Exception ex)
				{
					Log.Warning($"Empire: Failed to get pawn kind from player faction: {ex.Message}");
				}
				
				// Fallback to default colonist if still null
				if (kindDef == null)
				{
					kindDef = PawnKindDefOf.Colonist;
				}
			}
			
			var faction = FactionColonies.getPlayerColonyFaction();
			if (faction == null)
			{
				faction = Faction.OfPlayer; // Fallback to player faction
			}
			
			var factionFC = Find.World.GetComponent<FactionFC>();
			
			// If no specific xenotype is requested, select from allowed xenotypes
			if (xenotypeDef == null)
			{
				if (factionFC?.xenotypeFilter != null && factionFC.xenotypeFilter.AllowedXenotypes.Any())
				{
					xenotypeDef = factionFC.xenotypeFilter.AllowedXenotypes.RandomElement();
				}
				else
				{
					xenotypeDef = XenotypeDefOf.Baseliner; // Fallback to default
				}
			}
			
			// Check if the xenotype needs security guards (is non-violent)
			bool needsSecurityGuards = factionFC?.xenotypeFilter?.XenotypeNeedsSecurityGuards(xenotypeDef) ?? false;
			
			// Get a safe age value
			float? fixedAge = null;
			try
			{
				fixedAge = kindDef?.GetReasonableMercenaryAge();
			}
			catch (Exception ex)
			{
				Log.Warning($"Empire: Failed to get reasonable age for {kindDef?.defName}: {ex.Message}");
				fixedAge = null; // Let the game decide the age
			}
			
			return new PawnGenerationRequest(
				kind: kindDef,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: !needsSecurityGuards, // Allow non-violent pawns if they have security guards
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
				fixedBiologicalAge: fixedAge
			);
		}

		public static PawnGenerationRequest CivilianRequest(PawnKindDef pawnKindDef = null, XenotypeDef xenotypeDef = null)
		{
			var kindDef = pawnKindDef;
			if (kindDef == null)
			{
				try
				{
					kindDef = FactionColonies.getPlayerColonyFaction()?.RandomPawnKind();
				}
				catch (Exception ex)
				{
					Log.Warning($"Empire: Failed to get pawn kind from player faction: {ex.Message}");
				}
				
				// Fallback to default colonist if still null
				if (kindDef == null)
				{
					kindDef = PawnKindDefOf.Colonist;
				}
			}
			
			var faction = FactionColonies.getPlayerColonyFaction();
			if (faction == null)
			{
				faction = Faction.OfPlayer; // Fallback to player faction
			}
			
			var factionFC = Find.World.GetComponent<FactionFC>();
			
			// If no specific xenotype is requested, select from allowed xenotypes
			if (xenotypeDef == null)
			{
				if (factionFC?.xenotypeFilter != null && factionFC.xenotypeFilter.AllowedXenotypes.Any())
				{
					xenotypeDef = factionFC.xenotypeFilter.AllowedXenotypes.RandomElement();
				}
				else
				{
					xenotypeDef = XenotypeDefOf.Baseliner; // Fallback to default
				}
			}
			
			// Check if the xenotype needs security guards (is non-violent)
			bool needsSecurityGuards = factionFC?.xenotypeFilter?.XenotypeNeedsSecurityGuards(xenotypeDef) ?? false;
			
			// Get a safe age value
			float? fixedAge = null;
			try
			{
				fixedAge = kindDef?.GetReasonableMercenaryAge();
			}
			catch (Exception ex)
			{
				Log.Warning($"Empire: Failed to get reasonable age for {kindDef?.defName}: {ex.Message}");
				fixedAge = null; // Let the game decide the age
			}
			
			return new PawnGenerationRequest(
				kind: kindDef,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: !needsSecurityGuards, // Allow non-violent pawns if they have security guards
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
				fixedBiologicalAge: fixedAge,
				fixedChronologicalAge: fixedAge
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

		/// <summary>
		/// Generate a simple delivery pawn that bypasses xenotype filtering issues
		/// </summary>
		public static PawnGenerationRequest SimpleDeliveryRequest()
		{
			var faction = FactionColonies.getPlayerColonyFaction();
			if (faction == null)
			{
				faction = Faction.OfPlayer; // Fallback to player faction
			}
			
			return new PawnGenerationRequest(
				kind: PawnKindDefOf.Colonist,
				faction: faction,
				context: PawnGenerationContext.NonPlayer,
				tile: -1,
				forceGenerateNewPawn: false,
				allowDead: false,
				allowDowned: false,
				canGeneratePawnRelations: true,
				mustBeCapableOfViolence: true, // Always capable of violence for delivery
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
				forcedXenotype: XenotypeDefOf.Baseliner // Force baseliner to avoid xenotype issues
			);
		}
	}
}

