Namespace WecErp.Domain
    Public Enum ResourceType
        Employee = 1
        Technician = 2
        Room = 3
        Vehicle = 4
        Equipment = 5
        Other = 6
    End Enum

    Public Class Resource
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Type As ResourceType
        Public Property IsActive As Boolean = True
    End Class
End Namespace
