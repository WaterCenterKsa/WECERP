Imports WecErp.Domain

Namespace WecErp.Application.Bookings
    Public Class CreateBookingRequest
        Public Property CustomerId As Guid
        Public Property ItemId As Guid
        Public Property ResourceId As Guid?
        Public Property StartsUtc As DateTimeOffset
        Public Property EndsUtc As DateTimeOffset
        Public Property Notes As String = String.Empty
    End Class

    Public Class BookingDto
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property ItemId As Guid
        Public Property ResourceId As Guid?
        Public Property StartsUtc As DateTimeOffset
        Public Property EndsUtc As DateTimeOffset
        Public Property Status As BookingStatus
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset
    End Class
End Namespace
