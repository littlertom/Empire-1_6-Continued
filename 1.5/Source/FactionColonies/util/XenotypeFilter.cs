using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies.util
{
    public class XenotypeFilter : IExposable
    {
        private FactionDef faction;
        private FactionFC factionFc;
        private MilitaryCustomizationUtil militaryUtil;
        private List<XenotypeDef> allowedXenotypes = new List<XenotypeDef>();
        private Dictionary<XenotypeDef, List<PawnKindDef>> securityGuardsByXenotype = new Dictionary<XenotypeDef, List<PawnKindDef>>();

        public IEnumerable<XenotypeDef> AllowedXenotypes => allowedXenotypes;
        public int AllowedXenotypeCount => allowedXenotypes.Count;

        public XenotypeFilter()
        {
        }

        public XenotypeFilter(FactionFC factionFc)
        {
            this.factionFc = factionFc;
            militaryUtil = factionFc.militaryCustomizationUtil;
            faction = DefDatabase<FactionDef>.GetNamed("PColony");
            InitializeWithAllXenotypes();
        }

        public void FinalizeInit(FactionFC factionFc)
        {
            this.factionFc = factionFc;
            militaryUtil = factionFc.militaryCustomizationUtil;
            faction = DefDatabase<FactionDef>.GetNamed("PColony");

            if (allowedXenotypes.Count == 0)
            {
                InitializeWithAllXenotypes();
            }

            RefreshPawnGroupMakers();
            WorldSettlementTraderTracker.reloadTraderKind();
        }

        private void InitializeWithAllXenotypes()
        {
            allowedXenotypes.Clear();
            securityGuardsByXenotype.Clear();

            // Add all available xenotypes by default
            foreach (XenotypeDef xenotype in DefDatabase<XenotypeDef>.AllDefsListForReading)
            {
                if (xenotype.IsXenotypeWithLabel() && xenotype != XenotypeDefOf.Baseliner)
                {
                    allowedXenotypes.Add(xenotype);
                    SetupSecurityGuards(xenotype);
                }
            }

            // Always include Baseliner as default
            if (!allowedXenotypes.Contains(XenotypeDefOf.Baseliner))
            {
                allowedXenotypes.Add(XenotypeDefOf.Baseliner);
                SetupSecurityGuards(XenotypeDefOf.Baseliner);
            }
        }

        private void SetupSecurityGuards(XenotypeDef xenotype)
        {
            if (!securityGuardsByXenotype.ContainsKey(xenotype))
            {
                securityGuardsByXenotype[xenotype] = new List<PawnKindDef>();
            }

            // Check if xenotype has violence disabled or low shooting skill
            bool needsSecurityGuards = XenotypeNeedsSecurityGuards(xenotype);
            
            if (needsSecurityGuards)
            {
                // Find suitable security guard animals
                var guardAnimals = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Where(def => def.race.race.Animal && 
                                def.RaceProps.trainability != null && 
                                def.RaceProps.trainability.intelligenceOrder >= TrainabilityDefOf.Intermediate.intelligenceOrder &&
                                def.race.race.predator &&
                                def.combatPower > 50f && // Only combat-capable animals
                                !def.race.tradeTags.NullOrEmpty() &&
                                !def.race.tradeTags.Contains("AnimalMonster") &&
                                !def.race.tradeTags.Contains("AnimalGenetic"))
                    .OrderByDescending(def => def.combatPower)
                    .Take(3); // Take top 3 guard animals

                securityGuardsByXenotype[xenotype].AddRange(guardAnimals);
            }
        }

        public bool XenotypeNeedsSecurityGuards(XenotypeDef xenotype)
        {
            if (xenotype?.genes == null) return false;

            // Simple heuristic: Check the xenotype name for known non-violent types
            string xenotypeName = xenotype.defName.ToLower();
            
            // Common non-violent or weak xenotypes that would benefit from security guards
            if (xenotypeName.Contains("pacifist") || 
                xenotypeName.Contains("gentle") ||
                xenotypeName.Contains("weak") ||
                xenotypeName.Contains("frail") ||
                xenotypeName.Contains("nearsighted") ||
                xenotypeName.Contains("peaceful"))
            {
                return true;
            }

            // Check for genes that explicitly reduce combat effectiveness
            foreach (var gene in xenotype.genes)
            {
                if (gene.statFactors != null)
                {
                    foreach (var statModifier in gene.statFactors)
                    {
                        // Check for severely reduced combat stats
                        if ((statModifier.stat == StatDefOf.ShootingAccuracyPawn || 
                             statModifier.stat == StatDefOf.MeleeHitChance ||
                             statModifier.stat == StatDefOf.MeleeDodgeChance) && 
                            statModifier.value < 0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            // For now, assume most xenotypes don't need security guards unless specifically flagged
            return false;
        }

        public bool Allows(XenotypeDef xenotype)
        {
            return allowedXenotypes.Contains(xenotype);
        }

        public bool SetAllow(XenotypeDef xenotype, bool allow)
        {
            if (allow && !allowedXenotypes.Contains(xenotype))
            {
                allowedXenotypes.Add(xenotype);
                SetupSecurityGuards(xenotype);
                RefreshPawnGroupMakers();
                return true;
            }
            else if (!allow && allowedXenotypes.Contains(xenotype))
            {
                // Don't allow removing the last xenotype
                if (allowedXenotypes.Count <= 1)
                {
                    return false;
                }

                allowedXenotypes.Remove(xenotype);
                securityGuardsByXenotype.Remove(xenotype);
                RefreshPawnGroupMakers();
                return true;
            }

            return false;
        }

        public void ResetToAllXenotypes()
        {
            InitializeWithAllXenotypes();
            RefreshPawnGroupMakers();
        }

        /// <summary>
        /// Get all available security guard animal kinds from all xenotypes
        /// </summary>
        public List<PawnKindDef> GetAvailableSecurityGuards()
        {
            var allGuards = new List<PawnKindDef>();
            foreach (var guardList in securityGuardsByXenotype.Values)
            {
                allGuards.AddRange(guardList);
            }
            return allGuards.Distinct().ToList();
        }

        private void RefreshPawnGroupMakers()
        {
            if (faction == null || factionFc == null) return;

            // Clear existing pawn group makers
            faction.pawnGroupMakers = new List<PawnGroupMaker>
            {
                new PawnGroupMaker { kindDef = PawnGroupKindDefOf.Combat },
                new PawnGroupMaker { kindDef = PawnGroupKindDefOf.Trader },
                new PawnGroupMaker { kindDef = PawnGroupKindDefOf.Settlement },
                new PawnGroupMaker { kindDef = PawnGroupKindDefOf.Peaceful }
            };

            // Generate pawn options for each allowed xenotype
            foreach (var xenotype in allowedXenotypes)
            {
                var humanPawns = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Where(def => def.race == ThingDefOf.Human && 
                                def.defaultFactionDef != null && 
                                def.defaultFactionDef.techLevel <= factionFc.techLevel);

                foreach (var pawnKind in humanPawns)
                {
                    var pawnOption = new PawnGenOption 
                    { 
                        kind = pawnKind, 
                        selectionWeight = 1 
                    };

                    // Add to all relevant pawn group makers
                    faction.pawnGroupMakers[2].options.Add(pawnOption); // Settlement
                    
                    if (pawnKind.label != "mercenary")
                    {
                        faction.pawnGroupMakers[1].options.Add(pawnOption); // Trader
                        faction.pawnGroupMakers[3].options.Add(pawnOption); // Peaceful
                    }

                    if (pawnKind.isFighter)
                    {
                        faction.pawnGroupMakers[0].options.Add(pawnOption); // Combat
                        faction.pawnGroupMakers[1].guards.Add(pawnOption); // Trader guards
                    }

                    if (pawnKind.trader)
                    {
                        faction.pawnGroupMakers[1].traders.Add(pawnOption);
                    }
                }

                // Add security guards for xenotypes that need them
                if (securityGuardsByXenotype.ContainsKey(xenotype) && securityGuardsByXenotype[xenotype].Any())
                {
                    foreach (var guardAnimal in securityGuardsByXenotype[xenotype])
                    {
                        var guardOption = new PawnGenOption 
                        { 
                            kind = guardAnimal, 
                            selectionWeight = 1 
                        };

                        faction.pawnGroupMakers[0].options.Add(guardOption); // Combat
                        faction.pawnGroupMakers[1].guards.Add(guardOption); // Trader guards
                    }
                }
            }

            // Add pack animals for caravans
            foreach (PawnKindDef animalKindDef in DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(kind => kind.RaceProps.packAnimal))
            {
                faction.pawnGroupMakers[1].carriers.Add(new PawnGenOption { kind = animalKindDef, selectionWeight = 1 });
            }

            RefreshMercenaryPawnGenOptions();
        }

        private void RefreshMercenaryPawnGenOptions()
        {
            if (militaryUtil?.mercenarySquads == null) return;

            foreach (MercenarySquadFC mercenarySquadFc in militaryUtil.mercenarySquads)
            {
                List<Mercenary> newMercs = new List<Mercenary>();
                foreach (Mercenary mercenary in mercenarySquadFc.mercenaries)
                {
                    // For now, keep existing mercenaries but could be updated to use xenotype system
                    newMercs.Add(mercenary);
                }
                mercenarySquadFc.mercenaries = newMercs;
            }
        }

        public List<PawnKindDef> GetSecurityGuardsForXenotype(XenotypeDef xenotype)
        {
            return securityGuardsByXenotype.ContainsKey(xenotype) 
                ? securityGuardsByXenotype[xenotype] 
                : new List<PawnKindDef>();
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref allowedXenotypes, "allowedXenotypes", LookMode.Def);
            Scribe_Collections.Look(ref securityGuardsByXenotype, "securityGuardsByXenotype", LookMode.Def, LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (allowedXenotypes == null)
                {
                    allowedXenotypes = new List<XenotypeDef>();
                }
                if (securityGuardsByXenotype == null)
                {
                    securityGuardsByXenotype = new Dictionary<XenotypeDef, List<PawnKindDef>>();
                }
            }
        }
    }
}
