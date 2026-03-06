Imports System
Imports System.Collections.Generic
Imports API_Cargos.Contracts

Namespace Persistence
    Public Interface IAgencyFrontieraRepository
        Function ClaimEligible(maxItems As Integer, workerId As String, claimTimeoutMinutes As Integer) As IList(Of AgencyOutboxRecord)
        Sub RegisterAttempt(itemId As Long)
        Sub SetReadyToSend(itemId As Long, luogoCod As String)
        Sub SetSentOk(itemId As Long)
        Sub SetDataError(itemId As Long, lastError As String)
        Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime)
    End Interface
End Namespace
