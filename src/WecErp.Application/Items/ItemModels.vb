Imports WecErp.Domain

Namespace WecErp.Application.Items
    Public Class ItemDto
        Public Property Id As Guid
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Type As ItemType
        Public Property IsActive As Boolean
    End Class

    Public Class CreateItemRequest
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Type As ItemType
    End Class
End Namespace
