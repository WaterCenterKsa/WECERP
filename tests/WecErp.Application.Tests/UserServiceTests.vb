Imports WecErp.Application.Identity
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class UserServiceTests
        <Fact>
        Public Sub ValidateNewUser_RejectsShortPassword()
            Dim service = New UserService(New PasswordHasher())

            Dim errorMessage = service.ValidateNewUser(New CreateUserRequest With {
                .UserName = "tester",
                .DisplayName = "Test User",
                .Password = "short",
                .Role = UserRole.Viewer
            })

            Assert.Equal("Password must be at least 12 characters.", errorMessage)
        End Sub

        <Fact>
        Public Sub CreateEntity_HashesPasswordAndPreservesRole()
            Dim password = "A-strong-test-password-123!"
            Dim hasher = New PasswordHasher()
            Dim service = New UserService(hasher)

            Dim user = service.CreateEntity(New CreateUserRequest With {
                .UserName = " tester ",
                .DisplayName = " Test User ",
                .Password = password,
                .Role = UserRole.Viewer
            })

            Assert.NotEqual(Guid.Empty, user.Id)
            Assert.Equal("tester", user.UserName)
            Assert.Equal("Test User", user.DisplayName)
            Assert.Equal(UserRole.Viewer, user.Role)
            Assert.True(user.IsActive)
            Assert.NotEqual(password, user.PasswordHash)
            Assert.True(hasher.Verify(password, user.PasswordHash))
        End Sub
    End Class
End Namespace
