Imports System.Globalization
Imports WecErp.Domain

Namespace WecErp.Application.Quotations
    Public Class QuotationService
        Public Function ValidateNewQuotation(request As CreateQuotationRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If request.Lines Is Nothing OrElse request.Lines.Count = 0 Then Return "At least one quotation line is required."
            If String.IsNullOrWhiteSpace(request.CurrencyCode) Then Return "CurrencyCode is required."
            If request.ValidUntil.HasValue AndAlso request.ValidUntil.Value < DateTimeOffset.UtcNow Then Return "ValidUntil cannot be in the past."

            For Each line In request.Lines
                If line.ItemId = Guid.Empty Then Return "Each quotation line requires an ItemId."
                If line.Quantity <= 0D Then Return "Each quotation line quantity must be greater than zero."
                If line.UnitPrice < 0D Then Return "UnitPrice cannot be negative."
                If line.DiscountPercent < 0D OrElse line.DiscountPercent > 100D Then Return "DiscountPercent must be between 0 and 100."
                If line.TaxPercent < 0D OrElse line.TaxPercent > 100D Then Return "TaxPercent must be between 0 and 100."
            Next

            Return String.Empty
        End Function

        Public Function CreateEntity(request As CreateQuotationRequest, items As IReadOnlyDictionary(Of Guid, Item)) As Quotation
            Dim quotation = New Quotation With {
                .Id = Guid.NewGuid(),
                .Number = CreateQuotationNumber(),
                .CustomerId = request.CustomerId,
                .CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
                .ValidUntil = request.ValidUntil,
                .Notes = request.Notes.Trim(),
                .Status = QuotationStatus.Draft,
                .CreatedUtc = DateTimeOffset.UtcNow
            }

            For Each requestLine In request.Lines
                Dim item = items(requestLine.ItemId)
                Dim subtotal = Decimal.Round(requestLine.Quantity * requestLine.UnitPrice, 2, MidpointRounding.AwayFromZero)
                Dim discount = Decimal.Round(subtotal * requestLine.DiscountPercent / 100D, 2, MidpointRounding.AwayFromZero)
                Dim taxable = subtotal - discount
                Dim tax = Decimal.Round(taxable * requestLine.TaxPercent / 100D, 2, MidpointRounding.AwayFromZero)
                Dim total = taxable + tax

                quotation.Lines.Add(New QuotationLine With {
                    .Id = Guid.NewGuid(),
                    .QuotationId = quotation.Id,
                    .ItemId = item.Id,
                    .Description = If(String.IsNullOrWhiteSpace(requestLine.Description), item.Name, requestLine.Description.Trim()),
                    .Quantity = requestLine.Quantity,
                    .UnitPrice = requestLine.UnitPrice,
                    .DiscountPercent = requestLine.DiscountPercent,
                    .TaxPercent = requestLine.TaxPercent,
                    .LineSubtotal = subtotal,
                    .LineDiscount = discount,
                    .LineTax = tax,
                    .LineTotal = total
                })
            Next

            quotation.Subtotal = Decimal.Round(quotation.Lines.Sum(Function(x) x.LineSubtotal), 2, MidpointRounding.AwayFromZero)
            quotation.DiscountAmount = Decimal.Round(quotation.Lines.Sum(Function(x) x.LineDiscount), 2, MidpointRounding.AwayFromZero)
            quotation.TaxAmount = Decimal.Round(quotation.Lines.Sum(Function(x) x.LineTax), 2, MidpointRounding.AwayFromZero)
            quotation.Total = Decimal.Round(quotation.Lines.Sum(Function(x) x.LineTotal), 2, MidpointRounding.AwayFromZero)

            Return quotation
        End Function

        Public Function ToDto(entity As Quotation) As QuotationDto
            Return New QuotationDto With {
                .Id = entity.Id,
                .Number = entity.Number,
                .CustomerId = entity.CustomerId,
                .Status = entity.Status,
                .CurrencyCode = entity.CurrencyCode,
                .Subtotal = entity.Subtotal,
                .DiscountAmount = entity.DiscountAmount,
                .TaxAmount = entity.TaxAmount,
                .Total = entity.Total,
                .ValidUntil = entity.ValidUntil,
                .Notes = entity.Notes,
                .CreatedUtc = entity.CreatedUtc,
                .Lines = entity.Lines.Select(Function(x) New QuotationLineDto With {
                    .Id = x.Id,
                    .ItemId = x.ItemId,
                    .Description = x.Description,
                    .Quantity = x.Quantity,
                    .UnitPrice = x.UnitPrice,
                    .DiscountPercent = x.DiscountPercent,
                    .TaxPercent = x.TaxPercent,
                    .LineSubtotal = x.LineSubtotal,
                    .LineDiscount = x.LineDiscount,
                    .LineTax = x.LineTax,
                    .LineTotal = x.LineTotal
                }).ToList()
            }
        End Function

        Private Shared Function CreateQuotationNumber() As String
            Return $"Q-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}"
        End Function
    End Class
End Namespace
