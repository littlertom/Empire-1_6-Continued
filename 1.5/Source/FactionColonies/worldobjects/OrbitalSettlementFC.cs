using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class OrbitalSettlementFC : WorldSettlementFC
    {
        public OrbitalPlatformTier platformTier = OrbitalPlatformTier.Basic;
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref platformTier, "platformTier", OrbitalPlatformTier.Basic);
        }
        
        // Override any orbital-specific behavior here
        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            return $"{baseString}\nPlatform Type: {platformTier}";
        }
    }
}
