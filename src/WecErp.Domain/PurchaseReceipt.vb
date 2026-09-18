Namespace WecErp.Domain
    Public Class PurchaseReceipt
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property PurchaseOrderId As Guid
        Public Property WarehouseId As Guid
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Lines As List(Of PurchaseReceiptLine) = New List(Of PurchaseReceiptLine)()
    End Class

    Public Class PurchaseReceiptLine
        Public Property Id As Guid
        Public Property PurchaseReceiptId As Guid
        Public Property PurchaseOrderLineId As Guid
        Public Property ItemId As Guid
        Public Property Quantity As Decimal
    End Class
End Namespace
