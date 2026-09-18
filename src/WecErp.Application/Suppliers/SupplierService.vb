Imports WecErp.Domain

Namespace WecErp.Application.Suppliers
    Public Class SupplierService
        Public Function ValidateNewSupplier(request As CreateSupplierRequest) As String
            If String.IsNullOrWhiteSpace(request.Code) Then Return "Code is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateSupplierRequest) As Supplier
            Return New Supplier With {
                .Id = Guid.NewGuid(),
                .Code = request.Code.Trim(),
                .Name = request.Name.Trim(),
                .Phone = request.Phone.Trim(),
                .Email = request.Email.Trim(),
                .TaxNumber = request.TaxNumber.Trim(),
                .IsActive = True,
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ToDto(entity As Supplier) As SupplierDto
            Return New SupplierDto With {
                .Id = entity.Id, .Code = entity.Code, .Name = entity.Name,
                .Phone = entity.Phone, .Email = entity.Email, .TaxNumber = entity.TaxNumber,
                .IsActive = entity.IsActive, .CreatedUtc = entity.CreatedUtc
            }
        End Function
    End Class
End Namespace
