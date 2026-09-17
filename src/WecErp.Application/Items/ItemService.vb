Imports WecErp.Domain

Namespace WecErp.Application.Items
    Public Class ItemService
        Public Function ValidateNewItem(request As CreateItemRequest) As String
            If request Is Nothing Then Return "Request is required."
            If String.IsNullOrWhiteSpace(request.Sku) Then Return "SKU is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Item name is required."
            If request.Sku.Trim().Length > 100 Then Return "SKU cannot exceed 100 characters."
            If request.Name.Trim().Length > 300 Then Return "Item name cannot exceed 300 characters."
            If Not [Enum].IsDefined(GetType(ItemType), request.Type) Then Return "Invalid item type."
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateItemRequest) As Item
            Return New Item With {
                .Id = Guid.NewGuid(),
                .Sku = request.Sku.Trim(),
                .Name = request.Name.Trim(),
                .Type = request.Type,
                .IsActive = True
            }
        End Function

        Public Function ToDto(item As Item) As ItemDto
            Return New ItemDto With {
                .Id = item.Id,
                .Sku = item.Sku,
                .Name = item.Name,
                .Type = item.Type,
                .IsActive = item.IsActive
            }
        End Function
    End Class
End Namespace
