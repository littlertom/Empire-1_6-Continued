using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionColonies.util;
using System.Collections.Generic;

namespace FactionColonies
{
    public class CreateColonyWindowFc : Window
    {
        public sealed override Vector2 InitialSize => new Vector2(300f, 600f);

        public int currentTileSelected = -1;
        public BiomeResourceDef currentBiomeSelected; //DefDatabase<BiomeResourceDef>.GetNamed(this.biome)
        public BiomeResourceDef currentHillinessSelected;
        public bool traitExpansionistReducedFee;
        public int timeToTravel = -1;

        private int settlementCreationCost = 0;
        private readonly FactionFC faction = null;

        private int SettlementCreationBaseCost => (int)(TraitUtilsFC.cycleTraits("createSettlementMultiplier", faction.traits, Operation.Multiplication) * (FactionColonies.silverToCreateSettlement + (500 * (faction.settlements.Count() + faction.settlementCaravansList.Count())) + (TraitUtilsFC.cycleTraits("createSettlementBaseCost", faction.traits, Operation.Addition))));

        public CreateColonyWindowFc()
        {
            forcePause = false;
            draggable = false;
            preventCameraMotion = false;
            doCloseX = true;
            windowRect = new Rect(UI.screenWidth - InitialSize.x, (UI.screenHeight - InitialSize.y) / 2f - (UI.screenHeight/8f), InitialSize.x, InitialSize.y);
            faction = Find.World.GetComponent<FactionFC>();
        }



        //Pre-Opening
        public override void PreOpen()
        {

        }

        //Drawing
        public override void DoWindowContents(Rect inRect)
        {
            faction.roadBuilder.DrawPaths();

            GetTileData();

            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            CalculateSettlementCreationCost();
            
            //Draw Label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, 268, 40), "SettleANewColony".Translate());

            //hori line
            Widgets.DrawLineHorizontal(0, 40, 300);


            //Upper menu
            Widgets.DrawMenuSection(new Rect(5, 45, 258, 220));

            DrawLabelBox(new Rect(10, 50, 100, 100), "TravelTime".Translate(), timeToTravel.ToTimeString());
            DrawLabelBox(new Rect(153, 50, 100, 100), "InitialCost".Translate(), settlementCreationCost + " " + "Silver".Translate());


            //Lower Menu label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 270, 268, 40), "BaseProductionStats".Translate());


            //Lower menu
            Widgets.DrawMenuSection(new Rect(5, 310, 258, 220));


            //Draw production
            DrawProduction();
            DrawCreateSettlementButton();

            // Orbital Platform button (only show if research is complete)
            if (CanCreateOrbitalPlatforms())
            {
                int orbitalBtnLength = 140;
                if (Widgets.ButtonText(
                        new Rect((InitialSize.x - 32 - orbitalBtnLength) / 2f, 535 - 38f, orbitalBtnLength, 32),
                        "Create Orbital Platform"))
                {
                    Find.WindowStack.Add(new OrbitalPlatformCreationWindow());
                }
            }

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void SpawnOrbitalPlatformAboveSelectedTile()
        {
            // Resolve a tile even if none is selected
            int targetTile = ResolveTargetTile();
            currentTileSelected = targetTile; // keep your UI in sync

            // Use our custom orbital platform definition
            var orbitalWorldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail("FCOrbitalPlatform");

            if (orbitalWorldObjectDef == null)
            {
                Log.Warning("FCOrbitalPlatform WorldObjectDef not found. This mod may not be properly installed.");
                Messages.Message("Could not find orbital platform definition. Check mod installation.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Log.Message($"=== USING CUSTOM ORBITAL PLATFORM DEF ===");
            Log.Message($"Using def: {orbitalWorldObjectDef.defName}");
            Log.Message($"ExpandingIconTexture: {orbitalWorldObjectDef.expandingIconTexture}");

            // DEBUG: Check if the texture actually loads
            Log.Message($"=== TEXTURE DEBUG ===");
            var testTexture = ContentFinder<Texture2D>.Get("World/WorldObjects/Expanding/SettlementPlatform", false);
            Log.Message($"Can load SettlementPlatform texture: {testTexture != null}");

            if (testTexture != null)
            {
                Log.Message($"SettlementPlatform texture name: {testTexture.name}");
            }
            else
            {
                Log.Warning("SettlementPlatform texture not found! Checking available textures...");
                
                // Test other known expanding textures
                var availableTextures = new string[]
                {
                    "World/WorldObjects/Expanding/Settlement",
                    "World/WorldObjects/Expanding/Site", 
                    "World/WorldObjects/Expanding/Caravan",
                    "World/WorldObjects/Expanding/AsteroidMine"
                };
                
                foreach (var texPath in availableTextures)
                {
                    var tex = ContentFinder<Texture2D>.Get(texPath, false);
                    Log.Message($"  {texPath}: {tex != null}");
                }
            }

            // DEBUG: Check the def properties
            Log.Message($"ExpandingIcon enabled: {orbitalWorldObjectDef.expandingIcon}");
            Log.Message($"UseDynamicDrawer: {orbitalWorldObjectDef.useDynamicDrawer}");
            Log.Message($"ExpandingIconDrawSize: {orbitalWorldObjectDef.expandingIconDrawSize}");
            Log.Message($"FullyExpandedInSpace: {orbitalWorldObjectDef.fullyExpandedInSpace}");

            // Find an empty orbital tile for the platform
            PlanetTile orbitalTile = FindEmptyOrbitalTile();

            if (!orbitalTile.Valid)
            {
                Messages.Message("Could not find suitable empty space for orbital platform.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create the orbital platform using our custom def
            MapParent orbitalPlatform = (MapParent)WorldObjectMaker.MakeWorldObject(orbitalWorldObjectDef);
            orbitalPlatform.Tile = orbitalTile;  // Use orbital tile, not target tile
            orbitalPlatform.SetFaction(Faction.OfPlayer);

            // Set the name
            try
            {
                int platformCount = Find.WorldObjects.AllWorldObjects.Count(wo => wo.Label?.Contains("Orbital Platform") == true) + 1;
                string platformName = $"Orbital Platform {platformCount}";
                
                var labelField = orbitalPlatform.GetType().GetField("labelInt", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (labelField != null)
                {
                    labelField.SetValue(orbitalPlatform, platformName);
                    Log.Message($"Set platform name to: {platformName}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"Could not set orbital platform name: {ex.Message}");
            }

            // Add it to the world
            Find.WorldObjects.Add(orbitalPlatform);

            // DON'T generate a map - just show success message
            string layerName = Find.WorldGrid[targetTile].Layer?.Def?.label ?? "space";
            Messages.Message($"Orbital platform created in {layerName}. Click on it to visit when needed.", 
                MessageTypeDefOf.PositiveEvent, false);

            // Optional: Switch world view to show the new platform
            if (Find.WorldSelector != null)
            {
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.Select(orbitalPlatform);
            }

            Log.Message($"=== ORBITAL PLATFORM CREATION COMPLETE ===");
        }

        private int ResolveTargetTile()
        {
            // If you already have a selection, use it
            if (currentTileSelected >= 0)
                return currentTileSelected;

            // Prefer the player’s current map tile (if in a map)
            var map = Find.CurrentMap;
            if (map != null)
                return map.Tile;

            // Fallback: your faction capital (if set)
            if (faction != null && faction.capitalLocation >= 0)
                return faction.capitalLocation;

            // Fallback: any player home map
            var home = Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
            if (home != null)
                return home.Tile;

            // Last resort: pick a random valid settlement tile
            if (TileFinder.TryFindNewSiteTile(out PlanetTile randomTile))
                return randomTile;

            // Ultra fallback: tile 0 (should be safe in most seeds)
            return 0;
        }


        private void GetTileData()
        {
            var selectedTile = Find.WorldSelector.SelectedTile;
            if (selectedTile.Valid && selectedTile.tileId != currentTileSelected)
            {
                currentTileSelected = selectedTile.tileId;
                currentBiomeSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid[currentTileSelected].PrimaryBiome.defName, false);
                //default biome
                if (currentBiomeSelected == default(BiomeResourceDef))
                {
                    //Log Modded Biome
                    currentBiomeSelected = BiomeResourceDefOf.defaultBiome;
                }
                currentHillinessSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid[currentTileSelected].hilliness.ToString());
                if (currentBiomeSelected.canSettle && currentHillinessSelected.canSettle && currentTileSelected != 1)
                {
                    timeToTravel = FactionColonies.ReturnTicksToArrive(faction.capitalLocation, currentTileSelected);
                }
                else
                {
                    timeToTravel = 0;
                }
            }
        }

        private void CalculateSettlementCreationCost()
        {
            settlementCreationCost = SettlementCreationBaseCost;

            if (faction.hasPolicy(FCPolicyDefOf.isolationist)) settlementCreationCost *= 2;

            if (!faction.hasPolicy(FCPolicyDefOf.expansionist)) return;

            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
            {
                traitExpansionistReducedFee = false;
                settlementCreationCost = 0;
                return;
            }

            if (faction.traitExpansionistTickLastUsedSettlementFeeReduction == -1 || (faction.traitExpansionistBoolCanUseSettlementFeeReduction))
            {
                traitExpansionistReducedFee = true;
                settlementCreationCost /= 2;
                return;
            }

            traitExpansionistReducedFee = false;
        }
        
        private void DrawProduction()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //Production headers
            Widgets.Label(new Rect(40, 310, 60, 25), "Base".Translate());
            Widgets.Label(new Rect(110, 310, 60, 25), "Modifier".Translate());
            Widgets.Label(new Rect(180, 310, 60, 25), "Final".Translate());

            if (currentTileSelected != -1)
            {
                foreach (ResourceType titheType in ResourceUtils.resourceTypes)
                {
                    int titheTypeInt = (int)titheType;
                    int baseHeight = 15;
                    if (Widgets.ButtonImage(new Rect(20, 335 + titheTypeInt * (5 + baseHeight), baseHeight, baseHeight), faction.returnResource(titheType).getIcon()))
                    {
                        string label = faction.returnResource(titheType).label;

                        Find.WindowStack.Add(new DescWindowFc("SettlementProductionOf".Translate() + ": " + label, label.CapitalizeFirst()));
                    }

                    float xMod = 70f;
                    Rect baseRect = new Rect(40, 335 + titheTypeInt * (5 + baseHeight), 60, baseHeight + 2);

                    double titheAddBaseProductionCurBiome = currentBiomeSelected.BaseProductionAdditive[titheTypeInt];
                    double titheAddBaseProductionCurHilli = currentHillinessSelected.BaseProductionAdditive[titheTypeInt];

                    double titheMultBaseProductionCurBiome = currentBiomeSelected.BaseProductionMultiplicative[titheTypeInt];
                    double titheMultBaseProductionCurHilli = currentHillinessSelected.BaseProductionMultiplicative[titheTypeInt];

                    Widgets.Label(baseRect, (titheAddBaseProductionCurBiome + titheAddBaseProductionCurHilli).ToString());
                    Widgets.Label(baseRect.CopyAndShift(xMod, 0f), (titheMultBaseProductionCurBiome * titheMultBaseProductionCurHilli).ToString());
                    Widgets.Label(baseRect.CopyAndShift(xMod * 2f, 0f), ((titheAddBaseProductionCurBiome + titheAddBaseProductionCurHilli) * (titheMultBaseProductionCurBiome * titheMultBaseProductionCurHilli)).ToString());
                }
            }
        }

        private void DrawCreateSettlementButton()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            int buttonLength = 130;
            if (Widgets.ButtonText(new Rect((InitialSize.x - 32 - buttonLength) / 2f, 535, buttonLength, 32), "Settle".Translate() + ": (" + settlementCreationCost + ")")) //add inital cost
            {
                if (!CanCreateSettlementHere()) return;

                PaymentUtil.paySilver(settlementCreationCost);

                //create settle event
                FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.settleNewColony);
                evt.location = currentTileSelected;
                evt.planetName = Find.World.info.name;
                evt.timeTillTrigger = Find.TickManager.TicksGame + timeToTravel;
                evt.source = faction.capitalLocation;
                faction.addEvent(evt);

                faction.settlementCaravansList.Add(evt.location.ToString());
                Messages.Message("CaravanSentToLocation".Translate() + " " + (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString() + "!", MessageTypeDefOf.PositiveEvent);

                DoPostEventCreationTraitThings();
            }
        }

        private bool CanCreateSettlementHere()
        {
            StringBuilder reason = new StringBuilder();
            if (!WorldTileChecker.IsValidTileForNewSettlement(currentTileSelected, reason) || faction.checkSettlementCaravansList(currentTileSelected.ToString()) || !PlayerHasEnoughSilver(reason))
            {
                Messages.Message(reason.ToString(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        private void DoPostEventCreationTraitThings()
        {
            if (traitExpansionistReducedFee)
            {
                faction.traitExpansionistTickLastUsedSettlementFeeReduction = Find.TickManager.TicksGame;
                faction.traitExpansionistBoolCanUseSettlementFeeReduction = false;
            }
        }

        private bool PlayerHasEnoughSilver(StringBuilder reason)
        {
            if (PaymentUtil.getSilver() >= settlementCreationCost) return true;

            reason?.Append("NotEnoughSilverToSettle".Translate() + "!");
            return false;
        }

        public void DrawLabelBox(Rect rect, string text1, string text2)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            //Draw highlight
            Widgets.DrawHighlight(new Rect(rect.x, rect.y + rect.height /8, rect.width, rect.height / 4f));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, rect.height / 2f), text1);

            //divider
            Widgets.DrawLineHorizontal(rect.x + 5, rect.y + rect.height / 2, rect.width - 10);

            //Bottom Text - Gamers Rise Up
            Widgets.Label(new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2f), text2);
        }

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

        private bool CanCreateOrbitalPlatforms()
        {
            var research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("OrbitalConstruction");
            return research != null && research.IsFinished;
        }

        private OrbitalPlatformTier GetHighestOrbitalTier()
        {
            var tiers = new System.Collections.Generic.List<OrbitalPlatformTier>();
            if (DefDatabase<ResearchProjectDef>.GetNamed("ZeroGManufacturing").IsFinished)
                tiers.Add(OrbitalPlatformTier.ZeroG);
            if (DefDatabase<ResearchProjectDef>.GetNamed("AdvancedOrbitalEngineering").IsFinished)
                tiers.Add(OrbitalPlatformTier.Advanced);
            if (DefDatabase<ResearchProjectDef>.GetNamed("OrbitalLogistics").IsFinished)
                return OrbitalPlatformTier.Logistics;
            if (DefDatabase<ResearchProjectDef>.GetNamed("OrbitalConstruction").IsFinished)
                tiers.Add(OrbitalPlatformTier.Basic);
            return OrbitalPlatformTier.None;
        }

        public enum OrbitalPlatformTier
        {
            None,
            Basic,
            Logistics,
            Advanced,
            ZeroG
        }
    }
}
