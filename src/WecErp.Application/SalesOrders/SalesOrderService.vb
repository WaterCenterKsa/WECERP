Imports WecErp.Domain

Namespace WecErp.Application.SalesOrders
    Public Class SalesOrderService
        Public Function CreateFromAcceptedQuotation(quotation As Quotation) As SalesOrder
            If quotation Is Nothing Then Throw New ArgumentNullException(NameOf(quotation))
            If quotation.Status <> QuotationStatus.Accepted Then
                Throw New InvalidOperationException("Only accepted quotations can be converted to sales orders.")
            End If
            If quotation.Lines Is Nothing OrElse quotation.Lines.Count = 0 Then
                Throw New InvalidOperationException("Quotation must contain at least one line.")
            End If

            Dim order = New SalesOrder With {
                .Id = Guid.NewGuid(),
                .Number = CreateSalesOrderNumber(),
                .CustomerId = quotation.CustomerId,
                .SourceQuotationId = quotation.Id,
                .Status = SalesOrderStatus.Draft,
                .CurrencyCode = quotation.CurrencyCode,
                .Subtotal = quotation.Subtotal,
                .DiscountAmount = quotation.DiscountAmount,
                .TaxAmount = quotation.TaxAmount,
                .Total = quotation.Total,
                .CreatedUtc = DateTimeOffset.UtcNow
            }

            For Each sourceLine In quotation.Lines
                order.Lines.Add(New SalesOrderLine With {
                    .Id = Guid.NewGuid(),
                    .SalesOrderId = order.Id,
                    .ItemId = sourceLine.ItemId,
                    .Description = sourceLine.Description,
                    .Quantity = sourceLine.Quantity,
                    .UnitPrice = sourceLine.UnitPrice,
                    .DiscountPercent = sourceLine.DiscountPercent,
                    .TaxPercent = sourceLine.TaxPercent,
                    .LineSubtotal = sourceLine.LineSubtotal,
                    .LineDiscount = sourceLine.LineDiscount,
                    .LineTax = sourceLine.LineTax,
                    .LineTotal = sourceLine.LineTotal
                })
            Next

            Return order
        End Function

        Public Function ToDto(entity As SalesOrder) As SalesOrderDto
            Return New SalesOrderDto With {
                .Id = entity.Id,
                .Number = entity.Number,
                .CustomerId = entity.CustomerId,
                .SourceQuotationId = entity.SourceQuotationId,
                .Status = entity.Status,
                .CurrencyCode = entity.CurrencyCode,
                .Subtotal = entity.Subtotal,
                .DiscountAmount = entity.DiscountAmount,
                .TaxAmount = entity.TaxAmount,
                .Total = entity.Total,
                .CreatedUtc = entity.CreatedUtc,
                .Lines = entity.Lines.Select(Function(x) New SalesOrderLineDto With {
                    .Id = x.Id,
                    .ItemId = x.ItemId,
                    .Description = x.Description,
                    .Quantity = x.Quantity,
                    .UnitPrice = x.UnitPrice,
                    .DiscountPercent = x.DiscountPercent,
                    .TaxPercent = x.TaxPercent,
                    .LineSubtotal = x.LineSubtotal,
                    .LineDiscount = x.LineDiscount,
                    .LineTax = x.LineTax,
                    .LineTotal = x.LineTotal
                }).ToList()
            }
        End Function

        Private Shared Function CreateSalesOrderNumber() As String
            Return $"SO-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}"
        End Function
    End Class
End Namespace
