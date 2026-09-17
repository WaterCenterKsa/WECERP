Namespace WecErp.Domain
    Public Class SalesOrder
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property SourceQuotationId As Guid?
        Public Property Status As SalesOrderStatus = SalesOrderStatus.Draft
        Public Property CurrencyCode As String = "SAR"
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Lines As List(Of SalesOrderLine) = New List(Of SalesOrderLine)()
    End Class

    Public Class SalesOrderLine
        Public Property Id As Guid
        Public Property SalesOrderId As Guid
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
End Namespace
