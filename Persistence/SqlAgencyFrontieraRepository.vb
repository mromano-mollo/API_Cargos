Imports System.Data.SqlClient
Imports API_Cargos.Contracts

Namespace Persistence
    Public NotInheritable Class SqlAgencyFrontieraRepository
        Implements IAgencyFrontieraRepository

        Private ReadOnly _connectionString As String
        Private ReadOnly _commandTimeoutSeconds As Integer

        Public Sub New(connectionString As String, commandTimeoutSeconds As Integer)
            _connectionString = connectionString
            _commandTimeoutSeconds = commandTimeoutSeconds
        End Sub

        Public Function ClaimEligible(maxItems As Integer, workerId As String, claimTimeoutMinutes As Integer) As IList(Of AgencyOutboxRecord) Implements IAgencyFrontieraRepository.ClaimEligible
            Dim items As New List(Of AgencyOutboxRecord)()
            Dim staleBeforeLocal As DateTime = DateTime.Now.AddMinutes(-Math.Max(1, claimTimeoutMinutes))

            Const sql As String =
";WITH Latest AS (" & vbCrLf &
"    SELECT f.*," & vbCrLf &
"           ROW_NUMBER() OVER (PARTITION BY f.BranchId ORDER BY f.CreatedAt DESC, f.Id DESC) AS rn" & vbCrLf &
"    FROM dbo.Cargos_Agenzie_Frontiera f" & vbCrLf &
"), Candidates AS (" & vbCrLf &
"    SELECT TOP (@Take) l.Id" & vbCrLf &
"    FROM Latest l" & vbCrLf &
"    WHERE l.rn = 1" & vbCrLf &
"      AND l.Status IN ('PENDING', 'READY_TO_SEND', 'SENT_KO_RETRY')" & vbCrLf &
"      AND (l.NextRetryAt IS NULL OR l.NextRetryAt <= @NowLocal)" & vbCrLf &
"      AND (l.ClaimedAt IS NULL OR l.ClaimedAt <= @StaleBeforeLocal)" & vbCrLf &
"    ORDER BY l.CreatedAt, l.Id" & vbCrLf &
")" & vbCrLf &
"UPDATE f" & vbCrLf &
"SET ClaimedBy = @WorkerId," & vbCrLf &
"    ClaimedAt = @NowLocal," & vbCrLf &
"    UpdatedAt = @NowLocal" & vbCrLf &
"OUTPUT inserted.Id, inserted.BranchId, inserted.BranchEmail, inserted.AgenziaId, inserted.AgenziaNome, inserted.AgenziaLuogoValue, inserted.AgenziaCity, inserted.AgenziaCounty, inserted.AgenziaPostCode, inserted.AgenziaLuogoCod, inserted.AgenziaIndirizzo, inserted.AgenziaRecapitoTel, inserted.Reason, inserted.SnapshotHash, inserted.Status, inserted.LastError, inserted.AttemptCount, inserted.LastAttemptAt, inserted.NextRetryAt, inserted.CreatedAt" & vbCrLf &
"FROM dbo.Cargos_Agenzie_Frontiera f" & vbCrLf &
"INNER JOIN Candidates c ON c.Id = f.Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Take", SqlDbType.Int).Value = Math.Max(1, maxItems)
                    command.Parameters.Add("@WorkerId", SqlDbType.NVarChar, 100).Value = workerId
                    command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
                    command.Parameters.Add("@StaleBeforeLocal", SqlDbType.DateTime2).Value = staleBeforeLocal

                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            Dim item As New AgencyOutboxRecord() With {
                                .Id = Convert.ToInt64(reader("Id")),
                                .BranchId = Convert.ToString(reader("BranchId")),
                                .BranchEmail = Convert.ToString(reader("BranchEmail")),
                                .AgenziaId = Convert.ToString(reader("AgenziaId")),
                                .AgenziaNome = Convert.ToString(reader("AgenziaNome")),
                                .AgenziaLuogoValue = Convert.ToString(reader("AgenziaLuogoValue")),
                                .AgenziaCity = Convert.ToString(reader("AgenziaCity")),
                                .AgenziaCounty = Convert.ToString(reader("AgenziaCounty")),
                                .AgenziaPostCode = Convert.ToString(reader("AgenziaPostCode")),
                                .AgenziaLuogoCod = Convert.ToString(reader("AgenziaLuogoCod")),
                                .AgenziaIndirizzo = Convert.ToString(reader("AgenziaIndirizzo")),
                                .AgenziaRecapitoTel = Convert.ToString(reader("AgenziaRecapitoTel")),
                                .Reason = Convert.ToString(reader("Reason")),
                                .SnapshotHash = Convert.ToString(reader("SnapshotHash")),
                                .Status = Convert.ToString(reader("Status")),
                                .LastError = Convert.ToString(reader("LastError")),
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

        Public Sub RegisterAttempt(itemId As Long) Implements IAgencyFrontieraRepository.RegisterAttempt
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Agenzie_Frontiera SET AttemptCount = AttemptCount + 1, LastAttemptAt = @NowLocal, UpdatedAt = @NowLocal WHERE Id = @Id;",
                itemId,
                Sub(command) command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
            )
        End Sub

        Public Sub SetReadyToSend(itemId As Long, luogoCod As String) Implements IAgencyFrontieraRepository.SetReadyToSend
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Agenzie_Frontiera SET Status = 'READY_TO_SEND', AgenziaLuogoCod = @LuogoCod, LastError = NULL, UpdatedAt = @NowLocal WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@LuogoCod", SqlDbType.NVarChar, 9).Value = If(luogoCod, String.Empty)
                    command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
                End Sub
            )
        End Sub

        Public Sub SetSentOk(itemId As Long) Implements IAgencyFrontieraRepository.SetSentOk
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Agenzie_Frontiera SET Status = 'SENT_OK', LastError = NULL, NextRetryAt = NULL, ClaimedBy = NULL, ClaimedAt = NULL, UpdatedAt = @NowLocal WHERE Id = @Id;",
                itemId,
                Sub(command) command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
            )
        End Sub

        Public Sub SetDataError(itemId As Long, lastError As String) Implements IAgencyFrontieraRepository.SetDataError
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Agenzie_Frontiera SET Status = 'SENT_KO_DATA', LastError = @LastError, NextRetryAt = NULL, ClaimedBy = NULL, ClaimedAt = NULL, UpdatedAt = @NowLocal WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
                End Sub
            )
        End Sub

        Public Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime) Implements IAgencyFrontieraRepository.SetRetry
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Agenzie_Frontiera SET Status = 'SENT_KO_RETRY', LastError = @LastError, NextRetryAt = @NextRetryAt, ClaimedBy = NULL, ClaimedAt = NULL, UpdatedAt = @NowLocal WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NextRetryAt", SqlDbType.DateTime2).Value = nextRetryAt
                    command.Parameters.Add("@NowLocal", SqlDbType.DateTime2).Value = DateTime.Now
                End Sub
            )
        End Sub

        Private Sub ExecuteNonQuery(sql As String, itemId As Long, configure As Action(Of SqlCommand))
            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    configure(command)
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub
    End Class
End Namespace
