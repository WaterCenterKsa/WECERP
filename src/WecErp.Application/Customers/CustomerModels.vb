Namespace WecErp.Application.Customers
    Public Class CustomerDto
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property IsActive As Boolean
        Public Property CreatedUtc As DateTimeOffset
    End Class

    Public Class CreateCustomerRequest
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property TaxNumber As String = String.Empty
    End Class
End Namespace
