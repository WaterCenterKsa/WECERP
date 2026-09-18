Imports System.Security.Cryptography

Namespace WecErp.Application.Identity
    Public Class PasswordHasher
        Private Const Iterations As Integer = 210000
        Private Const SaltSize As Integer = 16
        Private Const HashSize As Integer = 32

        Public Function Hash(password As String) As String
            If String.IsNullOrEmpty(password) Then Throw New ArgumentException("Password is required.", NameOf(password))
            Dim salt(SaltSize - 1) As Byte
            RandomNumberGenerator.Fill(salt)
            Dim derivedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize)
            Return $"PBKDF2-SHA256.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(derivedHash)}"
        End Function

        Public Function Verify(password As String, encoded As String) As Boolean
            If String.IsNullOrEmpty(password) OrElse String.IsNullOrWhiteSpace(encoded) Then Return False
            Dim parts = encoded.Split("."c)
            If parts.Length <> 4 OrElse parts(0) <> "PBKDF2-SHA256" Then Return False
            Dim iterations As Integer
            If Not Integer.TryParse(parts(1), iterations) OrElse iterations < 100000 Then Return False
            Try
                Dim salt = Convert.FromBase64String(parts(2))
                Dim expected = Convert.FromBase64String(parts(3))
                Dim actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length)
                Return CryptographicOperations.FixedTimeEquals(actual, expected)
            Catch
                Return False
            End Try
        End Function
    End Class
End Namespace
