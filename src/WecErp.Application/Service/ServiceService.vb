Imports WecErp.Domain

Namespace WecErp.Application.Service
    Public Class ServiceService
        Public Function ValidateContract(request As CreateServiceContractRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If request.StartsOn > request.EndsOn Then Return "StartsOn cannot be after EndsOn."
            If request.VisitFrequencyPerWeek < 1 OrElse request.VisitFrequencyPerWeek > 14 Then Return "VisitFrequencyPerWeek must be between 1 and 14."
            Return String.Empty
        End Function

        Public Function CreateContract(request As CreateServiceContractRequest) As ServiceContract
            Return New ServiceContract With {
                .Id = Guid.NewGuid(),
                .Number = $"SC-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                .CustomerId = request.CustomerId,
                .StartsOn = request.StartsOn,
                .EndsOn = request.EndsOn,
                .VisitFrequencyPerWeek = request.VisitFrequencyPerWeek,
                .Notes = request.Notes.Trim(),
                .Status = ServiceContractStatus.Draft,
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function CreateWorkOrder(request As CreateWorkOrderRequest) As WorkOrder
            If request.CustomerId = Guid.Empty Then Throw New InvalidOperationException("CustomerId is required.")
            If request.ScheduledStartUtc.HasValue Xor request.ScheduledEndUtc.HasValue Then Throw New InvalidOperationException("Both schedule endpoints are required when scheduling a work order.")
            If request.ScheduledStartUtc.HasValue AndAlso request.ScheduledStartUtc.Value >= request.ScheduledEndUtc.Value Then Throw New InvalidOperationException("ScheduledStartUtc must be before ScheduledEndUtc.")
            Return New WorkOrder With {
                .Id = Guid.NewGuid(),
                .Number = $"WO-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                .CustomerId = request.CustomerId,
                .ServiceContractId = request.ServiceContractId,
                .Status = If(request.ScheduledStartUtc.HasValue, WorkOrderStatus.Scheduled, WorkOrderStatus.Open),
                .ScheduledStartUtc = request.ScheduledStartUtc,
                .ScheduledEndUtc = request.ScheduledEndUtc,
                .Description = request.Description.Trim(),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function GenerateScheduledWorkOrders(contract As ServiceContract, request As GenerateWorkOrdersRequest, ByRef errorMessage As String) As List(Of WorkOrder)
            errorMessage = String.Empty
            If contract Is Nothing Then errorMessage = "Service contract is required.": Return New List(Of WorkOrder)()
            If contract.Status <> ServiceContractStatus.Active Then errorMessage = "Only active service contracts can generate work orders.": Return New List(Of WorkOrder)()
            If request.FirstVisitUtc = DateTimeOffset.MinValue Then errorMessage = "FirstVisitUtc is required.": Return New List(Of WorkOrder)()
            If request.DurationMinutes < 1 OrElse request.DurationMinutes > 1440 Then errorMessage = "DurationMinutes must be between 1 and 1440.": Return New List(Of WorkOrder)()

            Dim startDate = request.FirstVisitUtc.UtcDateTime.Date
            If startDate < contract.StartsOn.ToDateTime(TimeOnly.MinValue).Date OrElse startDate > contract.EndsOn.ToDateTime(TimeOnly.MaxValue).Date Then
                errorMessage = "FirstVisitUtc must fall within the service contract dates."
                Return New List(Of WorkOrder)()
            End If

            Dim interval = TimeSpan.FromDays(7.0 / contract.VisitFrequencyPerWeek)
            Dim cursor = request.FirstVisitUtc.ToUniversalTime()
            Dim contractEndUtc = New DateTimeOffset(contract.EndsOn.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero)
            Dim result = New List(Of WorkOrder)()

            While cursor <= contractEndUtc
                Dim orderEnd = cursor.AddMinutes(request.DurationMinutes)
                If orderEnd > contractEndUtc.AddMinutes(1) Then Exit While
                result.Add(New WorkOrder With {
                    .Id = Guid.NewGuid(),
                    .Number = $"WO-{cursor:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                    .CustomerId = contract.CustomerId,
                    .ServiceContractId = contract.Id,
                    .Status = WorkOrderStatus.Scheduled,
                    .ScheduledStartUtc = cursor,
                    .ScheduledEndUtc = orderEnd,
                    .Description = If(String.IsNullOrWhiteSpace(request.Description), "Scheduled maintenance visit", request.Description.Trim()),
                    .CreatedUtc = DateTimeOffset.UtcNow
                })
                cursor = cursor.Add(interval)
            End While

            Return result
        End Function

        Public Function TryChangeWorkOrderStatus(order As WorkOrder, requestedStatus As String, ByRef errorMessage As String) As Boolean
            errorMessage = String.Empty
            Dim target As WorkOrderStatus
            If order Is Nothing Then errorMessage = "Work order is required.": Return False
            If Not [Enum].TryParse(requestedStatus.Trim(), True, target) OrElse Not [Enum].IsDefined(GetType(WorkOrderStatus), target) Then errorMessage = "Invalid work order status.": Return False
            If order.Status = target Then errorMessage = "Work order is already in this status.": Return False
            Dim allowed = (order.Status = WorkOrderStatus.Open AndAlso target = WorkOrderStatus.Scheduled) OrElse
                          (order.Status = WorkOrderStatus.Open AndAlso target = WorkOrderStatus.Cancelled) OrElse
                          (order.Status = WorkOrderStatus.Scheduled AndAlso target = WorkOrderStatus.InProgress) OrElse
                          (order.Status = WorkOrderStatus.Scheduled AndAlso target = WorkOrderStatus.Cancelled) OrElse
                          (order.Status = WorkOrderStatus.InProgress AndAlso target = WorkOrderStatus.Completed) OrElse
                          (order.Status = WorkOrderStatus.InProgress AndAlso target = WorkOrderStatus.Cancelled)
            If Not allowed Then errorMessage = $"Work order cannot move from {order.Status} to {target}.": Return False
            order.Status = target
            Return True
        End Function
    End Class
End Namespace
