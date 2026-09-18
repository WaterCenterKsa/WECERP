Imports WecErp.Domain

Namespace WecErp.Application.Service
    Public Class CreateServiceContractRequest
        Public Property CustomerId As Guid
        Public Property StartsOn As DateOnly
        Public Property EndsOn As DateOnly
        Public Property VisitFrequencyPerWeek As Integer
        Public Property Notes As String = String.Empty
    End Class

    Public Class CreateWorkOrderRequest
        Public Property CustomerId As Guid
        Public Property ServiceContractId As Guid?
        Public Property ScheduledStartUtc As DateTimeOffset?
        Public Property ScheduledEndUtc As DateTimeOffset?
        Public Property Description As String = String.Empty
    End Class

    Public Class ChangeWorkOrderStatusRequest
        Public Property Status As String = String.Empty
    End Class
End Namespace
