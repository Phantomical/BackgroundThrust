namespace BackgroundThrust;

public static class PartExt
{
    public static BackgroundEngine GetBackgroundEngine(this Part part)
    {
        var instance = EventDispatcher.Instance;
        if (instance is not null)
            return instance.GetBackgroundEngine(part);
        return part.FindModuleImplementing<BackgroundEngine>();
    }
}
