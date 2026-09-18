Namespace WecErp.Domain
    Public Enum PurchaseOrderStatus
        Draft = 1
        Confirmed = 2
        PartiallyReceived = 3
        FullyReceived = 4
        Cancelled = 5
    End Enum

    Public Class PurchaseOrder
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property SupplierId As Guid
        Public Property Status As PurchaseOrderStatus = PurchaseOrderStatus.Draft
        Public Property CurrencyCode As String = "SAR"
        Public Property Subtotal As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Lines As List(Of PurchaseOrderLine) = New List(Of PurchaseOrderLine)()
    End Class

    Public Class PurchaseOrderLine
        Public Property Id As Guid
        Public Property PurchaseOrderId As Guid
        Public Property ItemId As Guid
        Public Property Description As String = String.Empty
        Public Property OrderedQuantity As Decimal
        Public Property UnitCost As Decimal
        Public Property TaxPercent As Decimal
        Public Property LineSubtotal As Decimal
        Public Property LineTax As Decimal
        Public Property LineTotal As Decimal
    End Class
End Namespace
