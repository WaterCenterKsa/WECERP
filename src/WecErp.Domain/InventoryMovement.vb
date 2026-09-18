Namespace WecErp.Domain
    Public Enum InventoryMovementType
        Receipt = 1
        Issue = 2
        TransferOut = 3
        TransferIn = 4
        AdjustmentIncrease = 5
        AdjustmentDecrease = 6
        Reservation = 7
        ReleaseReservation = 8
    End Enum

    Public Class InventoryMovement
        Public Property Id As Guid
        Public Property ItemId As Guid
        Public Property WarehouseId As Guid
        Public Property Type As InventoryMovementType
        Public Property Quantity As Decimal
        Public Property ReferenceType As String = String.Empty
        Public Property ReferenceId As Guid?
        Public Property Notes As String = String.Empty
        Public Property CreatedUtc As DateTimeOffset = DateTimeOffset.UtcNow
    End Class
End Namespace
