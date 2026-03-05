Imports API_Cargos.Contracts

Namespace Persistence
    Public Interface ICargosFrontieraRepository
        Function GetEligible(maxItems As Integer) As IList(Of OutboxRecord)
        Sub RegisterAttempt(itemId As Long)
        Sub SetSentOk(itemId As Long, transactionId As String)
        Sub SetDataError(itemId As Long, lastError As String)
        Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime)
    End Interface
End Namespace
