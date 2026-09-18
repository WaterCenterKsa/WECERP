Imports WecErp.Domain

Namespace WecErp.Application.Identity
    Public Class UserService
        Private ReadOnly hasher As PasswordHasher

        Public Sub New(hasher As PasswordHasher)
            Me.hasher = hasher
        End Sub

        Public Function ValidateNewUser(request As CreateUserRequest) As String
            If String.IsNullOrWhiteSpace(request.UserName) Then Return "UserName is required."
            If String.IsNullOrWhiteSpace(request.DisplayName) Then Return "DisplayName is required."
            If String.IsNullOrWhiteSpace(request.Password) OrElse request.Password.Length < 12 Then Return "Password must be at least 12 characters."
            If Not [Enum].IsDefined(GetType(UserRole), request.Role) Then Return "Invalid user role."
            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateUserRequest) As User
            Return New User With {
                .Id = Guid.NewGuid(),
                .UserName = request.UserName.Trim(),
                .DisplayName = request.DisplayName.Trim(),
                .PasswordHash = hasher.Hash(request.Password),
                .Role = request.Role,
                .IsActive = True,
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ToDto(entity As User) As UserDto
            Return New UserDto With {
                .Id = entity.Id, .UserName = entity.UserName, .DisplayName = entity.DisplayName,
                .Role = entity.Role, .IsActive = entity.IsActive, .CreatedUtc = entity.CreatedUtc,
                .LastLoginUtc = entity.LastLoginUtc
            }
        End Function
    End Class
End Namespace
