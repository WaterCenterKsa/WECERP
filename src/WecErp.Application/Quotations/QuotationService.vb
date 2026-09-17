Imports WecErp.Domain

Namespace WecErp.Application.Quotations
    Public Class QuotationService
        Public Function ValidateNewQuotation(request As CreateQuotationRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If request.Lines Is Nothing OrElse request.Lines.Count = 0 Then Return "At least one quotation line is required."
            If String.IsNullOrWhiteSpace(request.CurrencyCode) Then Return "CurrencyCode is required."
            If request.CurrencyCode.Trim().Length <> 3 Then Return "CurrencyCode must be a 3-letter ISO code."
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

        Public Function TryChangeStatus(quotation As Quotation, requestedStatus As String, ByRef errorMessage As String) As Boolean
            errorMessage = String.Empty

            If quotation Is Nothing Then
                errorMessage = "Quotation is required."
                Return False
            End If

            If String.IsNullOrWhiteSpace(requestedStatus) Then
                errorMessage = "Status is required."
                Return False
            End If

            Dim targetStatus As QuotationStatus
            If Not [Enum].TryParse(requestedStatus.Trim(), True, targetStatus) OrElse Not [Enum].IsDefined(GetType(QuotationStatus), targetStatus) Then
                errorMessage = "Invalid quotation status."
                Return False
            End If

            If quotation.Status = targetStatus Then
                errorMessage = "Quotation is already in this status."
                Return False
            End If

            Dim allowed As Boolean = False
            Select Case quotation.Status
                Case QuotationStatus.Draft
                    allowed = targetStatus = QuotationStatus.Sent OrElse targetStatus = QuotationStatus.Cancelled
                Case QuotationStatus.Sent
                    allowed = targetStatus = QuotationStatus.Accepted OrElse
                              targetStatus = QuotationStatus.Rejected OrElse
                              targetStatus = QuotationStatus.Expired OrElse
                              targetStatus = QuotationStatus.Cancelled
            End Select

            If Not allowed Then
                errorMessage = $"Quotation cannot move from {quotation.Status} to {targetStatus}."
                Return False
            End If

            If targetStatus = QuotationStatus.Sent OrElse targetStatus = QuotationStatus.Accepted Then
                If quotation.ValidUntil.HasValue AndAlso quotation.ValidUntil.Value <= DateTimeOffset.UtcNow Then
                    errorMessage = "Quotation has expired and cannot be sent or accepted."
                    Return False
                End If
            End If

            If targetStatus = QuotationStatus.Expired Then
                If Not quotation.ValidUntil.HasValue OrElse quotation.ValidUntil.Value > DateTimeOffset.UtcNow Then
                    errorMessage = "Quotation can only be marked Expired after its validity date has passed."
                    Return False
                End If
            End If

            quotation.Status = targetStatus
            Return True
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
