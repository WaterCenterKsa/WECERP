Imports WecErp.Application.Identity
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class PasswordHasherTests
        <Fact>
        Public Sub Hash_VerifiesOriginalPassword()
            Dim hasher = New PasswordHasher()
            Dim encoded = hasher.Hash("A-strong-test-password-123!")

            Assert.True(hasher.Verify("A-strong-test-password-123!", encoded))
            Assert.False(hasher.Verify("wrong-password", encoded))
        End Sub
    End Class
End Namespace
