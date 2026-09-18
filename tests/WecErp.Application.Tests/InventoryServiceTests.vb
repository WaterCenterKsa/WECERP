Imports WecErp.Application.Inventory
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class InventoryServiceTests
        <Fact>
        Public Sub ValidateMovement_RejectsNonPositiveQuantity()
            Dim service = New InventoryService()
            Dim request = New CreateInventoryMovementRequest With {
                .ItemId = Guid.NewGuid(),
                .WarehouseId = Guid.NewGuid(),
                .Type = InventoryMovementType.Receipt,
                .Quantity = 0D
            }

            Assert.Equal("Quantity must be greater than zero.", service.ValidateMovement(request))
        End Sub

        <Fact>
        Public Sub ValidateMovement_RequiresTransferWarehouse()
            Dim service = New InventoryService()
            Dim request = New CreateInventoryMovementRequest With {
                .ItemId = Guid.NewGuid(),
                .WarehouseId = Guid.NewGuid(),
                .Type = InventoryMovementType.TransferOut,
                .Quantity = 2D
            }

            Assert.Equal("TransferWarehouseId is required for transfers.", service.ValidateMovement(request))
        End Sub

        <Fact>
        Public Sub CreateMovement_RoundsQuantity()
            Dim service = New InventoryService()
            Dim warehouseId = Guid.NewGuid()
            Dim request = New CreateInventoryMovementRequest With {
                .ItemId = Guid.NewGuid(),
                .WarehouseId = warehouseId,
                .Type = InventoryMovementType.Receipt,
                .Quantity = 1.23456D
            }

            Dim movement = service.CreateMovement(request, InventoryMovementType.Receipt, warehouseId)

            Assert.Equal(1.2346D, movement.Quantity)
            Assert.Equal(warehouseId, movement.WarehouseId)
        End Sub
    End Class
End Namespace
