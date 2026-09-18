Namespace WecErp.Domain
    Public Class AuditLog
        Public Property Id As Guid
        Public Property UserId As Guid?
        Public Property UserName As String = String.Empty
        Public Property Action As String = String.Empty
        Public Property Path As String = String.Empty
        Public Property Method As String = String.Empty
        Public Property StatusCode As Integer
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
