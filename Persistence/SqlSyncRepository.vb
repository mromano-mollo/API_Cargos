Imports System.Data.SqlClient
Imports API_Cargos.Infrastructure

Namespace Persistence
    Public NotInheritable Class SqlSyncRepository
        Implements ISyncRepository

        Private ReadOnly _connectionString As String
        Private ReadOnly _commandTimeoutSeconds As Integer
        Private ReadOnly _logger As ILogger

        Public Sub New(connectionString As String, commandTimeoutSeconds As Integer, logger As ILogger)
            _connectionString = connectionString
            _commandTimeoutSeconds = commandTimeoutSeconds
            _logger = logger
        End Sub

        Public Function Execute(syncProcedureName As String) As Integer Implements ISyncRepository.Execute
            If String.IsNullOrWhiteSpace(syncProcedureName) Then
                Throw New ArgumentException("Sync procedure name is required.", NameOf(syncProcedureName))
            End If

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(syncProcedureName, connection)
                    command.CommandType = CommandType.StoredProcedure
                    command.CommandTimeout = _commandTimeoutSeconds

                    Dim result As Object = command.ExecuteScalar()
                    Dim queued As Integer = 0
                    If result IsNot Nothing AndAlso result IsNot DBNull.Value Then
                        Integer.TryParse(result.ToString(), queued)
                    End If

                    _logger.Info("Sync procedure executed. Queued items: " & queued.ToString())
                    Return queued
                End Using
            End Using
        End Function
    End Class
End Namespace
