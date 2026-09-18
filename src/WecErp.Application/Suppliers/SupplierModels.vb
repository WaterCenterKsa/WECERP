Imports WecErp.Domain

Namespace WecErp.Application.Suppliers
    Public Class CreateSupplierRequest
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property TaxNumber As String = String.Empty
    End Class

    Public Class SupplierDto
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property IsActive As Boolean
        Public Property CreatedUtc As DateTimeOffset
    End Class
End Namespace
