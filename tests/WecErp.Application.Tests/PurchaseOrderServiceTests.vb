Imports WecErp.Application.Purchasing
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class PurchaseOrderServiceTests
        <Fact>
        Public Sub CreateEntity_CalculatesTotals()
            Dim itemId = Guid.NewGuid()
            Dim service = New PurchaseOrderService()
            Dim request = New CreatePurchaseOrderRequest With {
                .SupplierId = Guid.NewGuid(),
                .Lines = New List(Of PurchaseOrderLineRequest) From {
                    New PurchaseOrderLineRequest With {.ItemId = itemId, .Quantity = 2D, .UnitCost = 100D, .TaxPercent = 15D}
                }
            }

            Dim order = service.CreateEntity(request, New Dictionary(Of Guid, Item) From {
                {itemId, New Item With {.Id = itemId, .Name = "Test"}}
            })

            Assert.Equal(200D, order.Subtotal)
            Assert.Equal(30D, order.TaxAmount)
            Assert.Equal(230D, order.Total)
        End Sub

        <Fact>
        Public Sub TryChangeStatus_AllowsDraftToConfirmed()
            Dim service = New PurchaseOrderService()
            Dim order = New PurchaseOrder With {.Status = PurchaseOrderStatus.Draft}
            Dim errorMessage = String.Empty

            Assert.True(service.TryChangeStatus(order, "Confirmed", errorMessage))
            Assert.Equal(PurchaseOrderStatus.Confirmed, order.Status)
        End Sub
    End Class
End Namespace
