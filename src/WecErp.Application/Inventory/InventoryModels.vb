Imports WecErp.Domain

Namespace WecErp.Application.Inventory
    Public Class CreateWarehouseRequest
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
    End Class

    Public Class WarehouseDto
        Public Property Id As Guid
        Public Property Code As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property IsActive As Boolean
        Public Property CreatedUtc As DateTimeOffset
    End Class

    Public Class CreateInventoryMovementRequest
        Public Property ItemId As Guid
        Public Property WarehouseId As Guid
        Public Property Type As InventoryMovementType
        Public Property Quantity As Decimal
        Public Property ReferenceType As String = String.Empty
        Public Property ReferenceId As Guid?
        Public Property Notes As String = String.Empty
        Public Property TransferWarehouseId As Guid?
    End Class

    Public Class InventoryBalanceDto
        Public Property ItemId As Guid
        Public Property WarehouseId As Guid
        Public Property OnHand As Decimal
        Public Property Reserved As Decimal
        Public Property Available As Decimal
    End Class
End Namespace
