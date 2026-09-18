Imports WecErp.Application.Service
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class SiteAssetServiceTests
        <Fact>
        Public Sub CreateSite_PreservesCustomerAndNormalizesFields()
            Dim service = New SiteAssetService()
            Dim customerId = Guid.NewGuid()
            Dim site = service.CreateSite(New CreateCustomerSiteRequest With {
                .CustomerId = customerId,
                .Code = "  SITE-01 ",
                .Name = "  Villa Pool ",
                .City = " Jeddah "
            })

            Assert.Equal(customerId, site.CustomerId)
            Assert.Equal("SITE-01", site.Code)
            Assert.Equal("Villa Pool", site.Name)
            Assert.Equal("Jeddah", site.City)
            Assert.True(site.IsActive)
        End Sub

        <Fact>
        Public Sub ValidateAsset_RejectsMissingAssetNumber()
            Dim service = New SiteAssetService()
            Dim errorMessage = service.ValidateAsset(New CreateServiceAssetRequest With {
                .CustomerId = Guid.NewGuid(),
                .Name = "Pump"
            })

            Assert.Equal("AssetNumber is required.", errorMessage)
        End Sub

        <Fact>
        Public Sub CreateAsset_PreservesOptionalSite()
            Dim service = New SiteAssetService()
            Dim customerId = Guid.NewGuid()
            Dim siteId = Guid.NewGuid()
            Dim asset = service.CreateAsset(New CreateServiceAssetRequest With {
                .CustomerId = customerId,
                .SiteId = siteId,
                .AssetNumber = "ASSET-001",
                .Name = "Pool Pump",
                .SerialNumber = " SN-1 "
            })

            Assert.Equal(customerId, asset.CustomerId)
            Assert.Equal(siteId, asset.SiteId)
            Assert.Equal("SN-1", asset.SerialNumber)
            Assert.False(asset.InstalledOn.HasValue)
        End Sub
    End Class
End Namespace
