Imports System
Imports System.Configuration

Namespace Infrastructure
    Public NotInheritable Class AppSettings
        Public Property ConnectionString As String
        Public Property ContractsSyncProcedure As String
        Public Property AgenciesSyncProcedure As String
        Public Property BatchSize As Integer
        Public Property DryRun As Boolean
        Public Property DbCommandTimeoutSeconds As Integer
        Public Property WorkerSleepMilliseconds As Integer
        Public Property WorkerCutoffHour As Integer
        Public Property WorkerClaimTimeoutMinutes As Integer
        Public Property RunSelfTests As Boolean
        Public Property CargosBaseUrl As String
        Public Property CargosUsername As String
        Public Property CargosPassword As String
        Public Property CargosApiKey As String
        Public Property CargosOrganization As String
        Public Property CargosTokenPath As String
        Public Property CargosCheckPath As String
        Public Property CargosTabellaPath As String
        Public Property CargosSendPath As String
        Public Property CargosHttpTimeoutSeconds As Integer
        Public Property CargosUseCheckEndpoint As Boolean
        Public Property CargosCheckOnly As Boolean
        Public Property CargosSyncTablesOnStartup As Boolean
        Public Property CargosFailStartupIfTableSyncFails As Boolean
        Public Property CargosWebBaseUrl As String
        Public Property CargosWebLoginPath As String
        Public Property CargosWebAgencyCreatePath As String
        Public Property CargosWebUsername As String
        Public Property CargosWebPassword As String
        Public Property CargosWebAuthCookieHeader As String
        Public Property CargosWebVerifyTokenField As String
        Public Property CargosWebLoginUsernameField As String
        Public Property CargosWebLoginPasswordField As String
        Public Property CargosWebSyncAgenciesOnStartup As Boolean
        Public Property CargosWebFailStartupIfAgencySyncFails As Boolean
        Public Property EmailSmtpHost As String
        Public Property EmailSmtpPort As Integer
        Public Property EmailUser As String
        Public Property EmailPassword As String
        Public Property EmailFrom As String
        Public Property EmailEnableSsl As Boolean
        Public Property EmailCooldownHours As Integer

        Public ReadOnly Property EmailEnabled As Boolean
            Get
                Return Not String.IsNullOrWhiteSpace(EmailSmtpHost) AndAlso
                    EmailSmtpPort > 0 AndAlso
                    Not String.IsNullOrWhiteSpace(EmailFrom)
            End Get
        End Property

        Public Shared Function Load() As AppSettings
            Dim settings As New AppSettings()

            settings.ConnectionString = GetConnectionString("CargosDb")
            If String.IsNullOrWhiteSpace(settings.ConnectionString) Then
                settings.ConnectionString = GetSetting("Db.ConnectionString", String.Empty)
            End If

            settings.ContractsSyncProcedure = GetSetting("Db.ContractsSyncProcedure", "Cargos_Sync_Contratti_Frontiera")
            settings.AgenciesSyncProcedure = GetSetting("Db.AgenciesSyncProcedure", "Cargos_Sync_Agenzie_Frontiera")
            settings.BatchSize = GetIntSetting("Worker.BatchSize", 100)
            settings.DryRun = GetBoolSetting("Worker.DryRun", True)
            settings.DbCommandTimeoutSeconds = GetIntSetting("Db.CommandTimeoutSeconds", 120)
            settings.WorkerSleepMilliseconds = GetIntSetting("Worker.SleepMilliseconds", 10000)
            settings.WorkerCutoffHour = GetBoundedIntSetting("Worker.CutoffHour", 22, 0, 23)
            settings.WorkerClaimTimeoutMinutes = GetIntSetting("Worker.ClaimTimeoutMinutes", 5)
            settings.RunSelfTests = GetBoolSetting("Diagnostics.RunSelfTests", False)
            settings.CargosBaseUrl = GetSetting("Cargos.BaseUrl", String.Empty)
            settings.CargosUsername = GetSetting("Cargos.Username", String.Empty)
            settings.CargosPassword = GetSetting("Cargos.Password", String.Empty)
            settings.CargosApiKey = GetSetting("Cargos.ApiKey", String.Empty)
            settings.CargosOrganization = GetSetting("Cargos.Organization", settings.CargosUsername)
            settings.CargosTokenPath = GetSetting("Cargos.TokenPath", "/api/Token")
            settings.CargosCheckPath = GetSetting("Cargos.CheckPath", "/api/Check")
            settings.CargosTabellaPath = GetSetting("Cargos.TabellaPath", "/api/Tabella")
            settings.CargosSendPath = GetSetting("Cargos.SendPath", "/api/Send")
            settings.CargosHttpTimeoutSeconds = GetIntSetting("Cargos.HttpTimeoutSeconds", 60)
            settings.CargosUseCheckEndpoint = GetBoolSetting("Cargos.UseCheckEndpoint", False)
            settings.CargosCheckOnly = GetBoolSetting("Cargos.CheckOnly", False)
            settings.CargosSyncTablesOnStartup = GetBoolSetting("Cargos.SyncTablesOnStartup", False)
            settings.CargosFailStartupIfTableSyncFails = GetBoolSetting("Cargos.FailStartupIfTableSyncFails", True)
            settings.CargosWebBaseUrl = GetSetting("CargosWeb.BaseUrl", "https://cargos.poliziadistato.it/CARGOS_WEB")
            settings.CargosWebLoginPath = GetSetting("CargosWeb.LoginPath", "/Login/Login")
            settings.CargosWebAgencyCreatePath = GetSetting("CargosWeb.AgencyCreatePath", "/Agenzia/Create")
            settings.CargosWebUsername = GetSetting("CargosWeb.Username", settings.CargosUsername)
            settings.CargosWebPassword = GetSetting("CargosWeb.Password", settings.CargosPassword)
            settings.CargosWebAuthCookieHeader = GetSetting("CargosWeb.AuthCookieHeader", String.Empty)
            settings.CargosWebVerifyTokenField = GetSetting("CargosWeb.VerifyTokenField", "__RequestVerificationToken")
            settings.CargosWebLoginUsernameField = GetSetting("CargosWeb.LoginUsernameField", "Username")
            settings.CargosWebLoginPasswordField = GetSetting("CargosWeb.LoginPasswordField", "Password")
            settings.CargosWebSyncAgenciesOnStartup = GetBoolSetting("CargosWeb.SyncAgenciesOnStartup", False)
            settings.CargosWebFailStartupIfAgencySyncFails = GetBoolSetting("CargosWeb.FailStartupIfAgencySyncFails", True)
            settings.EmailSmtpHost = GetSetting("Email.SmtpHost", String.Empty)
            settings.EmailSmtpPort = GetIntSetting("Email.SmtpPort", 25)
            settings.EmailUser = GetSetting("Email.User", String.Empty)
            settings.EmailPassword = GetSetting("Email.Password", String.Empty)
            settings.EmailFrom = GetSetting("Email.From", String.Empty)
            settings.EmailEnableSsl = GetBoolSetting("Email.EnableSsl", False)
            settings.EmailCooldownHours = GetIntSetting("Email.CooldownHours", 24)

            Return settings
        End Function

        Private Shared Function GetConnectionString(name As String) As String
            Dim envValue As String = Environment.GetEnvironmentVariable("ConnectionStrings__" & name)
            If Not String.IsNullOrWhiteSpace(envValue) Then
                Return envValue.Trim()
            End If

            Dim setting As ConnectionStringSettings = ConfigurationManager.ConnectionStrings(name)
            If setting Is Nothing Then
                Return String.Empty
            End If

            Return If(setting.ConnectionString, String.Empty).Trim()
        End Function

        Private Shared Function GetSetting(key As String, defaultValue As String) As String
            Dim envKey As String = key.Replace(":", "__").Replace(".", "__")
            Dim envValue As String = Environment.GetEnvironmentVariable(envKey)
            If Not String.IsNullOrWhiteSpace(envValue) Then
                Return envValue.Trim()
            End If

            Dim appValue As String = ConfigurationManager.AppSettings(key)
            If String.IsNullOrWhiteSpace(appValue) Then
                Return defaultValue
            End If

            Return appValue.Trim()
        End Function

        Private Shared Function GetIntSetting(key As String, defaultValue As Integer) As Integer
            Dim value As String = GetSetting(key, defaultValue.ToString())
            Dim parsed As Integer
            If Integer.TryParse(value, parsed) AndAlso parsed > 0 Then
                Return parsed
            End If

            Return defaultValue
        End Function

        Private Shared Function GetBoundedIntSetting(key As String, defaultValue As Integer, minValue As Integer, maxValue As Integer) As Integer
            Dim value As String = GetSetting(key, defaultValue.ToString())
            Dim parsed As Integer
            If Integer.TryParse(value, parsed) AndAlso parsed >= minValue AndAlso parsed <= maxValue Then
                Return parsed
            End If

            Return defaultValue
        End Function

        Private Shared Function GetBoolSetting(key As String, defaultValue As Boolean) As Boolean
            Dim value As String = GetSetting(key, defaultValue.ToString())
            Dim parsed As Boolean
            If Boolean.TryParse(value, parsed) Then
                Return parsed
            End If

            Return defaultValue
        End Function
    End Class
End Namespace
