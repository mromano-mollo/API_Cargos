Imports API_Cargos.Contracts
Imports API_Cargos.Integration
Imports API_Cargos.Infrastructure
Imports API_Cargos.Notifications
Imports API_Cargos.Persistence
Imports API_Cargos.Validation

Namespace Orchestration
    Public NotInheritable Class CargosProcessor
        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger
        Private ReadOnly _syncRepository As ISyncRepository
        Private ReadOnly _frontieraRepository As ICargosFrontieraRepository
        Private ReadOnly _cargosClient As ICargosClient
        Private ReadOnly _lookupService As ICargosLookupService
        Private ReadOnly _validationService As IValidationService
        Private ReadOnly _recordBuilder As IRecordBuilder
        Private ReadOnly _notificationService As INotificationService

        Public Sub New(
            settings As AppSettings,
            logger As ILogger,
            syncRepository As ISyncRepository,
            frontieraRepository As ICargosFrontieraRepository,
            cargosClient As ICargosClient,
            lookupService As ICargosLookupService,
            validationService As IValidationService,
            recordBuilder As IRecordBuilder,
            notificationService As INotificationService
        )
            _settings = settings
            _logger = logger
            _syncRepository = syncRepository
            _frontieraRepository = frontieraRepository
            _cargosClient = cargosClient
            _lookupService = lookupService
            _validationService = validationService
            _recordBuilder = recordBuilder
            _notificationService = notificationService
        End Sub

        Public Function Run(workerId As String) As Integer
            Try
                _logger.Info("CARGOS cycle started.")
                _logger.Info("DryRun mode: " & _settings.DryRun.ToString())
                _logger.Info("CheckOnly mode: " & _settings.CargosCheckOnly.ToString())

                _syncRepository.Execute(_settings.ContractsSyncProcedure)
                Dim claimedItems = _frontieraRepository.ClaimEligible(_settings.BatchSize, workerId, _settings.WorkerClaimTimeoutMinutes, Not _settings.CargosCheckOnly)
                _logger.Info("Claimed queue items: " & claimedItems.Count.ToString())

                If _settings.DryRun Then
                    For Each item In claimedItems
                        _logger.Info(String.Format("Claimed item {0} | ContractNo={1} | LineNo={2} | Reason={3} | Status={4}", item.Id, item.ContractNo, item.LineNo, item.Reason, item.Status))
                    Next

                    _logger.Info("CARGOS cycle completed.")
                    Return 0
                End If

                Dim sendable As New List(Of OutboxRecord)()
                For Each item In claimedItems
                    Dim validation As New ValidationResult()
                    _lookupService.Resolve(item, validation)
                    validation.Merge(_validationService.Validate(item))
                    If Not validation.IsValid Then
                        _frontieraRepository.SetMissingData(item.Id, validation.MissingFields, validation.ToSummary())
                        Dim missingHash As String = _notificationService.TrySendMissingData(item, validation)
                        If Not String.IsNullOrWhiteSpace(missingHash) Then
                            _frontieraRepository.MarkMissingEmailSent(item.Id, missingHash)
                        End If
                        Continue For
                    End If

                    item.RecordLine = _recordBuilder.Build(item)
                    _frontieraRepository.SetReadyToSend(item.Id, item.RecordLine)
                    sendable.Add(item)
                Next

                For Each batch In SplitBatches(sendable, _settings.BatchSize)
                    For Each item In batch
                        _frontieraRepository.RegisterAttempt(item.Id)
                    Next

                    Dim checkedItems As IList(Of OutboxRecord) = batch
                    If _settings.CargosUseCheckEndpoint OrElse _settings.CargosCheckOnly Then
                        checkedItems = ApplyOutcomes(batch, _cargosClient.Check(batch.Select(Function(x) x.RecordLine).ToList()), True)
                    End If

                    If _settings.CargosCheckOnly Then
                        Continue For
                    End If

                    If checkedItems.Count = 0 Then
                        Continue For
                    End If

                    ApplyOutcomes(checkedItems, _cargosClient.Send(checkedItems.Select(Function(x) x.RecordLine).ToList()), False)
                Next

                _logger.Info("CARGOS cycle completed.")
                Return 0
            Catch ex As Exception
                _logger.Error("CARGOS cycle failed.", ex)
                Return 1
            End Try
        End Function

        Private Function ApplyOutcomes(batch As IList(Of OutboxRecord), outcomes As IList(Of CargosLineOutcome), isCheckOperation As Boolean) As IList(Of OutboxRecord)
            Dim remaining As New List(Of OutboxRecord)()

            For i As Integer = 0 To batch.Count - 1
                Dim item = batch(i)
                Dim outcome As CargosLineOutcome = outcomes(i)

                Select Case outcome.OutcomeType
                    Case CargosOutcomeType.Success
                        If isCheckOperation Then
                            If _settings.CargosCheckOnly Then
                                _frontieraRepository.SetCheckOk(item.Id)
                            Else
                                remaining.Add(item)
                            End If
                        Else
                            _frontieraRepository.SetSentOk(item.Id, outcome.TransactionId)
                        End If

                    Case CargosOutcomeType.DataError
                        _frontieraRepository.SetDataError(item.Id, outcome.ErrorMessage)
                        Dim rejectHash As String = _notificationService.TrySendReject(item, outcome.ErrorMessage)
                        If Not String.IsNullOrWhiteSpace(rejectHash) Then
                            _frontieraRepository.MarkRejectEmailSent(item.Id, rejectHash)
                        End If

                    Case Else
                        _frontieraRepository.SetRetry(item.Id, outcome.ErrorMessage, ComputeNextRetryAt(item.AttemptCount))
                End Select
            Next

            Return remaining
        End Function

        Private Shared Function ComputeNextRetryAt(currentAttemptCount As Integer) As DateTime
            Dim attemptNumber As Integer = currentAttemptCount + 1
            Dim minutesDelay As Integer

            Select Case attemptNumber
                Case 1
                    minutesDelay = 5
                Case 2
                    minutesDelay = 15
                Case 3
                    minutesDelay = 60
                Case Else
                    minutesDelay = 240
            End Select

            Return DateTime.UtcNow.AddMinutes(minutesDelay)
        End Function

        Private Shared Function SplitBatches(items As IList(Of OutboxRecord), batchSize As Integer) As IList(Of IList(Of OutboxRecord))
            Dim result As New List(Of IList(Of OutboxRecord))()
            Dim size As Integer = Math.Max(1, batchSize)
            Dim index As Integer = 0

            While index < items.Count
                result.Add(items.Skip(index).Take(size).ToList())
                index += size
            End While

            Return result
        End Function
    End Class
End Namespace
