Namespace WecErp.Domain
    Public Enum ServiceContractStatus
        Draft = 1
        Active = 2
        Suspended = 3
        Expired = 4
        Cancelled = 5
    End Enum

    Public Enum WorkOrderStatus
        Open = 1
        Scheduled = 2
        InProgress = 3
        Completed = 4
        Cancelled = 5
    End Enum

    Public Class ServiceContract
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property Status As ServiceContractStatus = ServiceContractStatus.Draft
        Public Property StartsOn As DateOnly
        Public Property EndsOn As DateOnly
        Public Property VisitFrequencyPerWeek As Integer
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class

    Public Class WorkOrder
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property ServiceContractId As Guid?
        Public Property SiteId As Guid?
        Public Property AssetId As Guid?
        Public Property Status As WorkOrderStatus = WorkOrderStatus.Open
        Public Property ScheduledStartUtc As DateTimeOffset?
        Public Property ScheduledEndUtc As DateTimeOffset?
        Public Property Description As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
