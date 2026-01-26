namespace OrderMonitor.Core.Models;

/// <summary>
/// Configuration for status thresholds.
/// </summary>
public class StatusThresholdsConfig
{
    public StatusRange PrepStatuses { get; set; } = new();
    public StatusRange FacilityStatuses { get; set; } = new();

    /// <summary>
    /// Gets the threshold hours for a given status ID.
    /// </summary>
    public int GetThresholdHours(int statusId)
    {
        if (statusId >= PrepStatuses.MinStatusId && statusId <= PrepStatuses.MaxStatusId)
            return PrepStatuses.ThresholdHours;

        if (statusId >= FacilityStatuses.MinStatusId && statusId <= FacilityStatuses.MaxStatusId)
            return FacilityStatuses.ThresholdHours;

        return 24; // Default threshold
    }
}

/// <summary>
/// Represents a range of status IDs with a threshold.
/// </summary>
public class StatusRange
{
    public int MinStatusId { get; set; }
    public int MaxStatusId { get; set; }
    public int ThresholdHours { get; set; }
}
