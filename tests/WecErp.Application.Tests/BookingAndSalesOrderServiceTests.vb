Imports WecErp.Application.Bookings
Imports WecErp.Application.SalesOrders
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class BookingAndSalesOrderServiceTests
        <Fact>
        Public Sub BookingValidation_RejectsReversedTimeRange()
            Dim service = New BookingService()
            Dim request = New CreateBookingRequest With {
                .CustomerId = Guid.NewGuid(),
                .ItemId = Guid.NewGuid(),
                .StartsUtc = DateTimeOffset.UtcNow.AddHours(2),
                .EndsUtc = DateTimeOffset.UtcNow.AddHours(1)
            }

            Dim errorMessage = service.ValidateNewBooking(request)

            Assert.Equal("EndsUtc must be later than StartsUtc.", errorMessage)
        End Sub

        <Fact>
        Public Sub CreateFromAcceptedQuotation_CopiesCommercialValues()
            Dim service = New SalesOrderService()
            Dim itemId = Guid.NewGuid()
            Dim quotation = New Quotation With {
                .Id = Guid.NewGuid(),
                .CustomerId = Guid.NewGuid(),
                .Status = QuotationStatus.Accepted,
                .CurrencyCode = "SAR",
                .Subtotal = 100D,
                .DiscountAmount = 10D,
                .TaxAmount = 13.5D,
                .Total = 103.5D,
                .Lines = New List(Of QuotationLine) From {
                    New QuotationLine With {
                        .Id = Guid.NewGuid(),
                        .ItemId = itemId,
                        .Description = "Test",
                        .Quantity = 1D,
                        .UnitPrice = 100D,
                        .DiscountPercent = 10D,
                        .TaxPercent = 15D,
                        .LineSubtotal = 100D,
                        .LineDiscount = 10D,
                        .LineTax = 13.5D,
                        .LineTotal = 103.5D
                    }
                }
            }

            Dim order = service.CreateFromAcceptedQuotation(quotation)

            Assert.Equal(quotation.Id, order.SourceQuotationId)
            Assert.Equal(quotation.CustomerId, order.CustomerId)
            Assert.Equal(quotation.Total, order.Total)
            Assert.Equal(SalesOrderStatus.Draft, order.Status)
            Assert.Single(order.Lines)
            Assert.Equal(itemId, order.Lines(0).ItemId)
        End Sub
    End Class
End Namespace
