Imports WecErp.Application.Quotations
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class QuotationServiceTests
        <Fact>
        Public Sub CreateEntity_CalculatesLineAndTotals()
            Dim service = New QuotationService()
            Dim itemId = Guid.NewGuid()
            Dim customerId = Guid.NewGuid()
            Dim request = New CreateQuotationRequest With {
                .CustomerId = customerId,
                .CurrencyCode = "sar",
                .Lines = New List(Of QuotationLineRequest) From {
                    New QuotationLineRequest With {
                        .ItemId = itemId,
                        .Quantity = 2D,
                        .UnitPrice = 100D,
                        .DiscountPercent = 10D,
                        .TaxPercent = 15D
                    }
                }
            }
            Dim item = New Item With {.Id = itemId, .Sku = "TEST-1", .Name = "Test Item", .Type = ItemType.Product}
            Dim items = New Dictionary(Of Guid, Item) From {{itemId, item}}

            Dim quotation = service.CreateEntity(request, items)

            Assert.Equal(180D, quotation.Subtotal)
            Assert.Equal(20D, quotation.DiscountAmount)
            Assert.Equal(27D, quotation.TaxAmount)
            Assert.Equal(207D, quotation.Total)
            Assert.Equal("SAR", quotation.CurrencyCode)
            Assert.Single(quotation.Lines)
        End Sub

        <Fact>
        Public Sub TryChangeStatus_AllowsDraftToSent()
            Dim service = New QuotationService()
            Dim quotation = New Quotation With {
                .Id = Guid.NewGuid(),
                .Status = QuotationStatus.Draft,
                .ValidUntil = DateTimeOffset.UtcNow.AddDays(7)
            }
            Dim errorMessage = String.Empty

            Dim changed = service.TryChangeStatus(quotation, "Sent", errorMessage)

            Assert.True(changed)
            Assert.Equal(QuotationStatus.Sent, quotation.Status)
            Assert.Empty(errorMessage)
        End Sub

        <Fact>
        Public Sub TryChangeStatus_RejectsTerminalTransition()
            Dim service = New QuotationService()
            Dim quotation = New Quotation With {
                .Id = Guid.NewGuid(),
                .Status = QuotationStatus.Accepted
            }
            Dim errorMessage = String.Empty

            Dim changed = service.TryChangeStatus(quotation, "Cancelled", errorMessage)

            Assert.False(changed)
            Assert.Equal(QuotationStatus.Accepted, quotation.Status)
            Assert.NotEmpty(errorMessage)
        End Sub
    End Class
End Namespace
