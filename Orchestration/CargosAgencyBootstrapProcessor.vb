Imports API_Cargos.Contracts
Imports API_Cargos.Integration
Imports API_Cargos.Infrastructure
Imports API_Cargos.Persistence
Imports API_Cargos.Validation

Namespace Orchestration
    Public NotInheritable Class CargosAgencyBootstrapProcessor
        Private ReadOnly _logger As ILogger
        Private ReadOnly _syncRepository As IAgencySyncRepository
        Private ReadOnly _frontieraRepository As IAgencyFrontieraRepository
        Private ReadOnly _lookupService As ICargosLookupService
        Private ReadOnly _validationService As IAgencyValidationService
        Private ReadOnly _client As IAgencyCreateClient
        Private ReadOnly _batchSize As Integer
        Private ReadOnly _claimTimeoutMinutes As Integer

        Public Sub New(
            logger As ILogger,
            syncRepository As IAgencySyncRepository,
            frontieraRepository As IAgencyFrontieraRepository,
            lookupService As ICargosLookupService,
            validationService As IAgencyValidationService,
            client As IAgencyCreateClient,
            batchSize As Integer,
            claimTimeoutMinutes As Integer
        )
            _logger = logger
            _syncRepository = syncRepository
            _frontieraRepository = frontieraRepository
            _lookupService = lookupService
            _validationService = validationService
            _client = client
            _batchSize = batchSize
            _claimTimeoutMinutes = claimTimeoutMinutes
        End Sub

        Public Function Run(syncProcedureName As String, workerId As String) As Integer
            _logger.Info("Agency bootstrap cycle started.")
            _syncRepository.Execute(syncProcedureName)
            Dim claimedItems = _frontieraRepository.ClaimEligible(_batchSize, workerId, _claimTimeoutMinutes)
            _logger.Info("Claimed agency items: " & claimedItems.Count.ToString())

            For Each item In claimedItems
                _frontieraRepository.RegisterAttempt(item.Id)

                Dim validation As New ValidationResult()
                ResolveLookup(item, validation)
                validation.Merge(_validationService.Validate(item))

                If Not validation.IsValid Then
                    _logger.Warn(String.Format(
                        "Agency validation failed | BranchId={0} | AgenziaId={1} | Details={2}",
                        item.BranchId,
                        item.AgenziaId,
                        validation.ToSummary()
                    ))
                    _frontieraRepository.SetDataError(item.Id, validation.ToSummary())
                    Continue For
                End If

                _frontieraRepository.SetReadyToSend(item.Id, item.AgenziaLuogoCod)
                Dim outcome = _client.CreateAgency(item)

                Select Case outcome.OutcomeType
                    Case CargosOutcomeType.Success
                        _frontieraRepository.SetSentOk(item.Id)
                        _logger.Info(String.Format(
                            "Agency SENT_OK | BranchId={0} | AgenziaId={1}",
                            item.BranchId,
                            item.AgenziaId
                        ))
                    Case CargosOutcomeType.DataError
                        _frontieraRepository.SetDataError(item.Id, outcome.ErrorMessage)
                        _logger.Warn(String.Format(
                            "Agency SENT_KO_DATA | BranchId={0} | AgenziaId={1} | Error={2}",
                            item.BranchId,
                            item.AgenziaId,
                            If(outcome.ErrorMessage, String.Empty)
                        ))
                    Case Else
                        Dim nextRetryAt As DateTime = ComputeNextRetryAt(item.AttemptCount)
                        _frontieraRepository.SetRetry(item.Id, outcome.ErrorMessage, nextRetryAt)
                        _logger.Warn(String.Format(
                            "Agency SENT_KO_RETRY | BranchId={0} | AgenziaId={1} | NextRetryAt={2:yyyy-MM-dd HH:mm:ss} | Error={3}",
                            item.BranchId,
                            item.AgenziaId,
                            nextRetryAt,
                            If(outcome.ErrorMessage, String.Empty)
                        ))
                End Select
            Next

            _logger.Info("Agency bootstrap cycle completed.")
            Return claimedItems.Count
        End Function

        Private Sub ResolveLookup(item As AgencyOutboxRecord, validation As ValidationResult)
            item.AgenziaLuogoCod = _lookupService.ResolveLuogoCode(
                item.AgenziaCity,
                item.AgenziaCounty,
                item.AgenziaPostCode,
                item.AgenziaLuogoValue,
                "AGENZIA_LUOGO_COD",
                validation
            )
        End Sub

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

            Return DateTime.Now.AddMinutes(minutesDelay)
        End Function
    End Class
End Namespace
