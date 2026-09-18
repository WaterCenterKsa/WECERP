Namespace WecErp.Domain
    Public Enum InvoiceStatus
        Draft = 1
        Issued = 2
        PartiallyPaid = 3
        Paid = 4
        Void = 5
    End Enum

    Public Class Invoice
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property SourceSalesOrderId As Guid?
        Public Property Status As InvoiceStatus = InvoiceStatus.Draft
        Public Property CurrencyCode As String = "SAR"
        Public Property Subtotal As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property PaidAmount As Decimal
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Lines As List(Of InvoiceLine) = New List(Of InvoiceLine)()
    End Class

    Public Class InvoiceLine
        Public Property Id As Guid
        Public Property InvoiceId As Guid
        Public Property ItemId As Guid
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal
        Public Property UnitPrice As Decimal
        Public Property TaxPercent As Decimal
        Public Property LineSubtotal As Decimal
        Public Property LineTax As Decimal
        Public Property LineTotal As Decimal
    End Class

    Public Class Payment
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property InvoiceId As Guid
        Public Property Amount As Decimal
        Public Property Method As String = String.Empty
        Public Property Reference As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
