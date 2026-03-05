Imports System
Imports System.Configuration

Namespace Infrastructure
    Public NotInheritable Class AppSettings
        Public Property ConnectionString As String
        Public Property ContractsSyncProcedure As String
        Public Property BatchSize As Integer
        Public Property DryRun As Boolean
        Public Property DbCommandTimeoutSeconds As Integer
        Public Property CargosBaseUrl As String
        Public Property CargosUsername As String
        Public Property CargosPassword As String
        Public Property CargosApiKey As String
        Public Property CargosOrganization As String
        Public Property CargosTokenPath As String
        Public Property CargosSendPath As String
        Public Property CargosHttpTimeoutSeconds As Integer

        Public Shared Function Load() As AppSettings
            Dim settings As New AppSettings()

            settings.ConnectionString = GetConnectionString("CargosDb")
            If String.IsNullOrWhiteSpace(settings.ConnectionString) Then
                settings.ConnectionString = GetSetting("Db.ConnectionString", String.Empty)
            End If

            settings.ContractsSyncProcedure = GetSetting("Db.ContractsSyncProcedure", "Cargos_Sync_Contratti_Frontiera")
            settings.BatchSize = GetIntSetting("Worker.BatchSize", 100)
            settings.DryRun = GetBoolSetting("Worker.DryRun", True)
            settings.DbCommandTimeoutSeconds = GetIntSetting("Db.CommandTimeoutSeconds", 120)
            settings.CargosBaseUrl = GetSetting("Cargos.BaseUrl", String.Empty)
            settings.CargosUsername = GetSetting("Cargos.Username", String.Empty)
            settings.CargosPassword = GetSetting("Cargos.Password", String.Empty)
            settings.CargosApiKey = GetSetting("Cargos.ApiKey", String.Empty)
            settings.CargosOrganization = GetSetting("Cargos.Organization", settings.CargosUsername)
            settings.CargosTokenPath = GetSetting("Cargos.TokenPath", "/api/Token")
            settings.CargosSendPath = GetSetting("Cargos.SendPath", "/api/Send")
            settings.CargosHttpTimeoutSeconds = GetIntSetting("Cargos.HttpTimeoutSeconds", 60)

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
