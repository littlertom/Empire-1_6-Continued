using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.util
{
    class ShuttleSenderCaravan : ShuttleSender
    {
        private readonly Caravan caravan;

        public ShuttleSenderCaravan(PlanetTile Tile, Caravan caravan, WorldSettlementFC settlementFC) : base(Tile, settlementFC)
        {
			this.caravan = caravan;
        }

        public override bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            int tile = caravan.Tile;
            return CompLaunchable.ChoseWorldTarget(target, tile, Gen.YieldSingle(caravan), ShuttleRange, Launch, null);
        }

        public void Launch(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            ActiveTransporter activeDropPod = (ActiveTransporter)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            activeDropPod.Contents = new ActiveTransporterInfo();
            activeDropPod.Contents.innerContainer.TryAddRangeOrTransfer(caravan.GetDirectlyHeldThings(), true, true);

            TravellingTransporters travelingTransporters = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravelingShuttle);
            travelingTransporters.Tile = Tile;
            travelingTransporters.SetFaction(Faction.OfPlayer);
            travelingTransporters.destinationTile = destinationTile;
            travelingTransporters.arrivalAction = arrivalAction;
            travelingTransporters.AddTransporter(activeDropPod.Contents, false);
            Find.WorldObjects.Add(travelingTransporters);

            caravan.Destroy();
            settlementFC.shuttleUsesRemaining -= cost;
        }
	}
}
