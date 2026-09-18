Imports WecErp.Domain

Namespace WecErp.Application.Projects
    Public Class CreateProjectRequest
        Public Property CustomerId As Guid
        Public Property Name As String = String.Empty
        Public Property StartsOn As DateOnly?
        Public Property EndsOn As DateOnly?
        Public Property BudgetAmount As Decimal
        Public Property CurrencyCode As String = "SAR"
        Public Property Notes As String = String.Empty
    End Class

    Public Class ChangeProjectStatusRequest
        Public Property Status As String = String.Empty
    End Class

    Public Class CreateProjectTaskRequest
        Public Property Name As String = String.Empty
        Public Property Sequence As Integer
        Public Property EstimatedCost As Decimal
    End Class

    Public Class UpdateProjectTaskCostRequest
        Public Property ActualCost As Decimal
    End Class

    Public Class CompleteProjectTaskRequest
        Public Property Completed As Boolean = True
    End Class

    Public Class ProjectService
        Public Function ValidateProject(request As CreateProjectRequest) As String
            If request.CustomerId = Guid.Empty Then Return "CustomerId is required."
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            If request.Name.Trim().Length > 300 Then Return "Name cannot exceed 300 characters."
            If request.StartsOn.HasValue AndAlso request.EndsOn.HasValue AndAlso request.StartsOn.Value > request.EndsOn.Value Then Return "StartsOn cannot be after EndsOn."
            If request.BudgetAmount < 0D Then Return "BudgetAmount cannot be negative."
            If String.IsNullOrWhiteSpace(request.CurrencyCode) OrElse request.CurrencyCode.Trim().Length <> 3 Then Return "CurrencyCode must be a 3-letter code."
            Return String.Empty
        End Function

        Public Function CreateProject(request As CreateProjectRequest) As Project
            Return New Project With {
                .Id = Guid.NewGuid(),
                .Number = $"PRJ-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                .CustomerId = request.CustomerId,
                .Name = request.Name.Trim(),
                .Status = ProjectStatus.Draft,
                .StartsOn = request.StartsOn,
                .EndsOn = request.EndsOn,
                .BudgetAmount = Decimal.Round(request.BudgetAmount, 2, MidpointRounding.AwayFromZero),
                .CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
                .Notes = request.Notes.Trim(),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function ValidateTask(request As CreateProjectTaskRequest) As String
            If String.IsNullOrWhiteSpace(request.Name) Then Return "Name is required."
            If request.Name.Trim().Length > 300 Then Return "Name cannot exceed 300 characters."
            If request.Sequence < 0 Then Return "Sequence cannot be negative."
            If request.EstimatedCost < 0D Then Return "EstimatedCost cannot be negative."
            Return String.Empty
        End Function

        Public Function CreateTask(projectId As Guid, request As CreateProjectTaskRequest) As ProjectTask
            Return New ProjectTask With {
                .Id = Guid.NewGuid(),
                .ProjectId = projectId,
                .Name = request.Name.Trim(),
                .Sequence = request.Sequence,
                .IsCompleted = False,
                .EstimatedCost = Decimal.Round(request.EstimatedCost, 2, MidpointRounding.AwayFromZero),
                .CreatedUtc = DateTimeOffset.UtcNow
            }
        End Function

        Public Function TryChangeStatus(project As Project, requestedStatus As String, ByRef errorMessage As String) As Boolean
            errorMessage = String.Empty
            Dim target As ProjectStatus
            If project Is Nothing Then errorMessage = "Project is required.": Return False
            If Not [Enum].TryParse(requestedStatus.Trim(), True, target) OrElse Not [Enum].IsDefined(GetType(ProjectStatus), target) Then errorMessage = "Invalid project status.": Return False
            If project.Status = target Then errorMessage = "Project is already in this status.": Return False
            Dim allowed = (project.Status = ProjectStatus.Draft AndAlso target = ProjectStatus.Active) OrElse
                          (project.Status = ProjectStatus.Draft AndAlso target = ProjectStatus.Cancelled) OrElse
                          (project.Status = ProjectStatus.Active AndAlso target = ProjectStatus.OnHold) OrElse
                          (project.Status = ProjectStatus.Active AndAlso target = ProjectStatus.Completed) OrElse
                          (project.Status = ProjectStatus.Active AndAlso target = ProjectStatus.Cancelled) OrElse
                          (project.Status = ProjectStatus.OnHold AndAlso target = ProjectStatus.Active) OrElse
                          (project.Status = ProjectStatus.OnHold AndAlso target = ProjectStatus.Cancelled)
            If Not allowed Then errorMessage = $"Project cannot move from {project.Status} to {target}.": Return False
            project.Status = target
            Return True
        End Function
    End Class
End Namespace
