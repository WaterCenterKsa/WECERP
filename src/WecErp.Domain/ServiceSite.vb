Namespace WecErp.Domain
    Public Class CustomerSite
        Public Property Id As Guid
        Public Property CustomerId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property AddressLine As String = String.Empty
        Public Property City As String = String.Empty
        Public Property District As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class

    Public Class ServiceAsset
        Public Property Id As Guid
        Public Property CustomerId As Guid
        Public Property SiteId As Guid?
        Public Property AssetNumber As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property SerialNumber As String = String.Empty
        Public Property Model As String = String.Empty
        Public Property InstalledOn As DateOnly?
        Public Property IsActive As Boolean = True
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
