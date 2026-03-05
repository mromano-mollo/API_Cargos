Imports API_Cargos.Contracts
Imports API_Cargos.Integration
Imports API_Cargos.Infrastructure
Imports API_Cargos.Persistence

Namespace Orchestration
    Public NotInheritable Class CargosProcessor
        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger
        Private ReadOnly _syncRepository As ISyncRepository
        Private ReadOnly _frontieraRepository As ICargosFrontieraRepository
        Private ReadOnly _cargosClient As ICargosClient

        Public Sub New(
            settings As AppSettings,
            logger As ILogger,
            syncRepository As ISyncRepository,
            frontieraRepository As ICargosFrontieraRepository,
            cargosClient As ICargosClient
        )
            _settings = settings
            _logger = logger
            _syncRepository = syncRepository
            _frontieraRepository = frontieraRepository
            _cargosClient = cargosClient
        End Sub

        Public Function Run() As Integer
            Try
                _logger.Info("CARGOS run started.")
                _logger.Info("DryRun mode: " & _settings.DryRun.ToString())

                _syncRepository.Execute(_settings.ContractsSyncProcedure)
                Dim eligibleItems = _frontieraRepository.GetEligible(_settings.BatchSize)

                _logger.Info("Eligible queue items: " & eligibleItems.Count.ToString())

                If _settings.DryRun Then
                    For Each item In eligibleItems
                        _logger.Info(String.Format(
                            "Queue item {0} | ContractNo={1} | LineNo={2} | Reason={3} | Status={4}",
                            item.Id,
                            item.ContractNo,
                            item.LineNo,
                            item.Reason,
                            item.Status
                        ))
                    Next

                    _logger.Info("CARGOS run completed.")
                    Return 0
                End If

                Dim sendable As New List(Of OutboxRecord)()
                For Each item In eligibleItems
                    If String.IsNullOrWhiteSpace(item.RecordLine) Then
                        _frontieraRepository.SetDataError(item.Id, "Missing RecordLine payload. Add RecordLine (or CargosRecordLine) to Cargos_Vista_Contratti.")
                    Else
                        sendable.Add(item)
                    End If
                Next

                For Each batch In SplitBatches(sendable, _settings.BatchSize)
                    For Each item In batch
                        _frontieraRepository.RegisterAttempt(item.Id)
                    Next

                    Dim lines As IList(Of String) = batch.Select(Function(x) x.RecordLine).ToList()
                    Dim outcomes As IList(Of CargosLineOutcome) = _cargosClient.Send(lines)

                    For i As Integer = 0 To batch.Count - 1
                        Dim item = batch(i)
                        Dim outcome As CargosLineOutcome = outcomes(i)

                        Select Case outcome.OutcomeType
                            Case CargosOutcomeType.Success
                                _frontieraRepository.SetSentOk(item.Id, outcome.TransactionId)
                            Case CargosOutcomeType.DataError
                                _frontieraRepository.SetDataError(item.Id, outcome.ErrorMessage)
                            Case Else
                                _frontieraRepository.SetRetry(
                                    item.Id,
                                    outcome.ErrorMessage,
                                    ComputeNextRetryAt(item.AttemptCount)
                                )
                        End Select
                    Next
                Next

                _logger.Info("Send pipeline completed.")

                _logger.Info("CARGOS run completed.")
                Return 0
            Catch ex As Exception
                _logger.Error("CARGOS run failed.", ex)
                Return 1
            End Try
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
                Dim currentBatch As List(Of OutboxRecord) = items.Skip(index).Take(size).ToList()
                result.Add(currentBatch)
                index += size
            End While

            Return result
        End Function
    End Class
End Namespace
