Imports System.Data
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly apiUrlTextBox As New TextBox()
    Private ReadOnly connectionButton As New Button()
    Private ReadOnly loginButton As New Button()
    Private ReadOnly usernameTextBox As New TextBox()
    Private ReadOnly passwordTextBox As New TextBox()
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
            .Height = 104,
            .Padding = New Padding(20, 14, 20, 10)
        }

        titleLabel.AutoSize = True
        titleLabel.Font = New Drawing.Font("Segoe UI", 18.0F, Drawing.FontStyle.Bold)
        titleLabel.Text = "WEC ERP"
        titleLabel.Left = 20
        titleLabel.Top = 12

        apiUrlTextBox.Left = 300
        apiUrlTextBox.Top = 52
        apiUrlTextBox.Width = 500
        apiUrlTextBox.Text = "https://localhost:7001"
        apiUrlTextBox.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        usernameTextBox.Left = 300
        usernameTextBox.Top = 16
        usernameTextBox.Width = 160
        usernameTextBox.PlaceholderText = "Username"

        passwordTextBox.Left = 465
        passwordTextBox.Top = 16
        passwordTextBox.Width = 160
        passwordTextBox.UseSystemPasswordChar = True
        passwordTextBox.PlaceholderText = "Password"

        loginButton.Text = "Login"
        loginButton.Width = 90
        loginButton.Height = 32
        loginButton.Left = 630
        loginButton.Top = 14
        AddHandler loginButton.Click, AddressOf LoginAsync

        connectionButton.Text = "Check Server"
        connectionButton.Width = 130
        connectionButton.Height = 32
        connectionButton.Left = 800
        connectionButton.Top = 14
        connectionButton.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        AddHandler connectionButton.Click, AddressOf CheckServerAsync

        statusLabel.AutoSize = False
        statusLabel.Width = 1000
        statusLabel.Height = 20
        statusLabel.Left = 300
        statusLabel.Top = 78
        statusLabel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        statusLabel.Text = "Not checked"

        header.Controls.Add(titleLabel)
        header.Controls.Add(usernameTextBox)
        header.Controls.Add(passwordTextBox)
        header.Controls.Add(loginButton)
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
        AddNavigationButton("Suppliers", "/api/v1/suppliers")
        AddNavigationButton("Warehouses", "/api/v1/warehouses")
        AddNavigationButton("Inventory", "/api/v1/inventory/balances")
        AddNavigationButton("Purchase Orders", "/api/v1/purchase-orders")
        AddNavigationButton("Invoices", "/api/v1/invoices")

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
        table.Rows.Add("Suppliers", "Ready", "GET /api/v1/suppliers")
        table.Rows.Add("Warehouses", "Ready", "GET /api/v1/warehouses")
        table.Rows.Add("Inventory", "Ready", "GET /api/v1/inventory/balances")
        table.Rows.Add("Purchase Orders", "Ready", "GET /api/v1/purchase-orders")
        table.Rows.Add("Invoices", "Ready", "GET /api/v1/invoices")
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
            Case "/api/v1/suppliers"
                table.Columns.Add("Code")
                table.Columns.Add("Name")
                table.Columns.Add("Phone")
                table.Columns.Add("Email")
            Case "/api/v1/warehouses"
                table.Columns.Add("Code")
                table.Columns.Add("Name")
                table.Columns.Add("Active")
            Case "/api/v1/inventory/balances"
                table.Columns.Add("Item ID")
                table.Columns.Add("Warehouse ID")
                table.Columns.Add("On Hand")
                table.Columns.Add("Reserved")
                table.Columns.Add("Available")
            Case "/api/v1/purchase-orders"
                table.Columns.Add("Number")
                table.Columns.Add("Supplier ID")
                table.Columns.Add("Status")
                table.Columns.Add("Currency")
                table.Columns.Add("Total")
            Case "/api/v1/invoices"
                table.Columns.Add("Number")
                table.Columns.Add("Customer ID")
                table.Columns.Add("Status")
                table.Columns.Add("Currency")
                table.Columns.Add("Total")
                table.Columns.Add("Paid")
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
                Case "/api/v1/suppliers"
                    arrayProperty = "suppliers"
                Case "/api/v1/warehouses"
                    arrayProperty = "warehouses"
                Case "/api/v1/inventory/balances"
                    arrayProperty = "balances"
                Case "/api/v1/purchase-orders"
                    arrayProperty = "purchaseOrders"
                Case "/api/v1/invoices"
                    arrayProperty = "invoices"
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
                    Case "/api/v1/suppliers"
                        table.Rows.Add(GetString(record, "code"), GetString(record, "name"), GetString(record, "phone"), GetString(record, "email"))
                    Case "/api/v1/warehouses"
                        table.Rows.Add(GetString(record, "code"), GetString(record, "name"), GetString(record, "isActive"))
                    Case "/api/v1/inventory/balances"
                        table.Rows.Add(GetString(record, "itemId"), GetString(record, "warehouseId"), GetString(record, "onHand"), GetString(record, "reserved"), GetString(record, "available"))
                    Case "/api/v1/purchase-orders"
                        table.Rows.Add(GetString(record, "number"), GetString(record, "supplierId"), GetString(record, "status"), GetString(record, "currencyCode"), GetString(record, "total"))
                    Case "/api/v1/invoices"
                        table.Rows.Add(GetString(record, "number"), GetString(record, "customerId"), GetString(record, "status"), GetString(record, "currencyCode"), GetString(record, "total"), GetString(record, "paidAmount"))
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

    Private Async Sub LoginAsync(sender As Object, e As EventArgs)
        loginButton.Enabled = False
        Try
            Dim baseUrl = GetBaseUrl()
            If String.IsNullOrWhiteSpace(usernameTextBox.Text) OrElse String.IsNullOrWhiteSpace(passwordTextBox.Text) Then
                MessageBox.Show("Username and password are required.", "WEC ERP", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim payload = JsonSerializer.Serialize(New With {
                .userName = usernameTextBox.Text.Trim(),
                .password = passwordTextBox.Text
            })
            Using request = New HttpRequestMessage(HttpMethod.Post, baseUrl & "/api/v1/auth/login")
                request.Content = New StringContent(payload, Encoding.UTF8, "application/json")
                Using response = Await httpClient.SendAsync(request)
                    Dim body = Await response.Content.ReadAsStringAsync()
                    If Not response.IsSuccessStatusCode Then
                        statusLabel.Text = $"Login failed: HTTP {CInt(response.StatusCode)}"
                        MessageBox.Show(body, "WEC ERP", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    Using document = JsonDocument.Parse(body)
                        Dim tokenElement As JsonElement
                        If Not document.RootElement.TryGetProperty("accessToken", tokenElement) Then
                            Throw New InvalidOperationException("Login response did not contain an access token.")
                        End If
                        Dim token = tokenElement.GetString()
                        If String.IsNullOrWhiteSpace(token) Then Throw New InvalidOperationException("Login returned an empty access token.")
                        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)
                    End Using

                    statusLabel.Text = "Logged in successfully."
                    passwordTextBox.Clear()
                    ShowDashboard()
                End Using
            End Using
        Catch ex As Exception
            statusLabel.Text = "Login failed"
            MessageBox.Show(ex.Message, "WEC ERP", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            loginButton.Enabled = True
        End Try
    End Sub

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
