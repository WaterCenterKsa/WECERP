Namespace WecErp.Domain
    Public Class Item
        Public Property Id As Guid
        Public Property Sku As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Type As ItemType
        Public Property IsActive As Boolean = True
    End Class
End Namespace
