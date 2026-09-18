Imports WecErp.Domain

Namespace WecErp.Application.Invoices
    Public Class InvoiceService
        Public Function CreateFromSalesOrder(order As SalesOrder) As Invoice
            If order Is Nothing Then Throw New ArgumentNullException(NameOf(order))
            If order.Status <> SalesOrderStatus.Fulfilled Then Throw New InvalidOperationException("Only fulfilled sales orders can be invoiced.")
            If order.Lines Is Nothing OrElse order.Lines.Count = 0 Then Throw New InvalidOperationException("Sales order must contain at least one line.")

            Dim invoice = New Invoice With {
                .Id = Guid.NewGuid(),
                .Number = $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                .CustomerId = order.CustomerId,
                .SourceSalesOrderId = order.Id,
                .Status = InvoiceStatus.Issued,
                .CurrencyCode = order.CurrencyCode,
                .Subtotal = order.Subtotal,
                .TaxAmount = order.TaxAmount,
                .Total = order.Total,
                .PaidAmount = 0D,
                .CreatedUtc = DateTimeOffset.UtcNow
            }

            For Each sourceLine In order.Lines
                invoice.Lines.Add(New InvoiceLine With {
                    .Id = Guid.NewGuid(),
                    .InvoiceId = invoice.Id,
                    .ItemId = sourceLine.ItemId,
                    .Description = sourceLine.Description,
                    .Quantity = sourceLine.Quantity,
                    .UnitPrice = sourceLine.UnitPrice,
                    .TaxPercent = sourceLine.TaxPercent,
                    .LineSubtotal = sourceLine.LineSubtotal,
                    .LineTax = sourceLine.LineTax,
                    .LineTotal = sourceLine.LineTotal
                })
            Next
            Return invoice
        End Function
    End Class
End Namespace
