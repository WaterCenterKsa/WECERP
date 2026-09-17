Imports System.Data
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly apiUrlTextBox As New TextBox()
    Private ReadOnly connectionButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly navigationPanel As New FlowLayoutPanel()
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly titleLabel As New Label()
    Private ReadOnly httpClient As New HttpClient()

    Public Sub New()
        Text = "WEC ERP"
        Width = 1280
        Height = 760
        MinimumSize = New Drawing.Size(1000, 650)
        StartPosition = FormStartPosition.CenterScreen

        BuildHeader()
        BuildNavigation()
        BuildContent()

        AddHandler Shown, AddressOf MainForm_Shown
    End Sub

    Private Sub BuildHeader()
        Dim header = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 76,
            .Padding = New Padding(20, 14, 20, 10)
        }

        titleLabel.AutoSize = True
        titleLabel.Font = New Drawing.Font("Segoe UI", 18.0F, Drawing.FontStyle.Bold)
        titleLabel.Text = "WEC ERP"
        titleLabel.Left = 20
        titleLabel.Top = 12

        apiUrlTextBox.Left = 300
        apiUrlTextBox.Top = 16
        apiUrlTextBox.Width = 500
        apiUrlTextBox.Text = "https://localhost:7001"
        apiUrlTextBox.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        connectionButton.Text = "Check Server"
        connectionButton.Width = 130
        connectionButton.Height = 32
        connectionButton.Left = 815
        connectionButton.Top = 14
        connectionButton.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler connectionButton.Click, AddressOf CheckServerAsync

        statusLabel.AutoSize = False
        statusLabel.Width = 1000
        statusLabel.Height = 20
        statusLabel.Left = 300
        statusLabel.Top = 49
        statusLabel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        statusLabel.Text = "Not checked"

        header.Controls.Add(titleLabel)
        header.Controls.Add(apiUrlTextBox)
        header.Controls.Add(connectionButton)
        header.Controls.Add(statusLabel)
        Controls.Add(header)
    End Sub

    Private Sub BuildNavigation()
        navigationPanel.Dock = DockStyle.Left
        navigationPanel.Width = 190
        navigationPanel.FlowDirection = FlowDirection.TopDown
        navigationPanel.WrapContents = False
        navigationPanel.Padding = New Padding(12)
        navigationPanel.AutoScroll = True

        AddNavigationButton("Dashboard", Nothing)
        AddNavigationButton("Customers", "/api/v1/customers")
        AddNavigationButton("Items", "/api/v1/items")
        AddNavigationButton("Quotations", "/api/v1/quotations")
        AddNavigationButton("Sales Orders", "/api/v1/sales-orders")
        AddNavigationButton("Resources", "/api/v1/resources")
        AddNavigationButton("Bookings", "/api/v1/bookings")

        Controls.Add(navigationPanel)
    End Sub

    Private Sub AddNavigationButton(caption As String, endpoint As String)
        Dim button = New Button With {
            .Text = caption,
            .Width = 160,
            .Height = 42,
            .Margin = New Padding(0, 0, 0, 8),
            .Tag = endpoint
        }

        If endpoint Is Nothing Then
            AddHandler button.Click, Sub(sender, e) ShowDashboard()
        Else
            AddHandler button.Click, Async Sub(sender, e) Await LoadEndpointAsync(CStr(button.Tag), caption)
        End If

        navigationPanel.Controls.Add(button)
    End Sub

    Private Sub BuildContent()
        Dim content = New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(12)
        }

        grid.Dock = DockStyle.Fill
        grid.AutoGenerateColumns = True
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False

        content.Controls.Add(grid)
        Controls.Add(content)
    End Sub

    Private Sub MainForm_Shown(sender As Object, e As EventArgs)
        ShowDashboard()
    End Sub

    Private Sub ShowDashboard()
        titleLabel.Text = "WEC ERP Dashboard"
        Dim table = New DataTable()
        table.Columns.Add("Module")
        table.Columns.Add("Status")
        table.Columns.Add("API")
        table.Rows.Add("Customers", "Ready", "GET /api/v1/customers")
        table.Rows.Add("Items", "Ready", "GET /api/v1/items")
        table.Rows.Add("Quotations", "Ready", "GET /api/v1/quotations")
        table.Rows.Add("Sales Orders", "Ready", "GET /api/v1/sales-orders")
        table.Rows.Add("Resources", "Ready", "GET /api/v1/resources")
        table.Rows.Add("Bookings", "Ready", "GET /api/v1/bookings")
        grid.DataSource = table
    End Sub

    Private Async Function LoadEndpointAsync(endpoint As String, caption As String) As Task
        Try
            Dim baseUrl = GetBaseUrl()
            titleLabel.Text = caption
            statusLabel.Text = "Loading " & caption & "..."

            Using response = Await httpClient.GetAsync(baseUrl & endpoint)
                Dim body = Await response.Content.ReadAsStringAsync()
                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text = $"HTTP {CInt(response.StatusCode)}"
                    MessageBox.Show(body, "WEC ERP", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim table = CreateTableForEndpoint(endpoint)
                PopulateTable(table, body, endpoint)
                grid.DataSource = table
                statusLabel.Text = $"Loaded {table.Rows.Count} record(s)"
            End Using
        Catch ex As Exception
            statusLabel.Text = "Request failed"
            MessageBox.Show(ex.Message, "WEC ERP", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Function CreateTableForEndpoint(endpoint As String) As DataTable
        Dim table = New DataTable()

        Select Case endpoint
            Case "/api/v1/customers"
                table.Columns.Add("Code")
                table.Columns.Add("Name")
                table.Columns.Add("Phone")
                table.Columns.Add("Email")
                table.Columns.Add("Tax Number")
            Case "/api/v1/items"
                table.Columns.Add("SKU")
                table.Columns.Add("Name")
                table.Columns.Add("Type")
                table.Columns.Add("Active")
            Case "/api/v1/quotations"
                table.Columns.Add("Number")
                table.Columns.Add("Customer ID")
                table.Columns.Add("Status")
                table.Columns.Add("Currency")
                table.Columns.Add("Total")
                table.Columns.Add("Valid Until")
            Case "/api/v1/sales-orders"
                table.Columns.Add("Number")
                table.Columns.Add("Customer ID")
                table.Columns.Add("Status")
                table.Columns.Add("Currency")
                table.Columns.Add("Total")
            Case "/api/v1/resources"
                table.Columns.Add("Code")
                table.Columns.Add("Name")
                table.Columns.Add("Type")
                table.Columns.Add("Active")
            Case "/api/v1/bookings"
                table.Columns.Add("Number")
                table.Columns.Add("Customer ID")
                table.Columns.Add("Item ID")
                table.Columns.Add("Resource ID")
                table.Columns.Add("Starts UTC")
                table.Columns.Add("Ends UTC")
                table.Columns.Add("Status")
        End Select

        Return table
    End Function

    Private Sub PopulateTable(table As DataTable, json As String, endpoint As String)
        Using document = JsonDocument.Parse(json)
            Dim root = document.RootElement
            Dim arrayProperty As String

            Select Case endpoint
                Case "/api/v1/customers"
                    arrayProperty = "customers"
                Case "/api/v1/items"
                    arrayProperty = "items"
                Case "/api/v1/quotations"
                    arrayProperty = "quotations"
                Case "/api/v1/sales-orders"
                    arrayProperty = "salesOrders"
                Case "/api/v1/resources"
                    arrayProperty = "resources"
                Case "/api/v1/bookings"
                    arrayProperty = "bookings"
                Case Else
                    Return
            End Select

            Dim records As JsonElement
            If Not root.TryGetProperty(arrayProperty, records) OrElse records.ValueKind <> JsonValueKind.Array Then Return

            For Each record In records.EnumerateArray()
                Select Case endpoint
                    Case "/api/v1/customers"
                        table.Rows.Add(GetString(record, "code"), GetString(record, "name"), GetString(record, "phone"), GetString(record, "email"), GetString(record, "taxNumber"))
                    Case "/api/v1/items"
                        table.Rows.Add(GetString(record, "sku"), GetString(record, "name"), GetString(record, "type"), GetString(record, "isActive"))
                    Case "/api/v1/quotations"
                        table.Rows.Add(GetString(record, "number"), GetString(record, "customerId"), GetString(record, "status"), GetString(record, "currencyCode"), GetString(record, "total"), GetString(record, "validUntil"))
                    Case "/api/v1/sales-orders"
                        table.Rows.Add(GetString(record, "number"), GetString(record, "customerId"), GetString(record, "status"), GetString(record, "currencyCode"), GetString(record, "total"))
                    Case "/api/v1/resources"
                        table.Rows.Add(GetString(record, "code"), GetString(record, "name"), GetString(record, "type"), GetString(record, "isActive"))
                    Case "/api/v1/bookings"
                        table.Rows.Add(GetString(record, "number"), GetString(record, "customerId"), GetString(record, "itemId"), GetString(record, "resourceId"), GetString(record, "startsUtc"), GetString(record, "endsUtc"), GetString(record, "status"))
                End Select
            Next
        End Using
    End Sub

    Private Shared Function GetString(element As JsonElement, propertyName As String) As String
        Dim value As JsonElement
        If Not element.TryGetProperty(propertyName, value) Then Return String.Empty
        If value.ValueKind = JsonValueKind.Null Then Return String.Empty
        Return value.ToString()
    End Function

    Private Function GetBaseUrl() As String
        Dim baseUrl = apiUrlTextBox.Text.Trim().TrimEnd("/"c)
        Dim parsed As Uri = Nothing
        If Not Uri.TryCreate(baseUrl, UriKind.Absolute, parsed) OrElse (parsed.Scheme <> Uri.UriSchemeHttps AndAlso parsed.Scheme <> Uri.UriSchemeHttp) Then
            Throw New InvalidOperationException("Please enter a valid HTTP or HTTPS API URL.")
        End If
        Return baseUrl
    End Function

    Private Async Sub CheckServerAsync(sender As Object, e As EventArgs)
        connectionButton.Enabled = False
        statusLabel.Text = "Checking ERP API..."

        Try
            Dim baseUrl = GetBaseUrl()
            Using response = Await httpClient.GetAsync(baseUrl & "/api/v1/health/live")
                Dim body = Await response.Content.ReadAsStringAsync()
                statusLabel.Text = $"HTTP {CInt(response.StatusCode)} — {body}"
            End Using
        Catch ex As Exception
            statusLabel.Text = "Connection failed: " & ex.Message
        Finally
            connectionButton.Enabled = True
        End Try
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            httpClient.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
