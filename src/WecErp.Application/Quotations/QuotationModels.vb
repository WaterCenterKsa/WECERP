Imports WecErp.Domain

Namespace WecErp.Application.Quotations
    Public Class QuotationLineRequest
        Public Property ItemId As Guid
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal
        Public Property UnitPrice As Decimal
        Public Property DiscountPercent As Decimal
        Public Property TaxPercent As Decimal
    End Class

    Public Class CreateQuotationRequest
        Public Property CustomerId As Guid
        Public Property CurrencyCode As String = "SAR"
        Public Property ValidUntil As DateTimeOffset?
        Public Property Notes As String = String.Empty
        Public Property Lines As List(Of QuotationLineRequest) = New List(Of QuotationLineRequest)()
    End Class

    Public Class ChangeQuotationStatusRequest
        Public Property Status As String = String.Empty
    End Class

    Public Class QuotationLineDto
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

    Public Class QuotationDto
        Public Property Id As Guid
        Public Property Number As String = String.Empty
        Public Property CustomerId As Guid
        Public Property Status As QuotationStatus
        Public Property CurrencyCode As String = String.Empty
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property Total As Decimal
        Public Property ValidUntil As DateTimeOffset?
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset
        Public Property Lines As List(Of QuotationLineDto) = New List(Of QuotationLineDto)()
    End Class
End Namespace
