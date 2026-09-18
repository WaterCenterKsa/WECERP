Imports WecErp.Application.Customers
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class CustomerServiceTests
        <Fact>
        Public Sub ValidateNewCustomer_RejectsMissingCode()
            Dim service = New CustomerService()

            Dim errorMessage = service.ValidateNewCustomer(New CreateCustomerRequest With {
                .Code = "",
                .Name = "Test Customer"
            })

            Assert.Equal("Customer code is required.", errorMessage)
        End Sub

        <Fact>
        Public Sub CreateEntity_TrimsFieldsAndActivatesCustomer()
            Dim service = New CustomerService()

            Dim customer = service.CreateEntity(New CreateCustomerRequest With {
                .Code = " C-001 ",
                .Name = " Test Customer ",
                .Phone = " 0500000000 ",
                .Email = " test@example.com ",
                .TaxNumber = " 300000000000003 "
            })

            Assert.NotEqual(Guid.Empty, customer.Id)
            Assert.Equal("C-001", customer.Code)
            Assert.Equal("Test Customer", customer.Name)
            Assert.Equal("0500000000", customer.Phone)
            Assert.Equal("test@example.com", customer.Email)
            Assert.Equal("300000000000003", customer.TaxNumber)
            Assert.True(customer.IsActive)
        End Sub
    End Class
End Namespace
