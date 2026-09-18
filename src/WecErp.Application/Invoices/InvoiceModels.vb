Imports WecErp.Domain

Namespace WecErp.Application.Invoices
    Public Class CreateInvoiceFromSalesOrderRequest
        Public Property SalesOrderId As Guid
    End Class

    Public Class RecordPaymentRequest
        Public Property Amount As Decimal
        Public Property Method As String = String.Empty
        Public Property Reference As String = String.Empty
    End Class
End Namespace
