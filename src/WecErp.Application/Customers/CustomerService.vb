Imports WecErp.Domain

Namespace WecErp.Application.Customers
    Public Class CustomerService
        Public Function ValidateNewCustomer(request As CreateCustomerRequest) As String
            If request Is Nothing Then Return "Request is required."
            If String.IsNullOrWhiteSpace(request.Code) Then Return "Customer code is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Customer name is required."
            If request.Code.Trim().Length > 50 Then Return "Customer code cannot exceed 50 characters."
            If request.Name.Trim().Length > 300 Then Return "Customer name cannot exceed 300 characters."
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateCustomerRequest) As Customer
            Return New Customer With {
                .Id = Guid.NewGuid(),
                .Code = request.Code.Trim(),
                .Name = request.Name.Trim(),
                .Phone = If(request.Phone, String.Empty).Trim(),
                .Email = If(request.Email, String.Empty).Trim(),
                .TaxNumber = If(request.TaxNumber, String.Empty).Trim(),
                .IsActive = True,
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ToDto(customer As Customer) As CustomerDto
            Return New CustomerDto With {
                .Id = customer.Id,
                .Code = customer.Code,
                .Name = customer.Name,
                .Phone = customer.Phone,
                .Email = customer.Email,
                .TaxNumber = customer.TaxNumber,
                .IsActive = customer.IsActive,
                .CreatedUtc = customer.CreatedUtc
            }
        End Function
    End Class
End Namespace
