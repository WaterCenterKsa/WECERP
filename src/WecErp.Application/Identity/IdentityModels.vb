Imports WecErp.Domain

Namespace WecErp.Application.Identity
    Public Class LoginRequest
        Public Property UserName As String = String.Empty
        Public Property Password As String = String.Empty
    End Class

    Public Class CreateUserRequest
        Public Property UserName As String = String.Empty
        Public Property DisplayName As String = String.Empty
        Public Property Password As String = String.Empty
        Public Property Role As UserRole = UserRole.Viewer
    End Class

    Public Class UserDto
        Public Property Id As Guid
        Public Property UserName As String = String.Empty
        Public Property DisplayName As String = String.Empty
        Public Property Role As UserRole
        Public Property IsActive As Boolean
        Public Property CreatedUtc As DateTimeOffset
        Public Property LastLoginUtc As DateTimeOffset?
    End Class
End Namespace
