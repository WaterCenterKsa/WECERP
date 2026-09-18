Imports WecErp.Domain

Namespace WecErp.Application.Purchasing
    Public Class PurchaseOrderService
        Public Function ValidateNewOrder(request As CreatePurchaseOrderRequest) As String
            If request.SupplierId = Guid.Empty Then Return "SupplierId is required."
            If String.IsNullOrWhiteSpace(request.CurrencyCode) OrElse request.CurrencyCode.Trim().Length <> 3 Then Return "CurrencyCode must be a 3-letter ISO code."
            If request.Lines Is Nothing OrElse request.Lines.Count = 0 Then Return "At least one purchase order line is required."
            For Each line In request.Lines
                If line.ItemId = Guid.Empty Then Return "Each line requires an ItemId."
                If line.Quantity <= 0D Then Return "Each line quantity must be greater than zero."
                If line.UnitCost < 0D Then Return "UnitCost cannot be negative."
                If line.TaxPercent < 0D OrElse line.TaxPercent > 100D Then Return "TaxPercent must be between 0 and 100."
            Next
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreatePurchaseOrderRequest, items As IReadOnlyDictionary(Of Guid, Item)) As PurchaseOrder
            Dim order = New PurchaseOrder With {
                .Id = Guid.NewGuid(),
                .Number = $"PO-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                .SupplierId = request.SupplierId,
                .CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
                .Notes = request.Notes.Trim(),
                .Status = PurchaseOrderStatus.Draft,
                .CreatedUtc = DateTimeOffset.UtcNow
            }

            For Each line In request.Lines
                Dim item = items(line.ItemId)
                Dim subtotal = Decimal.Round(line.Quantity * line.UnitCost, 2, MidpointRounding.AwayFromZero)
                Dim tax = Decimal.Round(subtotal * line.TaxPercent / 100D, 2, MidpointRounding.AwayFromZero)
                order.Lines.Add(New PurchaseOrderLine With {
                    .Id = Guid.NewGuid(),
                    .PurchaseOrderId = order.Id,
                    .ItemId = item.Id,
                    .Description = If(String.IsNullOrWhiteSpace(line.Description), item.Name, line.Description.Trim()),
                    .OrderedQuantity = line.Quantity,
                    .UnitCost = line.UnitCost,
                    .TaxPercent = line.TaxPercent,
                    .LineSubtotal = subtotal,
                    .LineTax = tax,
                    .LineTotal = subtotal + tax
                })
            Next

            order.Subtotal = Decimal.Round(order.Lines.Sum(Function(x) x.LineSubtotal), 2, MidpointRounding.AwayFromZero)
            order.TaxAmount = Decimal.Round(order.Lines.Sum(Function(x) x.LineTax), 2, MidpointRounding.AwayFromZero)
            order.Total = Decimal.Round(order.Lines.Sum(Function(x) x.LineTotal), 2, MidpointRounding.AwayFromZero)
            Return order
        End Function

        Public Function TryChangeStatus(order As PurchaseOrder, requestedStatus As String, ByRef errorMessage As String) As Boolean
            errorMessage = String.Empty
            If order Is Nothing Then errorMessage = "Purchase order is required.": Return False
            Dim target As PurchaseOrderStatus
            If Not [Enum].TryParse(requestedStatus.Trim(), True, target) OrElse Not [Enum].IsDefined(GetType(PurchaseOrderStatus), target) Then
                errorMessage = "Invalid purchase order status.": Return False
            End If
            If order.Status = target Then errorMessage = "Purchase order is already in this status.": Return False

            Dim allowed = (order.Status = PurchaseOrderStatus.Draft AndAlso target = PurchaseOrderStatus.Confirmed) OrElse
                          (order.Status = PurchaseOrderStatus.Draft AndAlso target = PurchaseOrderStatus.Cancelled) OrElse
                          (order.Status = PurchaseOrderStatus.Confirmed AndAlso target = PurchaseOrderStatus.Cancelled) OrElse
                          (order.Status = PurchaseOrderStatus.PartiallyReceived AndAlso target = PurchaseOrderStatus.Cancelled)
            If Not allowed Then
                errorMessage = $"Purchase order cannot move from {order.Status} to {target}."
                Return False
            End If
            order.Status = target
            Return True
        End Function
    End Class
End Namespace
