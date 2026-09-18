Imports WecErp.Application.Suppliers
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class SupplierServiceTests
        <Fact>
        Public Sub ValidateNewSupplier_RequiresCodeAndName()
            Dim service = New SupplierService()
            Assert.NotEmpty(service.ValidateNewSupplier(New CreateSupplierRequest()))
        End Sub
    End Class
End Namespace
