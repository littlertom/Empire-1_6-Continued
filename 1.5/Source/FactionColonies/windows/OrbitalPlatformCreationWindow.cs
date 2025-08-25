using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace FactionColonies
{
    public enum OrbitalPlatformTier
    {
        None,
        Basic,
        Logistics,
        Advanced,
        ZeroG
    }

    public class OrbitalPlatformCreationWindow : Window
    {
        public sealed override Vector2 InitialSize => new Vector2(400f, 500f);

        private OrbitalPlatformTier selectedTier = OrbitalPlatformTier.Basic;
        private readonly FactionFC faction;

        public OrbitalPlatformCreationWindow()
        {
            forcePause = false;
            draggable = true;
            preventCameraMotion = false;
            doCloseX = true;
            faction = Find.World.GetComponent<FactionFC>();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, inRect.width, 40), "Create Orbital Platform");

            // Platform tier selection
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            float y = 60f;
            
            Widgets.Label(new Rect(20, y, 200, 25), "Platform Type:");
            y += 30f;

            var availableTiers = GetAvailableTiers();
            
            foreach (var tier in availableTiers)
            {
                var tierInfo = GetTierInfo(tier);
                bool isSelected = selectedTier == tier;
                
                if (Widgets.RadioButtonLabeled(new Rect(20, y, 300, 25), tierInfo.name, isSelected))
                {
                    selectedTier = tier;
                }
                y += 30f;
                
                // Show tier benefits
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(40, y, 320, 40), tierInfo.description);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += 45f;
            }

            // Cost display
            y += 20f;
            var cost = GetPlatformCost(selectedTier);
            Widgets.Label(new Rect(20, y, 200, 25), $"Cost: {cost} silver");
            y += 30f;

            // Create button
            if (Widgets.ButtonText(new Rect(20, y, 150, 35), "Create Platform"))
            {
                CreateOrbitalPlatform(selectedTier);
                Close();
            }

            // Cancel button  
            if (Widgets.ButtonText(new Rect(200, y, 150, 35), "Cancel"))
            {
                Close();
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private OrbitalPlatformTier[] GetAvailableTiers()
        {
            var tiers = new System.Collections.Generic.List<OrbitalPlatformTier>();
            
            if (DefDatabase<ResearchProjectDef>.GetNamed("OrbitalConstruction").IsFinished)
                tiers.Add(OrbitalPlatformTier.Basic);
            if (DefDatabase<ResearchProjectDef>.GetNamed("OrbitalLogistics").IsFinished)
                tiers.Add(OrbitalPlatformTier.Logistics);
            if (DefDatabase<ResearchProjectDef>.GetNamed("AdvancedOrbitalEngineering").IsFinished)
                tiers.Add(OrbitalPlatformTier.Advanced);
            if (DefDatabase<ResearchProjectDef>.GetNamed("ZeroGManufacturing").IsFinished)
                tiers.Add(OrbitalPlatformTier.ZeroG);
                
            return tiers.ToArray();
        }

        private (string name, string description) GetTierInfo(OrbitalPlatformTier tier)
        {
            switch (tier)
            {
                case OrbitalPlatformTier.Basic:
                    return ("Basic Platform", "Standard orbital construction capabilities");
                case OrbitalPlatformTier.Logistics:
                    return ("Logistics Platform", "50% faster tax delivery, special buildings");
                case OrbitalPlatformTier.Advanced:
                    return ("Advanced Platform", "25% lower construction cost, larger size");
                case OrbitalPlatformTier.ZeroG:
                    return ("Zero-G Manufacturing Platform", "Specialized production buildings with bonuses");
                default:
                    return ("Unknown", "");
            }
        }

        private int GetPlatformCost(OrbitalPlatformTier tier)
        {
            int baseCost = 5000;
            
            switch (tier)
            {
                case OrbitalPlatformTier.Basic:
                    return baseCost;
                case OrbitalPlatformTier.Logistics:
                    return baseCost + 2000;
                case OrbitalPlatformTier.Advanced:
                    return (int)(baseCost * 0.75f) + 3000; // 25% discount + premium
                case OrbitalPlatformTier.ZeroG:
                    return (int)(baseCost * 0.75f) + 5000;
                default:
                    return baseCost;
            }
        }

        private void CreateOrbitalPlatform(OrbitalPlatformTier tier)
        {
            // Check if player has enough silver
            int cost = GetPlatformCost(tier);
            if (PaymentUtil.getSilver() < cost)
            {
                Messages.Message("Not enough silver to create orbital platform!", MessageTypeDefOf.RejectInput);
                return;
            }

            // Pay the cost
            PaymentUtil.paySilver(cost);

            // Find an empty orbital tile
            PlanetTile orbitalTile = FindEmptyOrbitalTile();

            if (!orbitalTile.Valid)
            {
                Messages.Message("Could not find suitable empty space for orbital platform.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create orbital platform construction event (similar to regular settlement)
            CreateOrbitalPlatformEvent(tier, orbitalTile);
        }

        private void CreateOrbitalPlatformEvent(OrbitalPlatformTier tier, PlanetTile orbitalTile)
        {
            int constructionTime = GetConstructionTime(tier);
            
            FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.settleNewColony);
            evt.location = orbitalTile.tileId; // Use the tile ID, not the PlanetTile object
            evt.planetName = Find.World.info.name;
            evt.timeTillTrigger = Find.TickManager.TicksGame + constructionTime;
            evt.source = faction.capitalLocation;
            
            // Mark as orbital platform
            evt.isOrbitalPlatform = true;
            evt.orbitalTier = tier;
            evt.customDescription = $"Orbital Platform Construction ({GetTierInfo(tier).name})";
            
            faction.addEvent(evt);
            faction.settlementCaravansList.Add(orbitalTile.tileId.ToString()); // Use tile ID here too
            
            string tierName = GetTierInfo(tier).name;
            Messages.Message($"{tierName} construction initiated! Completion in {((float)constructionTime / GenDate.TicksPerDay).ToString("F1")} days", 
                MessageTypeDefOf.PositiveEvent);
        }

        private int GetConstructionTime(OrbitalPlatformTier tier)
        {
            // Base construction time (e.g., 10 days)
            int baseDays = 10;
            
            switch (tier)
            {
                case OrbitalPlatformTier.Basic:
                    return baseDays * GenDate.TicksPerDay;
                case OrbitalPlatformTier.Logistics:
                    return (baseDays + 5) * GenDate.TicksPerDay;
                case OrbitalPlatformTier.Advanced:
                    return (int)((baseDays + 8) * 0.75f * GenDate.TicksPerDay); // 25% faster due to research
                case OrbitalPlatformTier.ZeroG:
                    return (int)((baseDays + 12) * 0.75f * GenDate.TicksPerDay);
                default:
                    return baseDays * GenDate.TicksPerDay;
            }
        }

        // Copy the FindEmptyOrbitalTile method from CreateColonyWindowFC
        private PlanetTile FindEmptyOrbitalTile()
        {
            var worldGrid = Find.WorldGrid;
            var existingObjectTiles = Find.WorldObjects.AllWorldObjects.Select(wo => wo.Tile).ToHashSet();
            
            // If Odyssey is active and orbit layer exists, try to find a tile in orbit first
            if (ModsConfig.OdysseyActive && worldGrid.Orbit != null)
            {
                for (int attempts = 0; attempts < 1000; attempts++)
                {
                    int randomTileId = Rand.Range(0, worldGrid.Orbit.TilesCount);
                    PlanetTile orbitalTile = new PlanetTile(randomTileId, worldGrid.Orbit);
                    
                    if (!existingObjectTiles.Contains(orbitalTile))
                        return orbitalTile;
                }
            }
            
            // Fallback: find any empty surface tile
            for (int attempts = 0; attempts < 1000; attempts++)
            {
                int randomTileId = Rand.Range(0, worldGrid.TilesCount);
                PlanetTile surfaceTile = new PlanetTile(randomTileId, worldGrid.Surface);
                
                if (!existingObjectTiles.Contains(surfaceTile))
                    return surfaceTile;
            }
            
            return PlanetTile.Invalid;
        }
    }
}
