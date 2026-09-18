Imports WecErp.Application.Invoices
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class InvoiceServiceTests
        <Fact>
        Public Sub CreateFromSalesOrder_RequiresFulfilledOrder()
            Dim service = New InvoiceService()
            Dim order = New SalesOrder With {.Status = SalesOrderStatus.Confirmed}

            Assert.Throws(Of InvalidOperationException)(Function() service.CreateFromSalesOrder(order))
        End Sub
    End Class
End Namespace
