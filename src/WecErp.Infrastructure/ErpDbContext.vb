Imports Microsoft.EntityFrameworkCore
Imports WecErp.Domain

Namespace WecErp.Infrastructure
    Public Class ErpDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of ErpDbContext))
            MyBase.New(options)
        End Sub

        Public Property Items As DbSet(Of Item)

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            MyBase.OnModelCreating(modelBuilder)

            Dim item = modelBuilder.Entity(Of Item)()
            item.ToTable("Items")
            item.HasKey(Function(x) x.Id)
            item.Property(Function(x) x.Sku).HasMaxLength(100).IsRequired()
            item.Property(Function(x) x.Name).HasMaxLength(300).IsRequired()
            item.HasIndex(Function(x) x.Sku).IsUnique()
            item.Property(Function(x) x.Type).IsRequired()
            item.Property(Function(x) x.IsActive).IsRequired()
        End Sub
    End Class
End Namespace
