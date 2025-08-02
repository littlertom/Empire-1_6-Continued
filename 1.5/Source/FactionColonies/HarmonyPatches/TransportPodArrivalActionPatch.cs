using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch]
    public class WorldSettlementTransportersDefendAction : TransportersArrivalAction_LandInSpecificCell
    {
    private readonly IntVec3 cell;
    private readonly MapParent mapParent;
    private readonly bool landInShuttle;

    public WorldSettlementTransportersDefendAction(WorldSettlementFC mapParent, IntVec3 cell, bool landInShuttle)
    {
        this.mapParent = mapParent;
        this.cell = cell;
        this.landInShuttle = landInShuttle;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TransportersArrivalAction_LandInSpecificCell), "Arrived")]
    private static void ArrivePatch(TransportersArrivalAction_LandInSpecificCell __instance, List<ActiveTransporterInfo> transporters, PlanetTile tile)
    {
        if (Traverse.Create(__instance).Field("mapParent").GetValue() is WorldSettlementFC settlement)
        {
            List<Pawn> pawns = new List<Pawn>();
            bool hasAnyPawns = false;

            foreach (ActiveTransporterInfo activeTransporterInfo in transporters)
            {
                foreach (Thing thing in activeTransporterInfo.innerContainer)
                {
                    if (thing is Pawn pawn)
                    {
                        hasAnyPawns = true;
                        pawns.Add(pawn);
                    }
                }
            }

            if (hasAnyPawns) settlement.AddToDefenceFromList(pawns, tile);
        }
    }
    }
}
