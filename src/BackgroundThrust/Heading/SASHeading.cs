namespace BackgroundThrust.Heading;

/// <summary>
/// This is just an easy marker to tell whether a heading is a SAS heading
/// or not.
/// </summary>
public interface ISASHeading
{
    /// <summary>
    /// The autopilot mode that this heading corresponds to.
    /// </summary>
    VesselAutopilot.AutopilotMode Mode { get; }
}
