Imports Microsoft.EntityFrameworkCore
Imports WecErp.Domain

Namespace WecErp.Infrastructure
    Public Class ErpDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of ErpDbContext))
            MyBase.New(options)
        End Sub

        Public Property Items As DbSet(Of Item)
        Public Property Customers As DbSet(Of Customer)

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

            Dim customer = modelBuilder.Entity(Of Customer)()
            customer.ToTable("Customers")
            customer.HasKey(Function(x) x.Id)
            customer.Property(Function(x) x.Code).HasMaxLength(50).IsRequired()
            customer.Property(Function(x) x.Name).HasMaxLength(300).IsRequired()
            customer.Property(Function(x) x.Phone).HasMaxLength(50)
            customer.Property(Function(x) x.Email).HasMaxLength(320)
            customer.Property(Function(x) x.TaxNumber).HasMaxLength(50)
            customer.Property(Function(x) x.IsActive).IsRequired()
            customer.Property(Function(x) x.CreatedUtc).IsRequired()
            customer.HasIndex(Function(x) x.Code).IsUnique()
        End Sub
    End Class
End Namespace
