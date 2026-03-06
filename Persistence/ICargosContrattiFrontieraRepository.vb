Imports API_Cargos.Contracts

Namespace Persistence
    Public Interface ICargosContrattiFrontieraRepository
        Function ClaimEligible(maxItems As Integer, workerId As String, claimTimeoutMinutes As Integer, includeCheckOk As Boolean) As IList(Of OutboxRecord)
        Sub RegisterAttempt(itemId As Long)
        Sub SetReadyToSend(itemId As Long, recordLine As String)
        Sub SetMissingData(itemId As Long, missingFields As IList(Of String), lastError As String)
        Sub SetCheckOk(itemId As Long)
        Sub SetSentOk(itemId As Long, transactionId As String)
        Sub SetDataError(itemId As Long, lastError As String)
        Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime)
        Sub MarkMissingEmailSent(itemId As Long, missingFieldsHash As String)
        Sub MarkRejectEmailSent(itemId As Long, rejectHash As String)
    End Interface
End Namespace
