Namespace WecErp.Domain
    Public Class Booking
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property ItemId As Guid
        Public Property ResourceId As Guid?
        Public Property StartsUtc As DateTimeOffset
        Public Property EndsUtc As DateTimeOffset
        Public Property Status As BookingStatus = BookingStatus.Draft
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
