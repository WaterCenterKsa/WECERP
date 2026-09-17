Namespace WecErp.Domain
    Public Class Quotation
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property Status As QuotationStatus = QuotationStatus.Draft
        Public Property CurrencyCode As String = "SAR"
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property ValidUntil As DateTimeOffset?
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property Lines As List(Of QuotationLine) = New List(Of QuotationLine)()
    End Class

    Public Class QuotationLine
        Public Property Id As Guid
        Public Property QuotationId As Guid
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
