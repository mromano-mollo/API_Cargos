Imports System.Data.SqlClient
Imports API_Cargos.Contracts

Namespace Persistence
    Public NotInheritable Class SqlCargosReferenceTableRepository
        Implements ICargosReferenceTableRepository

        Private ReadOnly _connectionString As String
        Private ReadOnly _commandTimeoutSeconds As Integer

        Public Sub New(connectionString As String, commandTimeoutSeconds As Integer)
            _connectionString = connectionString
            _commandTimeoutSeconds = commandTimeoutSeconds
        End Sub

        Public Function GetRows(tableId As Integer) As IList(Of CargosReferenceTableRow) Implements ICargosReferenceTableRepository.GetRows
            Dim rows As New List(Of CargosReferenceTableRow)()

            Const sql As String =
"SELECT" & vbCrLf &
"    RowNumber," & vbCrLf &
"    Code," & vbCrLf &
"    [Description]," & vbCrLf &
"    Column3," & vbCrLf &
"    Column4," & vbCrLf &
"    Column5," & vbCrLf &
"    Column6," & vbCrLf &
"    Column7," & vbCrLf &
"    Column8," & vbCrLf &
"    RawLine" & vbCrLf &
"FROM dbo.Cargos_Tabella_Righe" & vbCrLf &
"WHERE TableId = @TableId" & vbCrLf &
"ORDER BY RowNumber;"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@TableId", SqlDbType.Int).Value = tableId

                    Using reader As SqlDataReader = command.ExecuteReader()
                        While reader.Read()
                            rows.Add(New CargosReferenceTableRow() With {
                                .RowNumber = Convert.ToInt32(reader("RowNumber")),
                                .Code = Convert.ToString(reader("Code")),
                                .Description = Convert.ToString(reader("Description")),
                                .Column3 = Convert.ToString(reader("Column3")),
                                .Column4 = Convert.ToString(reader("Column4")),
                                .Column5 = Convert.ToString(reader("Column5")),
                                .Column6 = Convert.ToString(reader("Column6")),
                                .Column7 = Convert.ToString(reader("Column7")),
                                .Column8 = Convert.ToString(reader("Column8")),
                                .RawLine = Convert.ToString(reader("RawLine"))
                            })
                        End While
                    End Using
                End Using
            End Using

            Return rows
        End Function

        Public Sub ReplaceTable(definition As CargosReferenceTableDefinition, rows As IList(Of CargosReferenceTableRow), syncedAtUtc As DateTime) Implements ICargosReferenceTableRepository.ReplaceTable
            If definition Is Nothing Then
                Throw New ArgumentNullException(NameOf(definition))
            End If

            If rows Is Nothing Then
                Throw New ArgumentNullException(NameOf(rows))
            End If

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using transaction = connection.BeginTransaction()
                    UpsertTableMetadata(connection, transaction, definition, syncedAtUtc, rows.Count)
                    DeleteExistingRows(connection, transaction, definition.TableId)
                    InsertRows(connection, transaction, definition.TableId, rows, syncedAtUtc)
                    transaction.Commit()
                End Using
            End Using
        End Sub

        Public Sub MarkSyncFailure(definition As CargosReferenceTableDefinition, failureMessage As String, attemptedAtUtc As DateTime) Implements ICargosReferenceTableRepository.MarkSyncFailure
            If definition Is Nothing Then
                Throw New ArgumentNullException(NameOf(definition))
            End If

            Const sql As String =
"MERGE dbo.Cargos_Tabella AS tgt" & vbCrLf &
"USING (SELECT @TableId AS TableId, @TableName AS TableName) AS src" & vbCrLf &
"    ON tgt.TableId = src.TableId" & vbCrLf &
"WHEN MATCHED THEN" & vbCrLf &
"    UPDATE SET" & vbCrLf &
"        tgt.TableName = src.TableName," & vbCrLf &
"        tgt.LastSyncStatus = 'FAILED'," & vbCrLf &
"        tgt.LastSyncError = @FailureMessage," & vbCrLf &
"        tgt.UpdatedAt = @AttemptedAtUtc" & vbCrLf &
"WHEN NOT MATCHED THEN" & vbCrLf &
"    INSERT (TableId, TableName, LastSyncedAt, LastSyncStatus, LastSyncError, RowCount, CreatedAt, UpdatedAt)" & vbCrLf &
"    VALUES (src.TableId, src.TableName, NULL, 'FAILED', @FailureMessage, 0, @AttemptedAtUtc, @AttemptedAtUtc);"

            Using connection As New SqlConnection(_connectionString)
                connection.Open()

                Using command As New SqlCommand(sql, connection)
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@TableId", SqlDbType.Int).Value = definition.TableId
                    command.Parameters.Add("@TableName", SqlDbType.NVarChar, 50).Value = definition.TableName
                    command.Parameters.Add("@FailureMessage", SqlDbType.NVarChar, -1).Value = If(failureMessage, String.Empty)
                    command.Parameters.Add("@AttemptedAtUtc", SqlDbType.DateTime2).Value = attemptedAtUtc
                    command.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Sub UpsertTableMetadata(connection As SqlConnection, transaction As SqlTransaction, definition As CargosReferenceTableDefinition, syncedAtUtc As DateTime, rowCount As Integer)
            Const sql As String =
"MERGE dbo.Cargos_Tabella AS tgt" & vbCrLf &
"USING (SELECT @TableId AS TableId, @TableName AS TableName) AS src" & vbCrLf &
"    ON tgt.TableId = src.TableId" & vbCrLf &
"WHEN MATCHED THEN" & vbCrLf &
"    UPDATE SET" & vbCrLf &
"        tgt.TableName = src.TableName," & vbCrLf &
"        tgt.LastSyncedAt = @SyncedAtUtc," & vbCrLf &
"        tgt.LastSyncStatus = 'OK'," & vbCrLf &
"        tgt.LastSyncError = NULL," & vbCrLf &
"        tgt.RowCount = @RowCount," & vbCrLf &
"        tgt.UpdatedAt = @SyncedAtUtc" & vbCrLf &
"WHEN NOT MATCHED THEN" & vbCrLf &
"    INSERT (TableId, TableName, LastSyncedAt, LastSyncStatus, LastSyncError, RowCount, CreatedAt, UpdatedAt)" & vbCrLf &
"    VALUES (src.TableId, src.TableName, @SyncedAtUtc, 'OK', NULL, @RowCount, @SyncedAtUtc, @SyncedAtUtc);"

            Using command As New SqlCommand(sql, connection, transaction)
                command.CommandTimeout = _commandTimeoutSeconds
                command.Parameters.Add("@TableId", SqlDbType.Int).Value = definition.TableId
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 50).Value = definition.TableName
                command.Parameters.Add("@SyncedAtUtc", SqlDbType.DateTime2).Value = syncedAtUtc
                command.Parameters.Add("@RowCount", SqlDbType.Int).Value = rowCount
                command.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub DeleteExistingRows(connection As SqlConnection, transaction As SqlTransaction, tableId As Integer)
            Const sql As String = "DELETE FROM dbo.Cargos_Tabella_Righe WHERE TableId = @TableId;"

            Using command As New SqlCommand(sql, connection, transaction)
                command.CommandTimeout = _commandTimeoutSeconds
                command.Parameters.Add("@TableId", SqlDbType.Int).Value = tableId
                command.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub InsertRows(connection As SqlConnection, transaction As SqlTransaction, tableId As Integer, rows As IList(Of CargosReferenceTableRow), syncedAtUtc As DateTime)
            Const sql As String =
"INSERT INTO dbo.Cargos_Tabella_Righe" & vbCrLf &
"(" & vbCrLf &
"    TableId, RowNumber, Code, [Description], Column3, Column4, Column5, Column6, Column7, Column8, RawLine, SyncedAt, CreatedAt, UpdatedAt" & vbCrLf &
")" & vbCrLf &
"VALUES" & vbCrLf &
"(" & vbCrLf &
"    @TableId, @RowNumber, @Code, @Description, @Column3, @Column4, @Column5, @Column6, @Column7, @Column8, @RawLine, @SyncedAtUtc, @SyncedAtUtc, @SyncedAtUtc" & vbCrLf &
");"

            For Each row In rows
                Using command As New SqlCommand(sql, connection, transaction)
                    command.CommandTimeout = _commandTimeoutSeconds
                    command.Parameters.Add("@TableId", SqlDbType.Int).Value = tableId
                    command.Parameters.Add("@RowNumber", SqlDbType.Int).Value = row.RowNumber
                    command.Parameters.Add("@Code", SqlDbType.NVarChar, 100).Value = If(row.Code, String.Empty)
                    command.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = If(row.Description, String.Empty)
                    command.Parameters.Add("@Column3", SqlDbType.NVarChar, 255).Value = If(row.Column3, String.Empty)
                    command.Parameters.Add("@Column4", SqlDbType.NVarChar, 255).Value = If(row.Column4, String.Empty)
                    command.Parameters.Add("@Column5", SqlDbType.NVarChar, 255).Value = If(row.Column5, String.Empty)
                    command.Parameters.Add("@Column6", SqlDbType.NVarChar, 255).Value = If(row.Column6, String.Empty)
                    command.Parameters.Add("@Column7", SqlDbType.NVarChar, 255).Value = If(row.Column7, String.Empty)
                    command.Parameters.Add("@Column8", SqlDbType.NVarChar, 255).Value = If(row.Column8, String.Empty)
                    command.Parameters.Add("@RawLine", SqlDbType.NVarChar, 2000).Value = If(row.RawLine, String.Empty)
                    command.Parameters.Add("@SyncedAtUtc", SqlDbType.DateTime2).Value = syncedAtUtc
                    command.ExecuteNonQuery()
                End Using
            Next
        End Sub
    End Class
End Namespace
