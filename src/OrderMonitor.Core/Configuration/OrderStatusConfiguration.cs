namespace OrderMonitor.Core.Configuration;

/// <summary>
/// Configuration and registry for all 152 monitored order statuses.
/// </summary>
public static class OrderStatusConfiguration
{
    /// <summary>
    /// Default threshold for unknown statuses (hours).
    /// </summary>
    public const int DefaultThresholdHours = 24;

    /// <summary>
    /// Threshold for preparation statuses (hours).
    /// </summary>
    public const int PrepThresholdHours = 6;

    /// <summary>
    /// Threshold for facility statuses (hours).
    /// </summary>
    public const int FacilityThresholdHours = 48;

    /// <summary>
    /// Minimum status ID for preparation statuses.
    /// </summary>
    public const int PrepMinStatusId = 3001;

    /// <summary>
    /// Maximum status ID for preparation statuses.
    /// </summary>
    public const int PrepMaxStatusId = 3910;

    /// <summary>
    /// Minimum status ID for facility statuses.
    /// </summary>
    public const int FacilityMinStatusId = 4001;

    /// <summary>
    /// Maximum status ID for facility statuses.
    /// </summary>
    public const int FacilityMaxStatusId = 5830;

    private static readonly Dictionary<int, OrderStatusDefinition> _statuses;
    private static readonly Dictionary<string, List<OrderStatusDefinition>> _categorizedStatuses;

    static OrderStatusConfiguration()
    {
        _statuses = InitializeStatuses();
        _categorizedStatuses = CategorizeStatuses(_statuses.Values);
    }

    /// <summary>
    /// Gets all configured statuses.
    /// </summary>
    public static IEnumerable<OrderStatusDefinition> GetAllStatuses() => _statuses.Values;

    /// <summary>
    /// Gets a status by its ID.
    /// </summary>
    public static OrderStatusDefinition? GetStatusById(int statusId) =>
        _statuses.TryGetValue(statusId, out var status) ? status : null;

    /// <summary>
    /// Gets all preparation statuses (6-hour threshold).
    /// </summary>
    public static IEnumerable<OrderStatusDefinition> GetPrepStatuses() =>
        _statuses.Values.Where(s => s.StatusId >= PrepMinStatusId && s.StatusId <= PrepMaxStatusId);

    /// <summary>
    /// Gets all facility statuses (48-hour threshold).
    /// </summary>
    public static IEnumerable<OrderStatusDefinition> GetFacilityStatuses() =>
        _statuses.Values.Where(s => s.StatusId >= FacilityMinStatusId && s.StatusId <= FacilityMaxStatusId);

    /// <summary>
    /// Gets the threshold hours for a given status ID.
    /// </summary>
    public static int GetThresholdHours(int statusId)
    {
        if (statusId >= PrepMinStatusId && statusId <= PrepMaxStatusId)
            return PrepThresholdHours;

        if (statusId >= FacilityMinStatusId && statusId <= FacilityMaxStatusId)
            return FacilityThresholdHours;

        return DefaultThresholdHours;
    }

    /// <summary>
    /// Checks if a status ID is a preparation status.
    /// </summary>
    public static bool IsPrepStatus(int statusId) =>
        statusId >= PrepMinStatusId && statusId <= PrepMaxStatusId;

    /// <summary>
    /// Checks if a status ID is a facility status.
    /// </summary>
    public static bool IsFacilityStatus(int statusId) =>
        statusId >= FacilityMinStatusId && statusId <= FacilityMaxStatusId;

    /// <summary>
    /// Gets all status categories with their statuses.
    /// </summary>
    public static IReadOnlyDictionary<string, List<OrderStatusDefinition>> GetStatusCategories() =>
        _categorizedStatuses;

    /// <summary>
    /// Gets statuses by category name.
    /// </summary>
    public static IEnumerable<OrderStatusDefinition> GetStatusesByCategory(string category) =>
        _categorizedStatuses.TryGetValue(category, out var statuses) ? statuses : [];

    private static Dictionary<int, OrderStatusDefinition> InitializeStatuses()
    {
        var statuses = new List<OrderStatusDefinition>
        {
            // ============================================
            // PREPARATION & ALERT STATUSES (6-hour threshold)
            // StatusId range: 3001-3910
            // ============================================

            // Initialization statuses
            new(3001, "Initialized_New", PrepThresholdHours, "Preparation"),
            new(3002, "Initialized_Rma", PrepThresholdHours, "Preparation"),
            new(3003, "Initialized_Reprint", PrepThresholdHours, "Preparation"),
            new(3004, "ReprintRequested", PrepThresholdHours, "Preparation"),

            // Assignment statuses
            new(3020, "FacilityAssigned", PrepThresholdHours, "Preparation"),
            new(3030, "Order Split (Awaiting File)", PrepThresholdHours, "Preparation"),
            new(3040, "DistributionAssigned", PrepThresholdHours, "Preparation"),

            // Preparation statuses
            new(3050, "PreparationStarted", PrepThresholdHours, "Preparation"),
            new(3052, "FileReceived", PrepThresholdHours, "Preparation"),
            new(3053, "LabPrintPendingPdf", PrepThresholdHours, "Preparation"),
            new(3054, "LabPrintPdfPendingCompress", PrepThresholdHours, "Preparation"),
            new(3055, "PreReadyToPrint", PrepThresholdHours, "Preparation"),
            new(3056, "PreparationDone_Individual", PrepThresholdHours, "Preparation"),
            new(3058, "PreparationDoneAwaitingDecison", PrepThresholdHours, "Preparation"),
            new(3059, "PreparationDoneAwaingReview", PrepThresholdHours, "Preparation"),
            new(3060, "PreparationDone", PrepThresholdHours, "Preparation"),

            // PrintBox Alert statuses
            new(3720, "PrintBoxAlert_RenderStatusFailure", PrepThresholdHours, "PrintBoxAlert"),
            new(3721, "PrintBoxAlert_HasIncorrectPageCount", PrepThresholdHours, "PrintBoxAlert"),
            new(3722, "PrintBoxAlert_ProjectOrdered", PrepThresholdHours, "PrintBoxAlert"),
            new(3723, "PrintBoxAlert_HasEditorValidationError", PrepThresholdHours, "PrintBoxAlert"),
            new(3724, "PrintBoxAlert_AlreadyExists", PrepThresholdHours, "PrintBoxAlert"),
            new(3725, "PrintBoxAlert_RenderStatusFailureJS", PrepThresholdHours, "PrintBoxAlert"),
            new(3726, "PrintBoxAlert_OrderStatusError", PrepThresholdHours, "PrintBoxAlert"),
            new(3727, "PrintBoxAlert_HasDisabledValues", PrepThresholdHours, "PrintBoxAlert"),
            new(3728, "PrintBoxAlert_HasMissingPhotos", PrepThresholdHours, "PrintBoxAlert"),
            new(3729, "PrintBoxAlert_Unhandle", PrepThresholdHours, "PrintBoxAlert"),

            // Ryzan Alert statuses
            new(3730, "RyzanAlert_ValidationFail", PrepThresholdHours, "RyzanAlert"),
            new(3731, "RyzanAlert_InvalidRatio", PrepThresholdHours, "RyzanAlert"),
            new(3732, "RyzanAlert_PendingDownload", PrepThresholdHours, "RyzanAlert"),

            // File status errors
            new(3790, "FileStatusError", PrepThresholdHours, "FileError"),
            new(3791, "PrintBoxAlert_RenderStatusFailureJS_FontIssue", PrepThresholdHours, "PrintBoxAlert"),
            new(3796, "PrintBoxAlert_Reviewed_Accepted", PrepThresholdHours, "PrintBoxAlert"),
            new(3797, "PrintBoxAlert_Reviewed_ContactCustomerRequired", PrepThresholdHours, "PrintBoxAlert"),
            new(3798, "PrintBoxAlert_ContactedCustomer", PrepThresholdHours, "PrintBoxAlert"),

            // On-Hold statuses
            new(3800, "GiveWayToXmasDelivery", PrepThresholdHours, "OnHold"),
            new(3801, "OnHoldFreeCode", PrepThresholdHours, "OnHold"),
            new(3802, "OnHoldOutOfStock", PrepThresholdHours, "OnHold"),
            new(3803, "OnHoldBadAddress", PrepThresholdHours, "OnHold"),
            new(3804, "AlertBadAddressIrishRepublic", PrepThresholdHours, "OnHold"),
            new(3805, "OnHoldBadAddressNoResponse", PrepThresholdHours, "OnHold"),
            new(3806, "AlertBadAddressIllegibleZipcode", PrepThresholdHours, "OnHold"),
            new(3807, "Verification_Hold", PrepThresholdHours, "OnHold"),
            new(3808, "OnHoldXmasStandard", PrepThresholdHours, "OnHold"),
            new(3809, "OnHoldXmasPriority", PrepThresholdHours, "OnHold"),
            new(3810, "Verification_ContactedCustomer", PrepThresholdHours, "OnHold"),
            new(3811, "QualityIssueContactedCustomer", PrepThresholdHours, "OnHold"),
            new(3815, "PREP:BadAddressContactedCustomer", PrepThresholdHours, "OnHold"),
            new(3820, "OnHoldEmptyPhotoElement", PrepThresholdHours, "OnHold"),
            new(3821, "OnHoldEmptyPhotoElementConfirmed", PrepThresholdHours, "OnHold"),

            // Alert statuses
            new(3822, "AlertCP_Erroneous", PrepThresholdHours, "Alert"),
            new(3823, "AlertCP_DownloadPhotoFail", PrepThresholdHours, "Alert"),
            new(3824, "AlertCP_DownloadXmlFail", PrepThresholdHours, "Alert"),
            new(3825, "AlertCP_OldOrderNoFile", PrepThresholdHours, "Alert"),
            new(3826, "AlertPackageInfoMissing", PrepThresholdHours, "Alert"),
            new(3827, "AlertRyzan_Erroneous", PrepThresholdHours, "Alert"),
            new(3828, "AlertPrintBox_Erroneous", PrepThresholdHours, "Alert"),
            new(3829, "AlertFile_NotFoundInLocation", PrepThresholdHours, "Alert"),
            new(3830, "MwareError500", PrepThresholdHours, "Alert"),
            new(3831, "InvalidRatio", PrepThresholdHours, "Alert"),
            new(3832, "InvalidFujiSize", PrepThresholdHours, "Alert"),

            // PrepDone Alert statuses
            new(3835, "PrepDoneAlert_NoProductWeight", PrepThresholdHours, "PrepDoneAlert"),
            new(3836, "PrepDoneAlert_NoPackageWeight", PrepThresholdHours, "PrepDoneAlert"),
            new(3840, "PrepDoneAlert_NoShippingRule", PrepThresholdHours, "PrepDoneAlert"),
            new(3841, "AlertNoAllocationRule", PrepThresholdHours, "PrepDoneAlert"),
            new(3842, "AlertBadOrderSplit", PrepThresholdHours, "PrepDoneAlert"),
            new(3843, "PrepDoneAlert_UnsupportedProduct", PrepThresholdHours, "PrepDoneAlert"),
            new(3844, "PrepDoneAlert_ProductVolumeMissing", PrepThresholdHours, "PrepDoneAlert"),
            new(3845, "AlertOverWeight", PrepThresholdHours, "PrepDoneAlert"),
            new(3846, "PrepDoneAlert_ProductionCostMissing", PrepThresholdHours, "PrepDoneAlert"),
            new(3847, "AlertShippingCustomDeclarationHigh", PrepThresholdHours, "PrepDoneAlert"),
            new(3848, "AlertPostalCodeIssue", PrepThresholdHours, "PrepDoneAlert"),
            new(3849, "AlertUnsupportShippingservice", PrepThresholdHours, "PrepDoneAlert"),

            // Insert/Update failures
            new(3850, "InsertBOFail", PrepThresholdHours, "SystemError"),
            new(3851, "PendingUpdate_NotForBO", PrepThresholdHours, "SystemError"),
            new(3852, "UpdateFail_NotForBO", PrepThresholdHours, "SystemError"),
            new(3853, "UpdateBoFail_NotForBO", PrepThresholdHours, "SystemError"),

            // Invalid/Unpaid statuses
            new(3860, "Invalid", PrepThresholdHours, "Invalid"),
            new(3861, "Unpaid", PrepThresholdHours, "Invalid"),
            new(3865, "ErrorOldOrderNoFile", PrepThresholdHours, "SystemError"),
            new(3866, "AlertNoWarehouseName", PrepThresholdHours, "Alert"),
            new(3867, "AlertEmptyCONumber", PrepThresholdHours, "Alert"),
            new(3868, "AlertPotentialDuplicate", PrepThresholdHours, "Alert"),

            // PrepDone Address alerts
            new(3871, "PrepDoneAlert_NoHoldShippingLabelFailed", PrepThresholdHours, "PrepDoneAlert"),
            new(3872, "PrepDoneAlert_BadAddress", PrepThresholdHours, "PrepDoneAlert"),
            new(3873, "PrepDoneAlert_BadAddressContactCustomerRequired", PrepThresholdHours, "PrepDoneAlert"),
            new(3874, "PrepDoneAlert_BadAddressContactedCustomer", PrepThresholdHours, "PrepDoneAlert"),
            new(3875, "PrepDoneAlert_BadAddressFixedForProduction", PrepThresholdHours, "PrepDoneAlert"),

            // Communication errors
            new(3900, "ComError", PrepThresholdHours, "ComError"),
            new(3901, "ComError_Payment", PrepThresholdHours, "ComError"),
            new(3902, "ComError_Timeout", PrepThresholdHours, "ComError"),
            new(3903, "ComError_AccessDenied", PrepThresholdHours, "ComError"),
            new(3904, "ComError_BadProduct", PrepThresholdHours, "ComError"),
            new(3905, "ComError_Kafka", PrepThresholdHours, "ComError"),
            new(3906, "ComError_BadXml", PrepThresholdHours, "ComError"),
            new(3907, "ComError_BadShippingService", PrepThresholdHours, "ComError"),
            new(3908, "ComError_DuplicateKey", PrepThresholdHours, "ComError"),

            // Quality issues
            new(3910, "QualityIssueNeedCancellation", PrepThresholdHours, "QualityIssue"),

            // ============================================
            // FACILITY & SHIPPING STATUSES (48-hour threshold)
            // StatusId range: 4001-5830
            // ============================================

            // Facility submission statuses
            new(4001, "SentToFacility", FacilityThresholdHours, "Facility"),
            new(4005, "FacilityMetadataReceived", FacilityThresholdHours, "Facility"),
            new(4006, "FileRequested", FacilityThresholdHours, "Facility"),
            new(4010, "FacilityOrderSubmissionError", FacilityThresholdHours, "FacilityError"),

            // Facility reprint statuses
            new(4030, "FacilityReprintRequested", FacilityThresholdHours, "FacilityReprint"),
            new(4031, "FacilityReprintAccepted", FacilityThresholdHours, "FacilityReprint"),
            new(4032, "FacilityReprintRejected", FacilityThresholdHours, "FacilityReprint"),
            new(4039, "FacilityReprintFulfilled", FacilityThresholdHours, "FacilityReprint"),

            // Facility file download statuses
            new(4040, "FacilityFileDownloading", FacilityThresholdHours, "Facility"),
            new(4050, "FacilityFileDownloaded", FacilityThresholdHours, "Facility"),
            new(4100, "FacilityFileReceived", FacilityThresholdHours, "Facility"),
            new(4101, "FacilityFileReceivedConfirmed", FacilityThresholdHours, "Facility"),
            new(4105, "Dispatched Status RollBack", FacilityThresholdHours, "Facility"),

            // Facility verification statuses
            new(4110, "FacilityVerifiying", FacilityThresholdHours, "Facility"),
            new(4120, "FacilityVerified", FacilityThresholdHours, "Facility"),
            new(4130, "FacilityReleaseToProduction", FacilityThresholdHours, "Facility"),
            new(4140, "FacilityReachedProduction", FacilityThresholdHours, "Facility"),

            // Facility production statuses
            new(4150, "FacilityMaterialPrepared", FacilityThresholdHours, "Facility"),
            new(4160, "FacilityReadyToPrint", FacilityThresholdHours, "Facility"),
            new(4170, "FacilitySentToPrint", FacilityThresholdHours, "Facility"),
            new(4200, "PrintedInFacility", FacilityThresholdHours, "Facility"),

            // Facility manufacturing statuses
            new(4201, "Cut", FacilityThresholdHours, "Manufacturing"),
            new(4203, "Stretched", FacilityThresholdHours, "Manufacturing"),
            new(4205, "Sticking", FacilityThresholdHours, "Manufacturing"),
            new(4206, "Pressed", FacilityThresholdHours, "Manufacturing"),
            new(4210, "ManufacturedInFacility", FacilityThresholdHours, "Manufacturing"),

            // Facility consolidation statuses
            new(4310, "FacilityConsolidationDone", FacilityThresholdHours, "Facility"),
            new(4320, "FacilityPartialDispatch", FacilityThresholdHours, "Facility"),

            // Facility dispatch statuses
            new(4700, "DispatchedFromFacility", FacilityThresholdHours, "Dispatch"),
            new(4710, "FacilityDispatchedDirectObsolete", FacilityThresholdHours, "Dispatch"),

            // Facility error statuses
            new(4800, "ErrorInFacility", FacilityThresholdHours, "FacilityError"),
            new(4801, "ErrorInFacility_Timeout", FacilityThresholdHours, "FacilityError"),
            new(4802, "ErrorInFacility_Error500", FacilityThresholdHours, "FacilityError"),
            new(4803, "ErrorInFacility_BadData", FacilityThresholdHours, "FacilityError"),
            new(4804, "FacilityDownloadError_OutOfMemory", FacilityThresholdHours, "FacilityError"),
            new(4805, "FacilityErrorDLShippingLabel", FacilityThresholdHours, "FacilityError"),

            // Facility feedback statuses
            new(4810, "FacilityFeecbackBlankFile", FacilityThresholdHours, "FacilityFeedback"),
            new(4811, "FacilityFeecbackCorruptFile", FacilityThresholdHours, "FacilityFeedback"),
            new(4812, "FacilityFeecbackImpositionIssue", FacilityThresholdHours, "FacilityFeedback"),
            new(4813, "FacilityFeedbackMissingFile", FacilityThresholdHours, "FacilityFeedback"),
            new(4814, "FacilityFeecbackInternalReprint", FacilityThresholdHours, "FacilityFeedback"),
            new(4815, "FacilityFeedbackShippingError", FacilityThresholdHours, "FacilityFeedback"),

            // Facility hold statuses
            new(4820, "FacilityHoldForReview", FacilityThresholdHours, "FacilityHold"),
            new(4840, "Awaiting Facility Reprint Approval", FacilityThresholdHours, "FacilityHold"),
            new(4845, "Out Of Stock Waiting to Print", FacilityThresholdHours, "FacilityHold"),
            new(4850, "Quality Issue given to CS", FacilityThresholdHours, "FacilityHold"),
            new(4855, "ShippingErrorContactedCustomer", FacilityThresholdHours, "FacilityHold"),
            new(4856, "ShippingErrorAddressCorrected", FacilityThresholdHours, "FacilityHold"),

            // Facility download errors
            new(4880, "FacilityDownloadError_AspectRatioMismatch", FacilityThresholdHours, "FacilityError"),
            new(4881, "FacilityDownloadError_NumberOfPagesMismatch", FacilityThresholdHours, "FacilityError"),
            new(4882, "FacilityDownloadError_FileNotReady", FacilityThresholdHours, "FacilityError"),
            new(4883, "FacilityDownloadError_NoSuitableProcess", FacilityThresholdHours, "FacilityError"),

            // Facility cancelled
            new(4900, "CancelledByFacility", FacilityThresholdHours, "Cancelled"),

            // Distribution/Shipping statuses
            new(5000, "DispatchedFromFacilityObsolete", FacilityThresholdHours, "Shipping"),
            new(5010, "Delivered To DC", FacilityThresholdHours, "Shipping"),
            new(5100, "Received from Facility", FacilityThresholdHours, "Shipping"),
            new(5140, "Distribution Pick List Printed", FacilityThresholdHours, "Shipping"),
            new(5150, "Distribution Consolidation", FacilityThresholdHours, "Shipping"),
            new(5153, "Distribution Consolidation Done", FacilityThresholdHours, "Shipping"),
            new(5155, "Awaiting Consolidation", FacilityThresholdHours, "Shipping"),
            new(5157, "Awaiting Consolidation Done", FacilityThresholdHours, "Shipping"),
            new(5200, "Distribution Order Verification", FacilityThresholdHours, "Shipping"),
            new(5600, "ShippingLabelPrinted", FacilityThresholdHours, "Shipping"),
            new(5605, "ParcelCollectedByCarrier", FacilityThresholdHours, "Shipping"),

            // Quarantine statuses
            new(5800, "Quarantine", FacilityThresholdHours, "Quarantine"),
            new(5801, "Received damage in transit from Facility", FacilityThresholdHours, "Quarantine"),
            new(5802, "Received with quality issues", FacilityThresholdHours, "Quarantine"),
            new(5803, "Received duplicate", FacilityThresholdHours, "Quarantine"),
            new(5805, "Quarantine at Facility", FacilityThresholdHours, "Quarantine"),

            // Shipping address issues
            new(5810, "Waiting For REO OR RES", FacilityThresholdHours, "ShippingIssue"),
            new(5815, "Shipping Address Issue", FacilityThresholdHours, "ShippingIssue"),
            new(5816, "MANU:ShippingAddressIssueContactedCustomer", FacilityThresholdHours, "ShippingIssue"),
            new(5820, "Shipping Address Corrected", FacilityThresholdHours, "ShippingIssue"),
            new(5825, "Reprint Requested for Address Correction", FacilityThresholdHours, "ShippingIssue"),
            new(5830, "Shipping Voided", FacilityThresholdHours, "ShippingIssue"),
        };

        return statuses.ToDictionary(s => s.StatusId);
    }

    private static Dictionary<string, List<OrderStatusDefinition>> CategorizeStatuses(
        IEnumerable<OrderStatusDefinition> statuses)
    {
        return statuses
            .GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

/// <summary>
/// Represents an order status definition with its configuration.
/// </summary>
public sealed class OrderStatusDefinition
{
    public int StatusId { get; }
    public string StatusName { get; }
    public int ThresholdHours { get; }
    public string Category { get; }

    public OrderStatusDefinition(int statusId, string statusName, int thresholdHours, string category)
    {
        StatusId = statusId;
        StatusName = statusName;
        ThresholdHours = thresholdHours;
        Category = category;
    }
}
