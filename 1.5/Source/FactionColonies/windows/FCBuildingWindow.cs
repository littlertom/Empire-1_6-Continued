using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    class FCBuildingWindow : Window
    {
        readonly SettlementFC settlement;
        readonly int buildingSlot;
        readonly BuildingFCDef buildingDef;
        readonly List<BuildingFCDef> buildingList;
        readonly FactionFC factionfc;

        private static readonly int offset = 8;
        private Vector2 scrollPosition = Vector2.zero;
        private static readonly int rowHeight = 90;
        
        // Dynamic rectangles that will be calculated based on window size
        Rect TopWindow;
        Rect TopIcon;
        Rect TopName;
        Rect TopDescription;

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
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Calculate dynamic layout
            CalculateLayout(inRect);
            
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            
            // Dynamic scroll area that adjusts to window size
            var outRect = new Rect(0f, TopWindow.height + 5f, inRect.width, inRect.height - TopWindow.height - 10f);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, buildingList.Count * rowHeight);
            
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            
            //Buildings
            for (int i = 0; i < buildingList.Count; i++)
            {
                BuildingFCDef building = buildingList[i];
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
