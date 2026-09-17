Namespace WecErp.Domain
    Public Class Customer
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
