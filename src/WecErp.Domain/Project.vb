Namespace WecErp.Domain
    Public Enum ProjectStatus
        Draft = 1
        Active = 2
        OnHold = 3
        Completed = 4
        Cancelled = 5
    End Enum

    Public Class Project
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property Name As String = String.Empty
        Public Property Status As ProjectStatus = ProjectStatus.Draft
        Public Property StartsOn As DateOnly?
        Public Property EndsOn As DateOnly?
        Public Property BudgetAmount As Decimal
        Public Property CurrencyCode As String = "SAR"
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Tasks As List(Of ProjectTask) = New List(Of ProjectTask)()
    End Class

    Public Class ProjectTask
        Public Property Id As Guid
        Public Property ProjectId As Guid
        Public Property Name As String = String.Empty
        Public Property Sequence As Integer
        Public Property IsCompleted As Boolean
        Public Property EstimatedCost As Decimal
        Public Property ActualCost As Decimal
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
