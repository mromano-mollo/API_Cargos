Imports System.Threading
Imports API_Cargos.Integration
Imports API_Cargos.Infrastructure
Imports API_Cargos.Notifications
Imports API_Cargos.Orchestration
Imports API_Cargos.Persistence
Imports API_Cargos.Validation
Imports API_Cargos.Verification

Module Module1

    Sub Main()
        Dim logger As ILogger = New ConsoleLogger()
        Dim singleInstanceMutex As Mutex = Nothing
        Dim ownsMutex As Boolean = False

        Try
            Dim settings As AppSettings = AppSettings.Load()
            ValidateSettings(settings)

            If settings.RunSelfTests Then
                SelfTestRunner.Run()
                logger.Info("Self-tests completed successfully.")
                Environment.ExitCode = 0
                Return
            End If

            Dim createdNew As Boolean
            singleInstanceMutex = New Mutex(False, "API_Cargos_SingleInstance", createdNew)
            ownsMutex = singleInstanceMutex.WaitOne(0, False)
            If Not ownsMutex Then
                logger.Warn("Another API_Cargos instance is already running. Exiting.")
                Environment.ExitCode = 1
                Return
            End If

            Dim syncRepository As ISyncRepository = New SqlSyncRepository(
                settings.ConnectionString,
                settings.DbCommandTimeoutSeconds,
                logger
            )

            If settings.CargosSyncTablesOnStartup Then
                Try
                    Dim startupReferenceTableRepository As New SqlCargosReferenceTableRepository(
                        settings.ConnectionString,
                        settings.DbCommandTimeoutSeconds
                    )
                    Dim referenceTableSyncService As ICargosReferenceTableSyncService = New CargosReferenceTableSyncService(settings, logger)
                    referenceTableSyncService.SyncAll(startupReferenceTableRepository)
                Catch ex As Exception
                    logger.Error("CaRGOS reference table sync on startup failed.", ex)
                    If settings.CargosFailStartupIfTableSyncFails Then
                        Throw
                    End If
                End Try
            End If

            Dim frontieraRepository As ICargosContrattiFrontieraRepository = New SqlCargosContrattiFrontieraRepository(
                settings.ConnectionString,
                settings.DbCommandTimeoutSeconds
            )

            Dim referenceTableRepository As ICargosReferenceTableRepository = New SqlCargosReferenceTableRepository(
                settings.ConnectionString,
                settings.DbCommandTimeoutSeconds
            )

            Dim processor As New CargosProcessor(
                settings,
                logger,
                syncRepository,
                frontieraRepository,
                New CargosClient(settings, logger),
                New CargosLookupService(referenceTableRepository),
                New ValidationService(),
                New RecordBuilder(),
                New NotificationService(settings, logger)
            )

            Dim workerId As String = Environment.MachineName & "-" & Process.GetCurrentProcess().Id.ToString()

            If settings.CargosWebSyncAgenciesOnStartup Then
                Try
                    Dim agencyBootstrapProcessor As New CargosAgencyBootstrapProcessor(
                        logger,
                        New SqlAgencySyncRepository(settings.ConnectionString, settings.DbCommandTimeoutSeconds, logger),
                        New SqlAgencyFrontieraRepository(settings.ConnectionString, settings.DbCommandTimeoutSeconds),
                        New CargosLookupService(referenceTableRepository),
                        New AgencyValidationService(),
                        New CargosWebAgencyClient(settings, logger),
                        settings.BatchSize,
                        settings.WorkerClaimTimeoutMinutes
                    )
                    agencyBootstrapProcessor.Run(settings.AgenciesSyncProcedure, workerId)
                Catch ex As Exception
                    logger.Error("CaRGOS agency bootstrap on startup failed.", ex)
                    If settings.CargosWebFailStartupIfAgencySyncFails Then
                        Throw
                    End If
                End Try
            End If

            Do While DateTime.Now.Hour < settings.WorkerCutoffHour
                Try
                    Environment.ExitCode = processor.Run(workerId)
                Catch ex As Exception
                    logger.Error("Fatal cycle error.", ex)
                    Environment.ExitCode = 1
                End Try

                If DateTime.Now.Hour >= settings.WorkerCutoffHour Then
                    Exit Do
                End If

                Thread.Sleep(settings.WorkerSleepMilliseconds)
            Loop
        Catch ex As Exception
            logger.Error("Fatal startup error.", ex)
            Environment.ExitCode = 1
        Finally
            If singleInstanceMutex IsNot Nothing AndAlso ownsMutex Then
                singleInstanceMutex.ReleaseMutex()
            End If

            If singleInstanceMutex IsNot Nothing Then
                singleInstanceMutex.Dispose()
            End If
        End Try
    End Sub

    Private Sub ValidateSettings(settings As AppSettings)
        If settings Is Nothing Then
            Throw New InvalidOperationException("Settings could not be loaded.")
        End If

        If settings.RunSelfTests Then
            Return
        End If

        If String.IsNullOrWhiteSpace(settings.ConnectionString) Then
            Throw New InvalidOperationException("Missing DB connection string. Configure 'CargosDb' in <connectionStrings> or env var 'ConnectionStrings__CargosDb'.")
        End If

        If String.IsNullOrWhiteSpace(settings.ContractsSyncProcedure) Then
            Throw New InvalidOperationException("Missing sync procedure name ('Db.ContractsSyncProcedure').")
        End If

        If settings.CargosWebSyncAgenciesOnStartup AndAlso String.IsNullOrWhiteSpace(settings.AgenciesSyncProcedure) Then
            Throw New InvalidOperationException("Missing agency sync procedure name ('Db.AgenciesSyncProcedure').")
        End If

        If settings.WorkerSleepMilliseconds <= 0 Then
            Throw New InvalidOperationException("'Worker.SleepMilliseconds' must be greater than zero.")
        End If

        If Not settings.DryRun OrElse settings.CargosSyncTablesOnStartup Then
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

        If settings.CargosWebSyncAgenciesOnStartup Then
            If String.IsNullOrWhiteSpace(settings.CargosWebBaseUrl) Then
                Throw New InvalidOperationException("Missing 'CargosWeb.BaseUrl'.")
            End If

            If String.IsNullOrWhiteSpace(settings.CargosWebAgencyCreatePath) Then
                Throw New InvalidOperationException("Missing 'CargosWeb.AgencyCreatePath'.")
            End If

            If String.IsNullOrWhiteSpace(settings.CargosWebAuthCookieHeader) Then
                If String.IsNullOrWhiteSpace(settings.CargosWebUsername) Then
                    Throw New InvalidOperationException("Missing 'CargosWeb.Username' or 'CargosWeb.AuthCookieHeader'.")
                End If

                If String.IsNullOrWhiteSpace(settings.CargosWebPassword) Then
                    Throw New InvalidOperationException("Missing 'CargosWeb.Password' or 'CargosWeb.AuthCookieHeader'.")
                End If
            End If
        End If
    End Sub

End Module
