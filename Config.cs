using Rocket.API;

namespace FinxEssentials
{
    public class CombinedConfig : IRocketPluginConfiguration
    {
        // RepairPluginConfig properties
        public float HealthToAdd { get; set; } = 100.0f;
        public bool EnableInfiniteStamina;


        // RefuelPluginConfig properties
        public int DefaultFuelValue { get; set; } = 100;

        public void LoadDefaults()
        {
            // Set the default values for the configurations here
            HealthToAdd = 100.0f;
            DefaultFuelValue = 100;
            EnableInfiniteStamina = true;
        }
    }
}
