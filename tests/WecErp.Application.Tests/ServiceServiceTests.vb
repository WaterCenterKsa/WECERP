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

        <Fact>
        Public Sub GenerateScheduledWorkOrders_UsesContractFrequency()
            Dim service = New ServiceService()
            Dim contract = New ServiceContract With {
                .Id = Guid.NewGuid(),
                .CustomerId = Guid.NewGuid(),
                .Status = ServiceContractStatus.Active,
                .StartsOn = New DateOnly(2026, 1, 1),
                .EndsOn = New DateOnly(2026, 1, 8),
                .VisitFrequencyPerWeek = 2
            }
            Dim errorMessage = String.Empty
            Dim request = New GenerateWorkOrdersRequest With {
                .FirstVisitUtc = New DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
                .DurationMinutes = 60
            }

            Dim orders = service.GenerateScheduledWorkOrders(contract, request, errorMessage)

            Assert.Empty(errorMessage)
            Assert.Equal(3, orders.Count)
            Assert.All(orders, Sub(x)
                                   Assert.Equal(contract.Id, x.ServiceContractId)
                                   Assert.Equal(WorkOrderStatus.Scheduled, x.Status)
                               End Sub)
        End Sub

        <Fact>
        Public Sub GenerateScheduledWorkOrders_RejectsInactiveContract()
            Dim service = New ServiceService()
            Dim contract = New ServiceContract With {.Status = ServiceContractStatus.Draft}
            Dim errorMessage = String.Empty

            Dim orders = service.GenerateScheduledWorkOrders(contract, New GenerateWorkOrdersRequest With {
                .FirstVisitUtc = New DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero)
            }, errorMessage)

            Assert.Empty(orders)
            Assert.NotEmpty(errorMessage)
        End Sub
    End Class
End Namespace
