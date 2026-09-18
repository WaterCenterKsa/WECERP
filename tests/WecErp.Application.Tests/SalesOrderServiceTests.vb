Imports WecErp.Application.SalesOrders
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class SalesOrderServiceTests
        <Fact>
        Public Sub TryChangeStatus_AllowsDraftToConfirmed()
            Dim service = New SalesOrderService()
            Dim order = New SalesOrder With {.Status = SalesOrderStatus.Draft}
            Dim errorMessage = String.Empty
            Assert.True(service.TryChangeStatus(order, "Confirmed", errorMessage))
            Assert.Equal(SalesOrderStatus.Confirmed, order.Status)
        End Sub

        <Fact>
        Public Sub TryChangeStatus_RejectsFulfilledToCancelled()
            Dim service = New SalesOrderService()
            Dim order = New SalesOrder With {.Status = SalesOrderStatus.Fulfilled}
            Dim errorMessage = String.Empty
            Assert.False(service.TryChangeStatus(order, "Cancelled", errorMessage))
            Assert.Equal(SalesOrderStatus.Fulfilled, order.Status)
        End Sub
    End Class
End Namespace
