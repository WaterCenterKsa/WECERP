Imports System.Net.Http
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly apiUrlTextBox As New TextBox()
    Private ReadOnly checkButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly httpClient As New HttpClient()

    Public Sub New()
        Text = "WEC ERP"
        Width = 900
        Height = 550
        StartPosition = FormStartPosition.CenterScreen

        apiUrlTextBox.Left = 30
        apiUrlTextBox.Top = 30
        apiUrlTextBox.Width = 600
        apiUrlTextBox.Text = "https://localhost:7001"

        checkButton.Left = 650
        checkButton.Top = 28
        checkButton.Width = 160
        checkButton.Text = "Check Server"
        AddHandler checkButton.Click, AddressOf CheckServerAsync

        statusLabel.Left = 30
        statusLabel.Top = 90
        statusLabel.Width = 780
        statusLabel.Height = 100
        statusLabel.Text = "Enter the ERP API address and check the live connection."

        Controls.Add(apiUrlTextBox)
        Controls.Add(checkButton)
        Controls.Add(statusLabel)
    End Sub

    Private Async Sub CheckServerAsync(sender As Object, e As EventArgs)
        checkButton.Enabled = False
        statusLabel.Text = "Checking ERP API..."

        Try
            Dim baseUrl = apiUrlTextBox.Text.Trim().TrimEnd("/"c)
            If Not Uri.TryCreate(baseUrl, UriKind.Absolute, Nothing) Then
                Throw New InvalidOperationException("Please enter a valid API URL.")
            End If

            Using response = Await httpClient.GetAsync(baseUrl & "/api/v1/health/live")
                Dim body = Await response.Content.ReadAsStringAsync()
                statusLabel.Text = $"HTTP {(CInt(response.StatusCode))}: {body}"
            End Using
        Catch ex As Exception
            statusLabel.Text = "Connection failed: " & ex.Message
        Finally
            checkButton.Enabled = True
        End Try
    End Sub
End Class
