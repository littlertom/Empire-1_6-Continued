using System;
using Verse;

namespace FactionColonies
{
    public enum ResourceType
    {
        Food,
        Weapons,
        Apparel,
        Animals,
        Logging, // This will be "Gravtech" for orbital platforms, "Logging" for regular settlements
        Mining,
        Research,
        Power,
        Medicine
    }

    public static class ResourceUtils
    {
        public static ResourceType[] resourceTypes = (ResourceType[]) Enum.GetValues(typeof(ResourceType));
        
        public static ResourceType getTypeFromName(String name)
        {
            int index = Array.FindIndex(Enum.GetNames(typeof(ResourceType)), 
                foundName => foundName.EqualsIgnoreCase(name));
            
            if (index == -1)
            {
                Log.Warning("Unknown resource type " + name);
            }

            return resourceTypes[index];
        }
        
        // Check if orbital platform
        public static bool IsOrbitalPlatform(SettlementFC settlement)
        {
            return settlement?.worldSettlement?.def?.defName == "FCOrbitalPlatform";
        }
        
        // New method to get the display name for a resource type based on settlement type
        public static string GetResourceDisplayName(ResourceType resourceType, SettlementFC settlement)
        {
            if (resourceType == ResourceType.Logging && IsOrbitalPlatform(settlement))
            {
                return "gravtech";
            }
            if (resourceType == ResourceType.Animals && IsOrbitalPlatform(settlement))
            {
                return "chemfuel";
            }
            return resourceType.ToString().ToLower();
        }
        
        // New method to get the display label for a resource type based on settlement type
        public static string GetResourceDisplayLabel(ResourceType resourceType, SettlementFC settlement)
        {
            if (resourceType == ResourceType.Logging && IsOrbitalPlatform(settlement))
            {
                return "Gravtech";
            }
            if (resourceType == ResourceType.Animals && IsOrbitalPlatform(settlement))
            {
                return "Chemfuel";
            }
            return resourceType.ToString();
        }
    }
}
