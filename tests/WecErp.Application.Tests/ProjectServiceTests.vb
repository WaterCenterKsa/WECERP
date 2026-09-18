Imports WecErp.Application.Projects
Imports WecErp.Domain
Imports Xunit

Namespace WecErp.Application.Tests
    Public Class ProjectServiceTests
        <Fact>
        Public Sub ValidateProject_RejectsInvalidDatesAndBudget()
            Dim service = New ProjectService()
            Dim request = New CreateProjectRequest With {
                .CustomerId = Guid.NewGuid(),
                .Name = "Pool Construction",
                .StartsOn = New DateOnly(2026, 12, 1),
                .EndsOn = New DateOnly(2026, 11, 1),
                .BudgetAmount = -1D
            }
            Assert.NotEmpty(service.ValidateProject(request))
        End Sub

        <Fact>
        Public Sub CreateProject_NormalizesCurrencyAndRoundsBudget()
            Dim service = New ProjectService()
            Dim project = service.CreateProject(New CreateProjectRequest With {
                .CustomerId = Guid.NewGuid(),
                .Name = "Pool Construction",
                .BudgetAmount = 1234.567D,
                .CurrencyCode = "sar"
            })
            Assert.Equal("SAR", project.CurrencyCode)
            Assert.Equal(1234.57D, project.BudgetAmount)
        End Sub

        <Fact>
        Public Sub TryChangeStatus_RejectsTerminalTransition()
            Dim service = New ProjectService()
            Dim project = New Project With {.Status = ProjectStatus.Completed}
            Dim errorMessage = String.Empty
            Assert.False(service.TryChangeStatus(project, "Active", errorMessage))
        End Sub
    End Class
End Namespace
