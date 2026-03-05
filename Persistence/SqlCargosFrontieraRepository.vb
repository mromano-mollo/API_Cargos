Imports System.Data.SqlClient
Imports API_Cargos.Contracts

Namespace Persistence
    Public NotInheritable Class SqlCargosFrontieraRepository
        Implements ICargosFrontieraRepository

        Private ReadOnly _connectionString As String
        Private ReadOnly _commandTimeoutSeconds As Integer

        Public Sub New(connectionString As String, commandTimeoutSeconds As Integer)
            _connectionString = connectionString
            _commandTimeoutSeconds = commandTimeoutSeconds
        End Sub

        Public Function GetEligible(maxItems As Integer) As IList(Of OutboxRecord) Implements ICargosFrontieraRepository.GetEligible
            Dim items As New List(Of OutboxRecord)()
            Dim take As Integer = Math.Max(1, maxItems)

            Const sql As String =
";WITH Ranked AS (" & vbCrLf &
"    SELECT" & vbCrLf &
"        f.Id," & vbCrLf &
"        f.ContractNo," & vbCrLf &
"        f.LineNo," & vbCrLf &
"        f.CargosContractId," & vbCrLf &
"        f.BranchId," & vbCrLf &
"        f.Reason," & vbCrLf &
"        f.SnapshotHash," & vbCrLf &
"        f.RecordLine," & vbCrLf &
"        f.Status," & vbCrLf &
"        f.AttemptCount," & vbCrLf &
"        f.LastAttemptAt," & vbCrLf &
"        f.NextRetryAt," & vbCrLf &
"        f.CreatedAt," & vbCrLf &
"        ROW_NUMBER() OVER (" & vbCrLf &
"            PARTITION BY f.ContractNo, f.LineNo" & vbCrLf &
"            ORDER BY f.CreatedAt DESC, f.Id DESC" & vbCrLf &
"        ) AS rn" & vbCrLf &
"    FROM dbo.Cargos_Frontiera f" & vbCrLf &
"    WHERE f.Status IN ('PENDING', 'READY_TO_SEND', 'SENT_KO_RETRY')" & vbCrLf &
"      AND (f.NextRetryAt IS NULL OR f.NextRetryAt <= @NowUtc)" & vbCrLf &
")" & vbCrLf &
"SELECT TOP (@Take)" & vbCrLf &
"    Id," & vbCrLf &
"    ContractNo," & vbCrLf &
"    LineNo," & vbCrLf &
"    CargosContractId," & vbCrLf &
"    BranchId," & vbCrLf &
"    Reason," & vbCrLf &
"    SnapshotHash," & vbCrLf &
"    RecordLine," & vbCrLf &
"    Status," & vbCrLf &
"    AttemptCount," & vbCrLf &
"    LastAttemptAt," & vbCrLf &
"    NextRetryAt," & vbCrLf &
"    CreatedAt" & vbCrLf &
"FROM Ranked" & vbCrLf &
"WHERE rn = 1" & vbCrLf &
"ORDER BY CreatedAt, Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Take", SqlDbType.Int).Value = take
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow

                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim item As New OutboxRecord() With {
                                .Id = Convert.ToInt64(reader("Id")),
                                .ContractNo = Convert.ToString(reader("ContractNo")),
                                .LineNo = Convert.ToInt64(reader("LineNo")),
                                .CargosContractId = Convert.ToString(reader("CargosContractId")),
                                .BranchId = Convert.ToString(reader("BranchId")),
                                .Reason = Convert.ToString(reader("Reason")),
                                .SnapshotHash = Convert.ToString(reader("SnapshotHash")),
                                .RecordLine = Convert.ToString(reader("RecordLine")),
                                .Status = Convert.ToString(reader("Status")),
                                .AttemptCount = Convert.ToInt32(reader("AttemptCount")),
                                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
                            }

                            If Not Convert.IsDBNull(reader("LastAttemptAt")) Then
                                item.LastAttemptAt = Convert.ToDateTime(reader("LastAttemptAt"))
                            End If

                            If Not Convert.IsDBNull(reader("NextRetryAt")) Then
                                item.NextRetryAt = Convert.ToDateTime(reader("NextRetryAt"))
                            End If

                            items.Add(item)
                        End While
                    End Using
                End Using
            End Using

            Return items
        End Function

        Public Sub RegisterAttempt(itemId As Long) Implements ICargosFrontieraRepository.RegisterAttempt
            Const sql As String =
"UPDATE dbo.Cargos_Frontiera" & vbCrLf &
"SET AttemptCount = AttemptCount + 1," & vbCrLf &
"    LastAttemptAt = @NowUtc," & vbCrLf &
"    UpdatedAt = @NowUtc" & vbCrLf &
"WHERE Id = @Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub SetSentOk(itemId As Long, transactionId As String) Implements ICargosFrontieraRepository.SetSentOk
            Const sql As String =
"UPDATE dbo.Cargos_Frontiera" & vbCrLf &
"SET Status = 'SENT_OK'," & vbCrLf &
"    TransactionId = @TransactionId," & vbCrLf &
"    LastError = NULL," & vbCrLf &
"    NextRetryAt = NULL," & vbCrLf &
"    UpdatedAt = @NowUtc" & vbCrLf &
"WHERE Id = @Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    command.Parameters.Add("@TransactionId", SqlDbType.NVarChar, 100).Value = If(transactionId, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub SetDataError(itemId As Long, lastError As String) Implements ICargosFrontieraRepository.SetDataError
            Const sql As String =
"UPDATE dbo.Cargos_Frontiera" & vbCrLf &
"SET Status = 'SENT_KO_DATA'," & vbCrLf &
"    LastError = @LastError," & vbCrLf &
"    NextRetryAt = NULL," & vbCrLf &
"    UpdatedAt = @NowUtc" & vbCrLf &
"WHERE Id = @Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime) Implements ICargosFrontieraRepository.SetRetry
            Const sql As String =
"UPDATE dbo.Cargos_Frontiera" & vbCrLf &
"SET Status = 'SENT_KO_RETRY'," & vbCrLf &
"    LastError = @LastError," & vbCrLf &
"    NextRetryAt = @NextRetryAt," & vbCrLf &
"    UpdatedAt = @NowUtc" & vbCrLf &
"WHERE Id = @Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NextRetryAt", SqlDbType.DateTime2).Value = nextRetryAt
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class
End Namespace
