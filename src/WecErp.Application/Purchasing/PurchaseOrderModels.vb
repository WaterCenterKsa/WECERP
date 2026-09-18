Imports WecErp.Domain

Namespace WecErp.Application.Purchasing
    Public Class PurchaseOrderLineRequest
        Public Property ItemId As Guid
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal
        Public Property UnitCost As Decimal
        Public Property TaxPercent As Decimal
    End Class

    Public Class CreatePurchaseOrderRequest
        Public Property SupplierId As Guid
        Public Property CurrencyCode As String = "SAR"
        Public Property Notes As String = String.Empty
        Public Property Lines As List(Of PurchaseOrderLineRequest) = New List(Of PurchaseOrderLineRequest)()
    End Class

    Public Class ChangePurchaseOrderStatusRequest
        Public Property Status As String = String.Empty
    End Class

    Public Class ReceivePurchaseOrderRequest
        Public Property WarehouseId As Guid
        Public Property Lines As List(Of ReceivePurchaseOrderLineRequest) = New List(Of ReceivePurchaseOrderLineRequest)()
    End Class

    Public Class ReceivePurchaseOrderLineRequest
        Public Property PurchaseOrderLineId As Guid
        Public Property Quantity As Decimal
    End Class
End Namespace
