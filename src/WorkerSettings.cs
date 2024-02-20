namespace Collector;

public class WorkerSettings
{
    private TimeSpan collectionInterval = TimeSpan.FromSeconds(30);

    public string MeterName { get; set; } = "servicecontrol";

    public TimeSpan CollectionInterval
    {
        get => collectionInterval;
        set
        {
            if (value.TotalSeconds <= 0)
            {
                throw new ArgumentException("CollectionInterval must be greater than 0 seconds");
            }
            collectionInterval = value;
        }
    }
}
