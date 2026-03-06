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

        Public Function ClaimEligible(maxItems As Integer, workerId As String, claimTimeoutMinutes As Integer, includeCheckOk As Boolean) As IList(Of OutboxRecord) Implements ICargosFrontieraRepository.ClaimEligible
            Dim items As New List(Of OutboxRecord)()
            Dim take As Integer = Math.Max(1, maxItems)
            Dim staleBeforeUtc As DateTime = DateTime.UtcNow.AddMinutes(-Math.Max(1, claimTimeoutMinutes))

            Const sql As String =
";WITH Latest AS (" & vbCrLf &
"    SELECT" & vbCrLf &
"        f.*," & vbCrLf &
"        ROW_NUMBER() OVER (" & vbCrLf &
"            PARTITION BY f.ContractNo, f.LineNo" & vbCrLf &
"            ORDER BY f.CreatedAt DESC, f.Id DESC" & vbCrLf &
"        ) AS rn" & vbCrLf &
"    FROM dbo.Cargos_Frontiera f" & vbCrLf &
"), Candidates AS (" & vbCrLf &
"    SELECT TOP (@Take) l.Id" & vbCrLf &
"    FROM Latest l" & vbCrLf &
"    WHERE l.rn = 1" & vbCrLf &
"      AND (" & vbCrLf &
"            l.Status IN ('PENDING', 'READY_TO_SEND', 'SENT_KO_RETRY')" & vbCrLf &
"            OR (@IncludeCheckOk = 1 AND l.Status = 'CHECK_OK')" & vbCrLf &
"          )" & vbCrLf &
"      AND (l.NextRetryAt IS NULL OR l.NextRetryAt <= @NowUtc)" & vbCrLf &
"      AND (l.ClaimedAt IS NULL OR l.ClaimedAt <= @StaleBeforeUtc)" & vbCrLf &
"    ORDER BY l.CreatedAt, l.Id" & vbCrLf &
")" & vbCrLf &
"UPDATE f" & vbCrLf &
"SET ClaimedBy = @WorkerId," & vbCrLf &
"    ClaimedAt = @NowUtc," & vbCrLf &
"    UpdatedAt = @NowUtc" & vbCrLf &
"OUTPUT" & vbCrLf &
"    inserted.Id," & vbCrLf &
"    inserted.ContractNo," & vbCrLf &
"    inserted.LineNo," & vbCrLf &
"    inserted.CargosContractId," & vbCrLf &
"    inserted.BranchId," & vbCrLf &
"    inserted.BranchEmail," & vbCrLf &
"    inserted.ContrattoId," & vbCrLf &
"    inserted.ContrattoData," & vbCrLf &
"    inserted.ContrattoTipoP," & vbCrLf &
"    inserted.ContrattoCheckoutData," & vbCrLf &
"    inserted.ContrattoCheckoutLuogoCod," & vbCrLf &
"    inserted.ContrattoCheckoutIndirizzo," & vbCrLf &
"    inserted.ContrattoCheckinData," & vbCrLf &
"    inserted.ContrattoCheckinLuogoCod," & vbCrLf &
"    inserted.ContrattoCheckinIndirizzo," & vbCrLf &
"    inserted.OperatoreId," & vbCrLf &
"    inserted.AgenziaId," & vbCrLf &
"    inserted.AgenziaNome," & vbCrLf &
"    inserted.AgenziaLuogoCod," & vbCrLf &
"    inserted.AgenziaIndirizzo," & vbCrLf &
"    inserted.AgenziaRecapitoTel," & vbCrLf &
"    inserted.VeicoloTipo," & vbCrLf &
"    inserted.VeicoloMarca," & vbCrLf &
"    inserted.VeicoloModello," & vbCrLf &
"    inserted.VeicoloTarga," & vbCrLf &
"    inserted.ConducenteContraenteCognome," & vbCrLf &
"    inserted.ConducenteContraenteNome," & vbCrLf &
"    inserted.ConducenteContraenteNascitaData," & vbCrLf &
"    inserted.ConducenteContraenteNascitaLuogoCod," & vbCrLf &
"    inserted.ConducenteContraenteCittadinanzaCod," & vbCrLf &
"    inserted.ConducenteContraenteDocideTipoCod," & vbCrLf &
"    inserted.ConducenteContraenteDocideNumero," & vbCrLf &
"    inserted.ConducenteContraenteDocideLuogorilCod," & vbCrLf &
"    inserted.ConducenteContraentePatenteNumero," & vbCrLf &
"    inserted.ConducenteContraentePatenteLuogorilCod," & vbCrLf &
"    inserted.Reason," & vbCrLf &
"    inserted.SnapshotHash," & vbCrLf &
"    inserted.RecordLine," & vbCrLf &
"    inserted.Status," & vbCrLf &
"    inserted.MissingFields," & vbCrLf &
"    inserted.LastError," & vbCrLf &
"    inserted.AttemptCount," & vbCrLf &
"    inserted.LastAttemptAt," & vbCrLf &
"    inserted.NextRetryAt," & vbCrLf &
"    inserted.LastMissingEmailAt," & vbCrLf &
"    inserted.LastMissingFieldsHash," & vbCrLf &
"    inserted.LastRejectEmailAt," & vbCrLf &
"    inserted.LastRejectHash," & vbCrLf &
"    inserted.CreatedAt" & vbCrLf &
"FROM dbo.Cargos_Frontiera f" & vbCrLf &
"INNER JOIN Candidates c ON c.Id = f.Id;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Take", SqlDbType.Int).Value = take
                    command.Parameters.Add("@WorkerId", SqlDbType.NVarChar, 100).Value = workerId
                    command.Parameters.Add("@IncludeCheckOk", SqlDbType.Bit).Value = includeCheckOk
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                    command.Parameters.Add("@StaleBeforeUtc", SqlDbType.DateTime2).Value = staleBeforeUtc

                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            items.Add(MapOutboxRecord(reader))
                        End While
                    End Using
                End Using
            End Using

            Return items
        End Function

        Public Sub RegisterAttempt(itemId As Long) Implements ICargosFrontieraRepository.RegisterAttempt
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET AttemptCount = AttemptCount + 1," & vbCrLf &
                "    LastAttemptAt = @NowUtc," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetReadyToSend(itemId As Long, recordLine As String) Implements ICargosFrontieraRepository.SetReadyToSend
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'READY_TO_SEND'," & vbCrLf &
                "    RecordLine = @RecordLine," & vbCrLf &
                "    MissingFields = NULL," & vbCrLf &
                "    LastError = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@RecordLine", SqlDbType.NVarChar, -1).Value = If(recordLine, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetMissingData(itemId As Long, missingFields As IList(Of String), lastError As String) Implements ICargosFrontieraRepository.SetMissingData
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'MISSING_DATA'," & vbCrLf &
                "    MissingFields = @MissingFields," & vbCrLf &
                "    LastError = @LastError," & vbCrLf &
                "    NextRetryAt = NULL," & vbCrLf &
                "    ClaimedBy = NULL," & vbCrLf &
                "    ClaimedAt = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@MissingFields", SqlDbType.NVarChar, -1).Value = String.Join(",", missingFields)
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetCheckOk(itemId As Long) Implements ICargosFrontieraRepository.SetCheckOk
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'CHECK_OK'," & vbCrLf &
                "    LastError = NULL," & vbCrLf &
                "    ClaimedBy = NULL," & vbCrLf &
                "    ClaimedAt = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetSentOk(itemId As Long, transactionId As String) Implements ICargosFrontieraRepository.SetSentOk
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'SENT_OK'," & vbCrLf &
                "    TransactionId = @TransactionId," & vbCrLf &
                "    MissingFields = NULL," & vbCrLf &
                "    LastError = NULL," & vbCrLf &
                "    NextRetryAt = NULL," & vbCrLf &
                "    ClaimedBy = NULL," & vbCrLf &
                "    ClaimedAt = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@TransactionId", SqlDbType.NVarChar, 100).Value = If(transactionId, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetDataError(itemId As Long, lastError As String) Implements ICargosFrontieraRepository.SetDataError
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'SENT_KO_DATA'," & vbCrLf &
                "    LastError = @LastError," & vbCrLf &
                "    NextRetryAt = NULL," & vbCrLf &
                "    ClaimedBy = NULL," & vbCrLf &
                "    ClaimedAt = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub SetRetry(itemId As Long, lastError As String, nextRetryAt As DateTime) Implements ICargosFrontieraRepository.SetRetry
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET Status = 'SENT_KO_RETRY'," & vbCrLf &
                "    LastError = @LastError," & vbCrLf &
                "    NextRetryAt = @NextRetryAt," & vbCrLf &
                "    ClaimedBy = NULL," & vbCrLf &
                "    ClaimedAt = NULL," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@LastError", SqlDbType.NVarChar, -1).Value = If(lastError, String.Empty)
                    command.Parameters.Add("@NextRetryAt", SqlDbType.DateTime2).Value = nextRetryAt
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub MarkMissingEmailSent(itemId As Long, missingFieldsHash As String) Implements ICargosFrontieraRepository.MarkMissingEmailSent
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET LastMissingEmailAt = @NowUtc," & vbCrLf &
                "    LastMissingFieldsHash = @DetailHash," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@DetailHash", SqlDbType.NVarChar, 128).Value = If(missingFieldsHash, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Public Sub MarkRejectEmailSent(itemId As Long, rejectHash As String) Implements ICargosFrontieraRepository.MarkRejectEmailSent
            ExecuteNonQuery(
                "UPDATE dbo.Cargos_Frontiera" & vbCrLf &
                "SET LastRejectEmailAt = @NowUtc," & vbCrLf &
                "    LastRejectHash = @DetailHash," & vbCrLf &
                "    UpdatedAt = @NowUtc" & vbCrLf &
                "WHERE Id = @Id;",
                itemId,
                Sub(command)
                    command.Parameters.Add("@DetailHash", SqlDbType.NVarChar, 128).Value = If(rejectHash, String.Empty)
                    command.Parameters.Add("@NowUtc", SqlDbType.DateTime2).Value = DateTime.UtcNow
                End Sub
            )
        End Sub

        Private Sub ExecuteNonQuery(sql As String, itemId As Long, configure As Action(Of SqlCommand))
            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandType = CommandType.Text
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@Id", SqlDbType.BigInt).Value = itemId
                    configure(command)
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Shared Function MapOutboxRecord(reader As SqlDataReader) As OutboxRecord
            Dim item As New OutboxRecord() With {
                .Id = Convert.ToInt64(reader("Id")),
                .ContractNo = Convert.ToString(reader("ContractNo")),
                .LineNo = Convert.ToInt64(reader("LineNo")),
                .CargosContractId = Convert.ToString(reader("CargosContractId")),
                .BranchId = Convert.ToString(reader("BranchId")),
                .BranchEmail = Convert.ToString(reader("BranchEmail")),
                .ContrattoId = Convert.ToString(reader("ContrattoId")),
                .ContrattoTipoP = Convert.ToString(reader("ContrattoTipoP")),
                .ContrattoCheckoutLuogoCod = Convert.ToString(reader("ContrattoCheckoutLuogoCod")),
                .ContrattoCheckoutIndirizzo = Convert.ToString(reader("ContrattoCheckoutIndirizzo")),
                .ContrattoCheckinLuogoCod = Convert.ToString(reader("ContrattoCheckinLuogoCod")),
                .ContrattoCheckinIndirizzo = Convert.ToString(reader("ContrattoCheckinIndirizzo")),
                .OperatoreId = Convert.ToString(reader("OperatoreId")),
                .AgenziaId = Convert.ToString(reader("AgenziaId")),
                .AgenziaNome = Convert.ToString(reader("AgenziaNome")),
                .AgenziaLuogoCod = Convert.ToString(reader("AgenziaLuogoCod")),
                .AgenziaIndirizzo = Convert.ToString(reader("AgenziaIndirizzo")),
                .AgenziaRecapitoTel = Convert.ToString(reader("AgenziaRecapitoTel")),
                .VeicoloTipo = Convert.ToString(reader("VeicoloTipo")),
                .VeicoloMarca = Convert.ToString(reader("VeicoloMarca")),
                .VeicoloModello = Convert.ToString(reader("VeicoloModello")),
                .VeicoloTarga = Convert.ToString(reader("VeicoloTarga")),
                .ConducenteContraenteCognome = Convert.ToString(reader("ConducenteContraenteCognome")),
                .ConducenteContraenteNome = Convert.ToString(reader("ConducenteContraenteNome")),
                .ConducenteContraenteNascitaLuogoCod = Convert.ToString(reader("ConducenteContraenteNascitaLuogoCod")),
                .ConducenteContraenteCittadinanzaCod = Convert.ToString(reader("ConducenteContraenteCittadinanzaCod")),
                .ConducenteContraenteDocideTipoCod = Convert.ToString(reader("ConducenteContraenteDocideTipoCod")),
                .ConducenteContraenteDocideNumero = Convert.ToString(reader("ConducenteContraenteDocideNumero")),
                .ConducenteContraenteDocideLuogorilCod = Convert.ToString(reader("ConducenteContraenteDocideLuogorilCod")),
                .ConducenteContraentePatenteNumero = Convert.ToString(reader("ConducenteContraentePatenteNumero")),
                .ConducenteContraentePatenteLuogorilCod = Convert.ToString(reader("ConducenteContraentePatenteLuogorilCod")),
                .Reason = Convert.ToString(reader("Reason")),
                .SnapshotHash = Convert.ToString(reader("SnapshotHash")),
                .RecordLine = Convert.ToString(reader("RecordLine")),
                .Status = Convert.ToString(reader("Status")),
                .MissingFields = Convert.ToString(reader("MissingFields")),
                .LastError = Convert.ToString(reader("LastError")),
                .AttemptCount = Convert.ToInt32(reader("AttemptCount")),
                .LastMissingFieldsHash = Convert.ToString(reader("LastMissingFieldsHash")),
                .LastRejectHash = Convert.ToString(reader("LastRejectHash")),
                .CreatedAt = Convert.ToDateTime(reader("CreatedAt"))
            }

            If Not Convert.IsDBNull(reader("ContrattoData")) Then
                item.ContrattoData = Convert.ToDateTime(reader("ContrattoData"))
            End If

            If Not Convert.IsDBNull(reader("ContrattoCheckoutData")) Then
                item.ContrattoCheckoutData = Convert.ToDateTime(reader("ContrattoCheckoutData"))
            End If

            If Not Convert.IsDBNull(reader("ContrattoCheckinData")) Then
                item.ContrattoCheckinData = Convert.ToDateTime(reader("ContrattoCheckinData"))
            End If

            If Not Convert.IsDBNull(reader("ConducenteContraenteNascitaData")) Then
                item.ConducenteContraenteNascitaData = Convert.ToDateTime(reader("ConducenteContraenteNascitaData"))
            End If

            If Not Convert.IsDBNull(reader("LastAttemptAt")) Then
                item.LastAttemptAt = Convert.ToDateTime(reader("LastAttemptAt"))
            End If

            If Not Convert.IsDBNull(reader("NextRetryAt")) Then
                item.NextRetryAt = Convert.ToDateTime(reader("NextRetryAt"))
            End If

            If Not Convert.IsDBNull(reader("LastMissingEmailAt")) Then
                item.LastMissingEmailAt = Convert.ToDateTime(reader("LastMissingEmailAt"))
            End If

            If Not Convert.IsDBNull(reader("LastRejectEmailAt")) Then
                item.LastRejectEmailAt = Convert.ToDateTime(reader("LastRejectEmailAt"))
            End If

            Return item
        End Function
    End Class
End Namespace
