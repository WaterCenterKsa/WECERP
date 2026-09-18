Imports WecErp.Domain

Namespace WecErp.Application.Inventory
    Public Class InventoryService
        Public Function ValidateWarehouse(request As CreateWarehouseRequest) As String
            If String.IsNullOrWhiteSpace(request.Code) Then Return "Code is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            Return String.Empty
        End Function

        Public Function ValidateMovement(request As CreateInventoryMovementRequest) As String
            If request.ItemId = Guid.Empty Then Return "ItemId is required."
            If request.WarehouseId = Guid.Empty Then Return "WarehouseId is required."
            If request.Quantity <= 0D Then Return "Quantity must be greater than zero."
            If request.Type = InventoryMovementType.TransferOut OrElse request.Type = InventoryMovementType.TransferIn Then
                If Not request.TransferWarehouseId.HasValue OrElse request.TransferWarehouseId.Value = Guid.Empty Then
                    Return "TransferWarehouseId is required for transfers."
                End If
                If request.TransferWarehouseId.Value = request.WarehouseId Then
                    Return "Transfer source and destination warehouses must be different."
                End If
            End If
            Return String.Empty
        End Function

        Public Function CreateMovement(request As CreateInventoryMovementRequest,
                                        movementType As InventoryMovementType,
                                        warehouseId As Guid) As InventoryMovement
            Return New InventoryMovement With {
                .Id = Guid.NewGuid(),
                .ItemId = request.ItemId,
                .WarehouseId = warehouseId,
                .Type = movementType,
                .Quantity = Decimal.Round(request.Quantity, 4, MidpointRounding.AwayFromZero),
                .ReferenceType = request.ReferenceType.Trim(),
                .ReferenceId = request.ReferenceId,
                .Notes = request.Notes.Trim(),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function
    End Class
End Namespace
