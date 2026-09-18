Imports WecErp.Domain

Namespace WecErp.Application.SalesOrders
    Public Class SalesOrderLineDto
        Public Property Id As Guid
        Public Property ItemId As Guid
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal
        Public Property UnitPrice As Decimal
        Public Property DiscountPercent As Decimal
        Public Property TaxPercent As Decimal
        Public Property LineSubtotal As Decimal
        Public Property LineDiscount As Decimal
        Public Property LineTax As Decimal
        Public Property LineTotal As Decimal
    End Class

    Public Class SalesOrderDto
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property SourceQuotationId As Guid?
        Public Property Status As SalesOrderStatus
        Public Property CurrencyCode As String = String.Empty
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property CreatedUtc As DateTimeOffset
        Public Property Lines As List(Of SalesOrderLineDto) = New List(Of SalesOrderLineDto)()
    End Class
    Public Class ChangeSalesOrderStatusRequest
        Public Property Status As String = String.Empty
    End Class

    Public Class FulfillSalesOrderRequest
        Public Property WarehouseId As Guid?
    End Class
End Namespace
