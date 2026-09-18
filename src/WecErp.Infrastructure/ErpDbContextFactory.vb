Imports System
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace WecErp.Infrastructure
    Public Class ErpDbContextFactory
        Implements IDesignTimeDbContextFactory(Of ErpDbContext)

        Public Function CreateDbContext(args As String()) As ErpDbContext Implements IDesignTimeDbContextFactory(Of ErpDbContext).CreateDbContext
            Dim connectionString = Environment.GetEnvironmentVariable("WECERP_DESIGN_CONNECTION")
            If String.IsNullOrWhiteSpace(connectionString) Then
                connectionString = "Server=(localdb)\MSSQLLocalDB;Database=WecErp_Dev;Trusted_Connection=True;TrustServerCertificate=True"
            End If

            Dim options = New DbContextOptionsBuilder(Of ErpDbContext)().
                UseSqlServer(connectionString).
                Options

            Return New ErpDbContext(options)
        End Function
    End Class
End Namespace
