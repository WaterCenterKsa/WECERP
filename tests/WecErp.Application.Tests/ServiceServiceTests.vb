Imports WecErp.Application.Service
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class ServiceServiceTests
        <Fact>
        Public Sub ValidateContract_RejectsInvalidFrequency()
            Dim service = New ServiceService()
            Dim request = New CreateServiceContractRequest With {
                .CustomerId = Guid.NewGuid(),
                .StartsOn = New DateOnly(2026, 1, 1),
                .EndsOn = New DateOnly(2026, 12, 31),
                .VisitFrequencyPerWeek = 0
            }
            Assert.NotEmpty(service.ValidateContract(request))
        End Sub

        <Fact>
        Public Sub WorkOrder_RejectsInvalidStatusTransition()
            Dim service = New ServiceService()
            Dim order = New WorkOrder With {.Status = WorkOrderStatus.Completed}
            Dim errorMessage = String.Empty
            Assert.False(service.TryChangeWorkOrderStatus(order, "Open", errorMessage))
        End Sub
    End Class
End Namespace
