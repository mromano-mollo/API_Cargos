Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Http
Imports System.Text
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
                Dim attemptedAtUtc As DateTime = DateTime.UtcNow
                Try
                    _logger.Info(String.Format("Syncing CaRGOS table {0} ({1}).", definition.TableName, definition.TableId))
                    Dim rows = DownloadTableRows(definition)
                    repository.ReplaceTable(definition, rows, attemptedAtUtc)
                    _logger.Info(String.Format("Synced CaRGOS table {0}: {1} rows.", definition.TableName, rows.Count))
                Catch ex As Exception
                    repository.MarkSyncFailure(definition, ex.Message, attemptedAtUtc)
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

                    Return ParseRows(responseBody)
                End Using
            End Using
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
            Return first.Contains("COD") OrElse first.Contains("ID") OrElse second.Contains("DESC") OrElse second.Contains("DENOM") OrElse second.Contains("NOME")
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
                New CargosReferenceTableDefinition With {.TableId = 2, .TableName = "LUOGHI"},
                New CargosReferenceTableDefinition With {.TableId = 9, .TableName = "TIPO_VEICOLO"},
                New CargosReferenceTableDefinition With {.TableId = 10, .TableName = "TIPO_DOCUMENTO"},
                New CargosReferenceTableDefinition With {.TableId = 11, .TableName = "TIPO_PAGAMENTO"}
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
    End Class
End Namespace
