using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public enum BuildingFilter
    {
        All,
        Happiness,
        Food,
        Weapons,
        Apparel,
        Research,
        Medicine,
        Power,
        Basetax,
        Workers
    }

    class FCBuildingWindow : Window
    {
        readonly SettlementFC settlement;
        readonly int buildingSlot;
        readonly BuildingFCDef buildingDef;
        readonly List<BuildingFCDef> buildingList;
        readonly List<BuildingFCDef> filteredBuildingList;
        readonly FactionFC factionfc;

        private static readonly int offset = 8;
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly int rowHeight = 90;
        
        // Filter state
        private BuildingFilter currentFilter = BuildingFilter.All;
        private static readonly int filterButtonHeight = 25;
        private static readonly int filterRowHeight = 30;
        
        // Dynamic rectangles that will be calculated based on window size
        Rect TopWindow;
        Rect TopIcon;
        Rect TopName;
        Rect TopDescription;
        Rect FilterArea;

        // Window size settings - add these fields
        public float buildingWindowWidth = 450f;
        public float buildingWindowHeight = 600f;
        
        // Static variables to remember window size during play session
        private static Vector2 savedWindowSize = new Vector2(450f, 600f);
        private static bool hasSavedSize = false;
        
        public override Vector2 InitialSize => new Vector2(
            FactionColonies.Settings().buildingWindowWidth, 
            FactionColonies.Settings().buildingWindowHeight
        );
        
        // Override PreClose to save the current window size
        public override void PreClose()
        {
            base.PreClose();
            
            // Save the current window size to settings
            var settings = FactionColonies.Settings();
            settings.buildingWindowWidth = windowRect.width;
            settings.buildingWindowHeight = windowRect.height;
            
            // Write the settings to disk
            LoadedModManager.GetMod<FactionColoniesMod>().WriteSettings();
        }
        
        // Calculate dynamic layout based on current window size
        private void CalculateLayout(Rect inRect)
        {
            float topWindowHeight = Math.Max(120f, inRect.height * 0.2f); // 20% of window height, minimum 120px
            
            TopWindow = new Rect(0, 0, inRect.width, topWindowHeight);
            TopIcon = new Rect(15, topWindowHeight - 74, 64, 64);
            TopName = new Rect(15, 15, inRect.width - 30, 30);
            TopDescription = new Rect(95, topWindowHeight - 74, inRect.width - 110, 64);
            FilterArea = new Rect(5, topWindowHeight + 5, inRect.width - 10, filterRowHeight * 2 + 5);
        }

        private void DrawFilterButtons(Rect inRect)
        {
            // Define filter categories
            var filters = new[]
            {
                BuildingFilter.All,
                BuildingFilter.Happiness,
                BuildingFilter.Food,
                BuildingFilter.Weapons,
                BuildingFilter.Apparel,
                BuildingFilter.Research,
                BuildingFilter.Medicine,
                BuildingFilter.Power,
                BuildingFilter.Basetax,
                BuildingFilter.Workers
            };

            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Text.Font = GameFont.Tiny;

            // Calculate button dimensions
            int buttonsPerRow = 5;
            float buttonWidth = (FilterArea.width - 10) / buttonsPerRow;
            float buttonHeight = filterButtonHeight;

            for (int i = 0; i < filters.Length; i++)
            {
                int row = i / buttonsPerRow;
                int col = i % buttonsPerRow;
                
                Rect buttonRect = new Rect(
                    FilterArea.x + 5 + (col * buttonWidth),
                    FilterArea.y + 5 + (row * (buttonHeight + 5)),
                    buttonWidth - 5,
                    buttonHeight
                );

                bool isSelected = currentFilter == filters[i];
                
                // Draw button background
                if (isSelected)
                {
                    // Draw selected state with blue background
                    Widgets.DrawBoxSolid(buttonRect, new Color(0.2f, 0.5f, 0.8f, 0.8f));
                    Widgets.DrawBox(buttonRect, 1);
                }
                else
                {
                    // Draw normal button background
                    if (Widgets.ButtonInvisible(buttonRect))
                    {
                        currentFilter = filters[i];
                        ApplyFilter();
                    }
                    Widgets.DrawAtlas(buttonRect, Widgets.ButtonBGAtlas);
                }
                
                // Draw text manually with consistent centering
                Text.Anchor = TextAnchor.MiddleCenter;
                Color textColor = isSelected ? Color.white : Color.white;
                GUI.color = textColor;
                Widgets.Label(buttonRect, filters[i].ToString());
                GUI.color = Color.white;
                
                // Handle click for selected buttons
                if (isSelected && Widgets.ButtonInvisible(buttonRect))
                {
                    // Allow clicking selected button to deselect (go back to All)
                    currentFilter = BuildingFilter.All;
                    ApplyFilter();
                }
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void ApplyFilter()
        {
            filteredBuildingList.Clear();
            
            foreach (var building in buildingList)
            {
                if (ShouldShowBuilding(building))
                {
                    filteredBuildingList.Add(building);
                }
            }
        }

        private bool ShouldShowBuilding(BuildingFCDef building)
        {
            if (currentFilter == BuildingFilter.All)
                return true;

            // Get the building's traits
            if (building.traits == null || building.traits.Count == 0)
                return false;

            foreach (var traitDefName in building.traits)
            {
                var traitDef = DefDatabase<FCTraitEffectDef>.GetNamedSilentFail(traitDefName.defName);
                if (traitDef == null) continue;

                switch (currentFilter)
                {
                    case BuildingFilter.Happiness:
                        if (traitDef.happinessLostBase != 0 || traitDef.happinessGainedBase != 0 || 
                            Math.Abs(traitDef.happinessLostMultiplier - 1.0) > 0.001 || 
                            Math.Abs(traitDef.happinessGainedMultiplier - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Food:
                        if (traitDef.productionBaseFood != 0 || Math.Abs(traitDef.productionMultiplierFood - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Weapons:
                        if (traitDef.productionBaseWeapons != 0 || Math.Abs(traitDef.productionMultiplierWeapons - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Apparel:
                        if (traitDef.productionBaseApparel != 0 || Math.Abs(traitDef.productionMultiplierApparel - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Research:
                        if (traitDef.productionBaseResearch != 0 || Math.Abs(traitDef.productionMultiplierResearch - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Medicine:
                        if (traitDef.productionBaseMedicine != 0 || Math.Abs(traitDef.productionMultiplierMedicine - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Power:
                        if (traitDef.productionBasePower != 0 || Math.Abs(traitDef.productionMultiplierPower - 1.0) > 0.001)
                            return true;
                        break;
                    
                    case BuildingFilter.Basetax:
                        if (traitDef.taxBasePercentage != 0 || traitDef.taxBaseRandomModifier != 0)
                            return true;
                        break;
                    
                    case BuildingFilter.Workers:
                        if (traitDef.workerBaseMax != 0 || traitDef.workerBaseOverMax != 0 || traitDef.workerBaseCost != 0)
                            return true;
                        break;
                }
            }

            return false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Calculate dynamic layout
            CalculateLayout(inRect);
            
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            
            // Draw filter buttons
            DrawFilterButtons(inRect);
            
            // Dynamic scroll area that adjusts to window size and accounts for filter area
            var scrollAreaTop = FilterArea.y + FilterArea.height + 5;
            var outRect = new Rect(0f, scrollAreaTop, inRect.width, inRect.height - scrollAreaTop);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, filteredBuildingList.Count * rowHeight);
            
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            
            //Buildings
            for (int i = 0; i < filteredBuildingList.Count; i++)
            {
                BuildingFCDef building = filteredBuildingList[i];
                var newBuildingWindow = ls.GetRect(rowHeight);
                var newBuildingIcon = new Rect(newBuildingWindow.x + offset, newBuildingWindow.y + offset, 64, 64);
                var newBuildingLabel = new Rect(newBuildingWindow.x + 80, newBuildingWindow.y + 5,
                    newBuildingWindow.width - 80, 20);
                var newBuildingDesc = new Rect(newBuildingWindow.x + 80, newBuildingWindow.y + 25,
                    newBuildingWindow.width - 80, 65);

                if (Widgets.ButtonInvisible(newBuildingWindow))
                {
                    //If click on building
                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                    if (building == buildingDef)
                    {
                        //if the same building
                        list.Add(new FloatMenuOption("Destroy".Translate(), delegate
                        {
                            settlement.deconstructBuilding(buildingSlot);
                            Find.WindowStack.TryRemove(this);
                            Find.WindowStack.WindowOfType<SettlementWindowFc>().windowUpdateFc();
                        }));
                    }
                    else
                    {
                        //if not the same building
                        list.Add(new FloatMenuOption("Build".Translate(), delegate
                        {
                            if (!settlement.validConstructBuilding(building, buildingSlot, settlement)) return;
                            FCEvent tmpEvt = new FCEvent(true)
                            {
                                def = FCEventDefOf.constructBuilding,
                                source = settlement.mapLocation,
                                planetName = settlement.planetName,
                                building = building,
                                buildingSlot = buildingSlot
                            };

                            int triggerTime = building.constructionDuration;
                            if (factionfc.hasPolicy(FCPolicyDefOf.isolationist))
                                triggerTime /= 2;

                            tmpEvt.timeTillTrigger = Find.TickManager.TicksGame + triggerTime;
                            Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);

                            PaymentUtil.paySilver(Convert.ToInt32(building.cost));
                            settlement.deconstructBuilding(buildingSlot);
                            Messages.Message(building.label + " " + "WillBeConstructedIn".Translate() + " " + (tmpEvt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString(), MessageTypeDefOf.PositiveEvent);
                            settlement.buildings[buildingSlot] = BuildingFCDefOf.Construction;
                            Find.WindowStack.TryRemove(this);
                            Find.WindowStack.WindowOfType<SettlementWindowFc>().windowUpdateFc();
                        }));
                    }

                    FloatMenu menu = new FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }

                Widgets.DrawMenuSection(newBuildingWindow);
                Widgets.DrawMenuSection(newBuildingIcon);
                Widgets.DrawLightHighlight(newBuildingIcon);
                Widgets.ButtonImage(newBuildingIcon, building.Icon);

                Text.Font = GameFont.Small;
                Widgets.ButtonTextSubtle(newBuildingLabel, "");
                Widgets.Label(newBuildingLabel, "  " + building.LabelCap + " - " + "Cost".Translate() + ": " + building.cost);

                Text.Font = GameFont.Tiny;
                Widgets.Label(newBuildingDesc, building.desc);
            }

            ls.End();
            Widgets.EndScrollView();

            //Top Window - now using dynamic sizing
            Widgets.DrawMenuSection(TopWindow);
            Widgets.DrawHighlight(TopWindow);
            Widgets.DrawMenuSection(TopIcon);
            Widgets.DrawLightHighlight(TopIcon);

            // Dynamic border that adjusts to window width
            Widgets.DrawBox(new Rect(0, 0, inRect.width, TopWindow.height + 5));
            Widgets.ButtonImage(TopIcon, buildingDef.Icon);

            Widgets.ButtonTextSubtle(TopName, "");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(TopName.x + 5, TopName.y, TopName.width, TopName.height), buildingDef.LabelCap);

            Widgets.DrawMenuSection(new Rect(TopDescription.x - 5, TopDescription.y - 5, TopDescription.width + 10, TopDescription.height));
            Text.Font = GameFont.Small;
            Widgets.Label(TopDescription, buildingDef.desc);

            // Dynamic horizontal line that spans the full width
            Widgets.DrawLineHorizontal(0, TopWindow.y + TopWindow.height, inRect.width);
            
            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public FCBuildingWindow(SettlementFC settlement, int buildingSlot)
        {
            factionfc = Find.World.GetComponent<FactionFC>();
            buildingList = new List<BuildingFCDef>();
            filteredBuildingList = new List<BuildingFCDef>();
            
            foreach (BuildingFCDef building in DefDatabase<BuildingFCDef>.AllDefsListForReading.Where(def => def.RequiredModsLoaded))
            {
                if(building.defName != "Empty" && building.defName != "Construction")
                {
                    //If not a building that shouldn't appear on the list
                    if (building.techLevel <= factionfc.techLevel)
                    {
                        //If building techlevel requirement is met
                        if (building.applicableBiomes.Count == 0 || building.applicableBiomes.Any() 
                            && building.applicableBiomes.Contains(settlement.biome)){
                            //If building meets the biome requirements
                            buildingList.Add(building);
                        }
                    }
                }
            }

            buildingList.Sort(FactionColonies.CompareBuildingDef);

            // Initialize filtered list with all buildings
            filteredBuildingList.AddRange(buildingList);

            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            resizeable = true;  // Enable window resizing

            this.settlement = settlement;
            this.buildingSlot = buildingSlot;
            buildingDef = settlement.buildings[buildingSlot];
        }
    }
}
