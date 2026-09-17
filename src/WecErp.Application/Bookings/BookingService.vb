Imports WecErp.Domain

Namespace WecErp.Application.Bookings
    Public Class BookingService
        Public Function ValidateNewBooking(request As CreateBookingRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If request.ItemId = Guid.Empty Then Return "ItemId is required."
            If request.StartsUtc >= request.EndsUtc Then Return "EndsUtc must be later than StartsUtc."
            If request.EndsUtc <= DateTimeOffset.UtcNow Then Return "Booking must end in the future."
            If request.EndsUtc.Subtract(request.StartsUtc) > TimeSpan.FromDays(31) Then Return "Booking duration cannot exceed 31 days."
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateBookingRequest) As Booking
            Return New Booking With {
                .Id = Guid.NewGuid(),
                .Number = CreateBookingNumber(),
                .CustomerId = request.CustomerId,
                .ItemId = request.ItemId,
                .ResourceId = request.ResourceId,
                .StartsUtc = request.StartsUtc,
                .EndsUtc = request.EndsUtc,
                .Status = BookingStatus.Confirmed,
                .Notes = request.Notes.Trim(),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ToDto(entity As Booking) As BookingDto
            Return New BookingDto With {
                .Id = entity.Id,
                .Number = entity.Number,
                .CustomerId = entity.CustomerId,
                .ItemId = entity.ItemId,
                .ResourceId = entity.ResourceId,
                .StartsUtc = entity.StartsUtc,
                .EndsUtc = entity.EndsUtc,
                .Status = entity.Status,
                .Notes = entity.Notes,
                .CreatedUtc = entity.CreatedUtc
            }
        End Function

        Private Shared Function CreateBookingNumber() As String
            Return $"BK-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}"
        End Function
    End Class
End Namespace
