Imports API_Cargos.Integration
Imports API_Cargos.Infrastructure
Imports API_Cargos.Orchestration
Imports API_Cargos.Persistence

Module Module1

    Sub Main()
        Dim logger As ILogger = New ConsoleLogger()

        Try
            Dim settings As AppSettings = AppSettings.Load()
            ValidateSettings(settings)

            Dim syncRepository As ISyncRepository = New SqlSyncRepository(
                settings.ConnectionString,
                settings.DbCommandTimeoutSeconds,
                logger
            )

            Dim frontieraRepository As ICargosFrontieraRepository = New SqlCargosFrontieraRepository(
                settings.ConnectionString,
                settings.DbCommandTimeoutSeconds
            )

            Dim cargosClient As ICargosClient = New CargosClient(settings, logger)
            Dim processor As New CargosProcessor(settings, logger, syncRepository, frontieraRepository, cargosClient)
            Environment.ExitCode = processor.Run()
        Catch ex As Exception
            logger.Error("Fatal startup error.", ex)
            Environment.ExitCode = 1
        End Try
    End Sub

    Private Sub ValidateSettings(settings As AppSettings)
        If settings Is Nothing Then
            Throw New InvalidOperationException("Settings could not be loaded.")
        End If

        If String.IsNullOrWhiteSpace(settings.ConnectionString) Then
            Throw New InvalidOperationException("Missing DB connection string. Configure 'CargosDb' in <connectionStrings> or env var 'ConnectionStrings__CargosDb'.")
        End If

        If String.IsNullOrWhiteSpace(settings.ContractsSyncProcedure) Then
            Throw New InvalidOperationException("Missing sync procedure name ('Db.ContractsSyncProcedure').")
        End If

        If Not settings.DryRun Then
            If String.IsNullOrWhiteSpace(settings.CargosBaseUrl) Then
                Throw New InvalidOperationException("Missing 'Cargos.BaseUrl'.")
            End If

            If String.IsNullOrWhiteSpace(settings.CargosUsername) Then
                Throw New InvalidOperationException("Missing 'Cargos.Username'.")
            End If

            If String.IsNullOrWhiteSpace(settings.CargosPassword) Then
                Throw New InvalidOperationException("Missing 'Cargos.Password'.")
            End If

            If String.IsNullOrWhiteSpace(settings.CargosApiKey) OrElse settings.CargosApiKey.Length < 48 Then
                Throw New InvalidOperationException("'Cargos.ApiKey' must contain at least 48 characters.")
            End If
        End If
    End Sub

End Module
