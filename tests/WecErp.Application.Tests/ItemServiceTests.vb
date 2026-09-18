Imports WecErp.Application.Items
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class ItemServiceTests
        <Fact>
        Public Sub ValidateNewItem_RejectsInvalidType()
            Dim service = New ItemService()

            Dim errorMessage = service.ValidateNewItem(New CreateItemRequest With {
                .Sku = "SKU-001",
                .Name = "Test Product",
                .Type = CType(999, ItemType)
            })

            Assert.Equal("Invalid item type.", errorMessage)
        End Sub

        <Fact>
        Public Sub CreateEntity_TrimsFieldsAndActivatesItem()
            Dim service = New ItemService()

            Dim item = service.CreateEntity(New CreateItemRequest With {
                .Sku = " SKU-001 ",
                .Name = " Test Product ",
                .Type = ItemType.Product
            })

            Assert.NotEqual(Guid.Empty, item.Id)
            Assert.Equal("SKU-001", item.Sku)
            Assert.Equal("Test Product", item.Name)
            Assert.Equal(ItemType.Product, item.Type)
            Assert.True(item.IsActive)
        End Sub
    End Class
End Namespace
