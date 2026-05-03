using wipisoft;

namespace TracePML.Services;

public class RegistrySettings
{
    private const string RegPath = @"wipiSoft\TracePML";

    public bool TestMode
    {
        get => WpsHKCU.HKCU_GetBoolean(RegPath, "TestMode", false);
        set => WpsHKCU.HKCU_SetString(RegPath, "TestMode", value.ToString());
    }

    public bool LocalTest
    {
        get => WpsHKCU.HKCU_GetBoolean(RegPath, "LocalTest", false);
        set => WpsHKCU.HKCU_SetString(RegPath, "LocalTest", value.ToString());
    }

    // Filtres émission
    public bool ShowOrder { get; set; } = true;
    public bool ShowRequestInfoProduct { get; set; } = true;
    public bool ShowAcknowledgement { get; set; }
    public bool ShowEmptying { get; set; }

    // Filtres réception
    public bool ShowOrderResponse { get; set; } = true;
    public bool ShowDispoInfoResponse { get; set; } = true;
    public bool ShowDeliveryResponse { get; set; }
    public bool ShowServiceEnd { get; set; }
    public bool ShowResponseError { get; set; }
}
