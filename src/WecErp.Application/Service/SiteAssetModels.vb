Imports WecErp.Domain

Namespace WecErp.Application.Service
    Public Class CreateCustomerSiteRequest
        Public Property CustomerId As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property AddressLine As String = String.Empty
        Public Property City As String = String.Empty
        Public Property District As String = String.Empty
        Public Property Notes As String = String.Empty
    End Class

    Public Class CreateServiceAssetRequest
        Public Property CustomerId As Guid
        Public Property SiteId As Guid?
        Public Property AssetNumber As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property SerialNumber As String = String.Empty
        Public Property Model As String = String.Empty
        Public Property InstalledOn As DateOnly?
        Public Property Notes As String = String.Empty
    End Class

    Public Class SiteAssetService
        Public Function ValidateSite(request As CreateCustomerSiteRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If String.IsNullOrWhiteSpace(request.Code) Then Return "Code is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            If request.Code.Trim().Length > 50 Then Return "Code cannot exceed 50 characters."
            If request.Name.Trim().Length > 300 Then Return "Name cannot exceed 300 characters."
            Return String.Empty
        End Function

        Public Function CreateSite(request As CreateCustomerSiteRequest) As CustomerSite
            Return New CustomerSite With {
                .Id = Guid.NewGuid(),
                .CustomerId = request.CustomerId,
                .Code = request.Code.Trim(),
                .Name = request.Name.Trim(),
                .AddressLine = request.AddressLine.Trim(),
                .City = request.City.Trim(),
                .District = request.District.Trim(),
                .Notes = request.Notes.Trim(),
                .IsActive = True,
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ValidateAsset(request As CreateServiceAssetRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If String.IsNullOrWhiteSpace(request.AssetNumber) Then Return "AssetNumber is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            If request.AssetNumber.Trim().Length > 80 Then Return "AssetNumber cannot exceed 80 characters."
            If request.Name.Trim().Length > 300 Then Return "Name cannot exceed 300 characters."
            Return String.Empty
        End Function

        Public Function CreateAsset(request As CreateServiceAssetRequest) As ServiceAsset
            Return New ServiceAsset With {
                .Id = Guid.NewGuid(),
                .CustomerId = request.CustomerId,
                .SiteId = request.SiteId,
                .AssetNumber = request.AssetNumber.Trim(),
                .Name = request.Name.Trim(),
                .SerialNumber = request.SerialNumber.Trim(),
                .Model = request.Model.Trim(),
                .InstalledOn = request.InstalledOn,
                .IsActive = True,
                .Notes = request.Notes.Trim(),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function
    End Class
End Namespace
