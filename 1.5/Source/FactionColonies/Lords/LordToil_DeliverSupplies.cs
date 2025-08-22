using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
	class LordToil_DeliverSupplies : LordToil
	{
		public override bool AllowSatisfyLongNeeds => false;

		private bool NoPawnCarries => lord.ownedPawns.All(pawn => pawn.carryTracker.CarriedThing == null);

		private bool sendMessage = false;
		private bool cellIsSet = false;
		private IntVec3 deliveryCell;
		private IntVec3 enterCell = IntVec3.Invalid;
		private int lastSetCellAttemptTick = 0; // Add this field

        public bool LeavingModeEngaged { get => sendMessage; }

        private void SetCell()
		{
			if (!cellIsSet)
			{
				// Fix: Ensure we have valid pawns before proceeding
				if (lord.ownedPawns.NullOrEmpty())
				{
					cellIsSet = true;
					return;
				}

				Pawn leadPawn = lord.ownedPawns[0];
				
				// Get fallback location from the lord job
				LordJob_DeliverSupplies deliveryJob = lord.LordJob as LordJob_DeliverSupplies;
				IntVec3 fallbackPos = (deliveryJob != null && deliveryJob.fallbackLocation.IsValid) 
					? deliveryJob.fallbackLocation 
					: IntVec3.Invalid;

				// IMPORTANT: Wait for pawns to be properly spawned before proceeding
				if (!leadPawn.Spawned || !leadPawn.Position.IsValid)
				{
					// PERFORMANCE FIX: Only try once per second to avoid busy loop
					if (Find.TickManager.TicksGame - lastSetCellAttemptTick < 60)
					{
						return; // Too soon to try again
					}
					lastSetCellAttemptTick = Find.TickManager.TicksGame;
					
					// If pawns aren't spawned yet, use fallback position but don't mark as set
					if (fallbackPos.IsValid)
					{
						enterCell = fallbackPos;
						deliveryCell = fallbackPos;
					}
					else
					{
						// Find a reasonable default position
						if (PaymentUtil.checkForTaxSpot(lord.Map, out IntVec3 taxSpot))
						{
							enterCell = taxSpot;
							deliveryCell = taxSpot;
						}
						else
						{
							enterCell = lord.Map.Center;
							deliveryCell = lord.Map.Center;
						}
					}
					// Don't set cellIsSet = true yet, so we'll try again later
					return;
				}

				// Now we have a properly spawned pawn with valid position
				enterCell = leadPawn.Position;

				// Try to get a proper delivery cell
				try
				{
					TraverseParms traverseParms = DeliveryEvent.DeliveryTraverseParms;
					traverseParms.pawn = leadPawn;
					
					// First try to find tax spot
					if (PaymentUtil.checkForTaxSpot(lord.Map, out deliveryCell))
					{
						// Validate tax spot
						if (!deliveryCell.IsValid || !deliveryCell.InBounds(lord.Map))
						{
							deliveryCell = lord.Map.Center;
						}
						
						// Check if the tax spot is reachable
						if (!leadPawn.CanReach(deliveryCell, PathEndMode.OnCell, Danger.Deadly))
						{
							// Tax spot exists but not reachable, find alternative
							IntVec3 alternativeCell;
							if (CellFinder.TryFindRandomReachableNearbyCell(deliveryCell, lord.Map, 10, traverseParms, null, null, out alternativeCell) && alternativeCell.IsValid)
							{
								deliveryCell = alternativeCell;
							}
							else
							{
								// Can't find reachable cell near tax spot, use map center
								deliveryCell = lord.Map.Center;
							}
						}
					}
					else
					{
						// No tax spot, use the GetDeliveryCell method
						deliveryCell = DeliveryEvent.GetDeliveryCell(traverseParms, lord.Map);
						
						// Validate the result
						if (!deliveryCell.IsValid || !deliveryCell.InBounds(lord.Map))
						{
							deliveryCell = lord.Map.Center;
						}
					}
				}
				catch (System.Exception ex)
				{
					Log.Warning($"[FactionColonies] Error finding delivery cell: {ex.Message}. Using fallback position.");
					deliveryCell = fallbackPos.IsValid ? fallbackPos : lord.Map.Center;
				}
				
				cellIsSet = true;
			}
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count(); i++)
			{
				SetCell();
				Pawn pawn = lord.ownedPawns[i];
				pawn.mindState.canFleeIndividual = true;
				if (!NoPawnCarries)
				{
					if (i == 0)
					{
						pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("FCDeliverItem"))
						{
							focus = deliveryCell
						};
					}
					else
					{
						TraverseParms traverseParms = DeliveryEvent.DeliveryTraverseParms;
						traverseParms.pawn = pawn;
						pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("FCFollowAndDeliverItem"))
						{
							focus = (lord.ownedPawns[0].carryTracker.CarriedThing == null) ? (LocalTargetInfo) deliveryCell : lord.ownedPawns[0],
						};
					}
					continue;
				}

				if (!sendMessage) 
				{
					Messages.Message("deliveryPawnsLeavingMap".Translate(), MessageTypeDefOf.NeutralEvent);
					sendMessage = true;
				}

				if (enterCell.IsValid)
                {
					pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapNearDutyTarget)
					{
						locomotion = LocomotionUrgency.Sprint,
						focus = enterCell,
						canDig = false,
					};
                }
				else
                {
					pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest)
					{
						locomotion = LocomotionUrgency.Sprint,
						canDig = false
					};
				}
			};
		}

		public override void Notify_ReachedDutyLocation(Pawn pawn)
		{
			UpdateAllDuties();
			base.Notify_ReachedDutyLocation(pawn);
		}
	}
}

