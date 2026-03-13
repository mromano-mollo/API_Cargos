Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Http
Imports System.Text
Imports System.Web.Script.Serialization
Imports API_Cargos.Contracts
Imports API_Cargos.Infrastructure
Imports API_Cargos.Persistence

Namespace Integration
    Public Interface ICargosReferenceTableSyncService
        Sub SyncAll(repository As ICargosReferenceTableRepository)
    End Interface

    Public NotInheritable Class CargosReferenceTableSyncService
        Implements ICargosReferenceTableSyncService

        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger
        Private ReadOnly _httpClient As HttpClient
        Private ReadOnly _tokenProvider As ICargosTokenProvider

        Public Sub New(settings As AppSettings, logger As ILogger)
            _settings = settings
            _logger = logger
            _httpClient = New HttpClient() With {
                .Timeout = TimeSpan.FromSeconds(settings.CargosHttpTimeoutSeconds)
            }
            _tokenProvider = New CargosTokenProvider(settings, logger, _httpClient, New CryptoService())
        End Sub

        Public Sub SyncAll(repository As ICargosReferenceTableRepository) Implements ICargosReferenceTableSyncService.SyncAll
            If repository Is Nothing Then
                Throw New ArgumentNullException(NameOf(repository))
            End If

            Dim failures As New List(Of String)()
            For Each definition In GetDefinitions()
                Dim attemptedAtLocal As DateTime = DateTime.Now
                Try
                    _logger.Info(String.Format("Syncing CaRGOS table {0} ({1}).", definition.TableName, definition.TableId))
                    Dim rows = DownloadTableRows(definition)
                    repository.ReplaceTable(definition, rows, attemptedAtLocal)
                    _logger.Info(String.Format("Synced CaRGOS table {0}: {1} rows.", definition.TableName, rows.Count))
                Catch ex As Exception
                    repository.MarkSyncFailure(definition, ex.Message, attemptedAtLocal)
                    failures.Add(definition.TableName & ": " & ex.Message)
                End Try
            Next

            If failures.Any() Then
                Throw New InvalidOperationException("One or more CaRGOS table sync operations failed: " & String.Join(" || ", failures))
            End If
        End Sub

        Private Function DownloadTableRows(definition As CargosReferenceTableDefinition) As IList(Of CargosReferenceTableRow)
            Dim requestUrl As String = BuildUrl(_settings.CargosBaseUrl, _settings.CargosTabellaPath & "?TabellaIdentificativo=" & definition.TableId.ToString())
            Dim token As String = _tokenProvider.GetEncryptedToken()
            Using request As New HttpRequestMessage(HttpMethod.[Get], requestUrl)
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " & token)
                request.Headers.TryAddWithoutValidation("Organization", _settings.CargosOrganization)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim responseBytes As Byte() = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
                    Dim responseBody As String = Encoding.UTF8.GetString(responseBytes)

                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(String.Format("Tabella {0} sync failed ({1}): {2}", definition.TableName, CInt(response.StatusCode), Truncate(responseBody, 500)))
                    End If

                    Return ParseRowsFromApiResponse(responseBody, definition.TableName)
                End Using
            End Using
        End Function

        Friend Shared Function ParseRowsFromApiResponse(responseBody As String, Optional tableName As String = Nothing) As IList(Of CargosReferenceTableRow)
            Dim decodedContent As String = DecodeFileContentFromApiResponse(responseBody, tableName)
            Return ParseRows(decodedContent)
        End Function

        Private Shared Function DecodeFileContentFromApiResponse(responseBody As String, tableName As String) As String
            If String.IsNullOrWhiteSpace(responseBody) Then
                Throw New InvalidOperationException(BuildTableMessage(tableName, "api/Tabella returned an empty response body."))
            End If

            Dim serializer As New JavaScriptSerializer()
            Dim payloadObject As Object = serializer.DeserializeObject(responseBody)

            If TypeOf payloadObject Is String Then
                payloadObject = serializer.DeserializeObject(CStr(payloadObject))
            End If

            Dim payload = TryCast(payloadObject, IDictionary(Of String, Object))
            If payload Is Nothing Then
                Throw New InvalidOperationException(BuildTableMessage(tableName, "api/Tabella response is not a valid JSON object."))
            End If

            Dim esito As Boolean = False
            If payload.ContainsKey("esito") AndAlso payload("esito") IsNot Nothing Then
                esito = Convert.ToBoolean(payload("esito"))
            End If

            Dim errore As String = String.Empty
            If payload.ContainsKey("errore") AndAlso payload("errore") IsNot Nothing Then
                errore = Convert.ToString(payload("errore")).Trim()
            End If

            If Not esito Then
                Throw New InvalidOperationException(BuildTableMessage(tableName, "api/Tabella returned esito=false. " & errore))
            End If

            Dim fileBase64 As String = String.Empty
            If payload.ContainsKey("file") AndAlso payload("file") IsNot Nothing Then
                fileBase64 = Convert.ToString(payload("file")).Trim()
            End If

            If String.IsNullOrWhiteSpace(fileBase64) Then
                Throw New InvalidOperationException(BuildTableMessage(tableName, "api/Tabella response does not contain the base64 file payload."))
            End If

            Try
                Dim decodedBytes As Byte() = Convert.FromBase64String(fileBase64)
                Return Encoding.UTF8.GetString(decodedBytes)
            Catch ex As FormatException
                Throw New InvalidOperationException(BuildTableMessage(tableName, "api/Tabella returned an invalid base64 file payload."), ex)
            End Try
        End Function

        Private Shared Function ParseRows(csvContent As String) As IList(Of CargosReferenceTableRow)
            Dim rows As New List(Of CargosReferenceTableRow)()
            Dim lines = csvContent.Replace(ChrW(&HFEFF), String.Empty).Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim rowNumber As Integer = 0

            For Each rawLine In lines
                Dim trimmed As String = rawLine.Trim()
                If String.IsNullOrWhiteSpace(trimmed) Then
                    Continue For
                End If

                Dim columns = trimmed.Split("#"c)
                If IsHeader(columns) Then
                    Continue For
                End If

                rowNumber += 1
                rows.Add(New CargosReferenceTableRow() With {
                    .RowNumber = rowNumber,
                    .Code = GetColumn(columns, 0),
                    .Description = GetColumn(columns, 1),
                    .Column3 = GetColumn(columns, 2),
                    .Column4 = GetColumn(columns, 3),
                    .Column5 = GetColumn(columns, 4),
                    .Column6 = GetColumn(columns, 5),
                    .Column7 = GetColumn(columns, 6),
                    .Column8 = GetColumn(columns, 7),
                    .RawLine = Truncate(trimmed, 2000)
                })
            Next

            Return rows
        End Function

        Private Shared Function IsHeader(columns As String()) As Boolean
            If columns Is Nothing OrElse columns.Length = 0 Then
                Return False
            End If

            Dim first As String = GetColumn(columns, 0).ToUpperInvariant()
            Dim second As String = GetColumn(columns, 1).ToUpperInvariant()
            Dim firstIsHeader As Boolean = first = "ID" OrElse first = "CODICE"
            Dim secondIsHeader As Boolean = second.Contains("DESCR") OrElse second.Contains("DENOM") OrElse second = "NOME"
            Return firstIsHeader AndAlso secondIsHeader
        End Function

        Private Shared Function GetColumn(columns As String(), index As Integer) As String
            If columns Is Nothing OrElse index < 0 OrElse index >= columns.Length Then
                Return String.Empty
            End If

            Return columns(index).Trim()
        End Function

        Private Shared Function GetDefinitions() As IList(Of CargosReferenceTableDefinition)
            Return New List(Of CargosReferenceTableDefinition) From
            {
                New CargosReferenceTableDefinition With {.TableId = 0, .TableName = "TIPO_PAGAMENTO"},
                New CargosReferenceTableDefinition With {.TableId = 1, .TableName = "LUOGHI"},
                New CargosReferenceTableDefinition With {.TableId = 2, .TableName = "TIPO_VEICOLO"},
                New CargosReferenceTableDefinition With {.TableId = 3, .TableName = "TIPO_DOCUMENTO"}
            }
        End Function

        Private Shared Function BuildUrl(baseUrl As String, relativePath As String) As String
            Dim safeBase As String = If(baseUrl, String.Empty).Trim().TrimEnd("/"c)
            Dim safePath As String = If(relativePath, String.Empty).Trim()
            If Not safePath.StartsWith("/", StringComparison.Ordinal) Then
                safePath = "/" & safePath
            End If

            Return safeBase & safePath
        End Function

        Private Shared Function Truncate(value As String, maxLength As Integer) As String
            If String.IsNullOrEmpty(value) Then
                Return String.Empty
            End If

            If value.Length <= maxLength Then
                Return value
            End If

            Return value.Substring(0, maxLength)
        End Function

        Private Shared Function BuildTableMessage(tableName As String, message As String) As String
            If String.IsNullOrWhiteSpace(tableName) Then
                Return message
            End If

            Return String.Format("Table {0}: {1}", tableName, message)
        End Function
    End Class
End Namespace
