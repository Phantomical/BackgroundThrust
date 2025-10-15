using BackgroundThrust;
using KERBALISM;

namespace BackgroundThrust.Integration.Kerbalism;

public class WrapBackgroundThrust : ResourceInfo.Wrap
{
    public BackgroundThrustVessel module;

    public override double amount
    {
        get => module?.Thrust ?? 0.0;
        set => module?.SetThrust(value);
    }
    public override double maxAmount
    {
        get => double.PositiveInfinity;
        set { }
    }

    public override void Reset()
    {
        module = null;
    }
}
