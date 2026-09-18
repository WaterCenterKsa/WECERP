Namespace WecErp.Domain
    Public Enum UserRole
        Administrator = 1
        Manager = 2
        Sales = 3
        Purchasing = 4
        Warehouse = 5
        Service = 6
        Accountant = 7
        Viewer = 8
    End Enum

    Public Class User
        Public Property Id As Guid
        Public Property UserName As String = String.Empty
        Public Property DisplayName As String = String.Empty
        Public Property PasswordHash As String = String.Empty
        Public Property Role As UserRole = UserRole.Viewer
        Public Property IsActive As Boolean = True
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property LastLoginUtc As DateTimeOffset?
    End Class
End Namespace
