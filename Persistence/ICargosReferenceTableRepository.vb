Imports System
Imports System.Collections.Generic
Imports API_Cargos.Contracts

Namespace Persistence
    Public Interface ICargosReferenceTableRepository
        Function GetRows(tableId As Integer) As IList(Of CargosReferenceTableRow)
        Sub ReplaceTable(definition As CargosReferenceTableDefinition, rows As IList(Of CargosReferenceTableRow), syncedAtUtc As DateTime)
        Sub MarkSyncFailure(definition As CargosReferenceTableDefinition, failureMessage As String, attemptedAtUtc As DateTime)
    End Interface
End Namespace
