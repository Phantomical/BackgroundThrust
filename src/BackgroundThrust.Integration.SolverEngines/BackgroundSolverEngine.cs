using SolverEngines;

namespace BackgroundThrust.Integration.SolverEngines;

public class BackgroundSolverEngine : BackgroundEngine
{
    protected override void EngineFixedUpdate()
    {
        if (Engine is ModuleEnginesSolver engine)
            engine.FixedUpdate();
        else
            base.EngineFixedUpdate();
    }

    public override void OnSave(ConfigNode node)
    {
        base.OnSave(node);
    }

    protected override void OnGoOnRails()
    {
        base.OnGoOnRails();
    }
}
