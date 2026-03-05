Imports System.Net.Http
Imports System.Text
Imports System.Web.Script.Serialization
Imports API_Cargos.Infrastructure

Namespace Integration
    Public NotInheritable Class CargosClient
        Implements ICargosClient

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

        Public Function Send(recordLines As IList(Of String)) As IList(Of CargosLineOutcome) Implements ICargosClient.Send
            If recordLines Is Nothing OrElse recordLines.Count = 0 Then
                Return New List(Of CargosLineOutcome)()
            End If

            Try
                Dim requestUrl As String = BuildUrl(_settings.CargosBaseUrl, _settings.CargosSendPath)
                Dim token As String = _tokenProvider.GetEncryptedToken()
                Dim serializer As New JavaScriptSerializer()
                Dim payload As String = serializer.Serialize(recordLines)

                Using request As New HttpRequestMessage(HttpMethod.Post, requestUrl)
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " & token)
                    request.Headers.TryAddWithoutValidation("Organization", _settings.CargosOrganization)
                    request.Content = New StringContent(payload, Encoding.UTF8, "application/json")

                    Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                        Dim responseBody As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

                        If Not response.IsSuccessStatusCode Then
                            If CInt(response.StatusCode) >= 400 AndAlso CInt(response.StatusCode) < 500 Then
                                Return CreateUniformOutcomes(recordLines.Count, CargosOutcomeType.DataError, String.Empty,
                                    String.Format("CaRGOS Send data error ({0}): {1}", CInt(response.StatusCode), Truncate(responseBody, 1000)))
                            End If

                            Return CreateUniformOutcomes(recordLines.Count, CargosOutcomeType.TechnicalError, String.Empty,
                                String.Format("CaRGOS Send technical error ({0}): {1}", CInt(response.StatusCode), Truncate(responseBody, 1000)))
                        End If

                        Return ParseSendResponse(recordLines.Count, responseBody)
                    End Using
                End Using
            Catch ex As Exception
                _logger.Error("CaRGOS send call failed with technical error.", ex)
                Return CreateUniformOutcomes(recordLines.Count, CargosOutcomeType.TechnicalError, String.Empty, ex.Message)
            End Try
        End Function

        Private Function ParseSendResponse(expectedCount As Integer, responseBody As String) As IList(Of CargosLineOutcome)
            Dim serializer As New JavaScriptSerializer()
            Dim parsed As Object

            Try
                parsed = serializer.DeserializeObject(responseBody)
            Catch ex As Exception
                Return CreateUniformOutcomes(expectedCount, CargosOutcomeType.TechnicalError, String.Empty,
                    "Unable to parse CaRGOS Send response JSON.")
            End Try

            Dim items As List(Of Object) = ExtractResultItems(parsed)
            If expectedCount > 1 AndAlso items.Count <> expectedCount Then
                Return CreateUniformOutcomes(expectedCount, CargosOutcomeType.TechnicalError, String.Empty,
                    "CaRGOS Send response count does not match request count.")
            End If

            If expectedCount = 1 AndAlso items.Count = 0 Then
                items.Add(parsed)
            End If

            Dim outcomes As New List(Of CargosLineOutcome)()
            For i As Integer = 0 To expectedCount - 1
                If i < items.Count Then
                    outcomes.Add(ParseItemOutcome(items(i)))
                Else
                    outcomes.Add(New CargosLineOutcome With {
                        .OutcomeType = CargosOutcomeType.TechnicalError,
                        .ErrorMessage = "Missing response item for request line."
                    })
                End If
            Next

            Return outcomes
        End Function

        Private Function ParseItemOutcome(item As Object) As CargosLineOutcome
            Dim dict As IDictionary(Of String, Object) = TryCast(item, IDictionary(Of String, Object))
            If dict Is Nothing Then
                Return New CargosLineOutcome With {
                    .OutcomeType = CargosOutcomeType.DataError,
                    .ErrorMessage = Convert.ToString(item)
                }
            End If

            Dim transactionId As String = ReadString(dict, "transactionid", "transactionId", "transaction_id", "idTransazione")
            Dim errorMessage As String = ReadString(dict, "error", "message", "messaggio", "descrizione", "detail", "dettaglio")
            Dim hasSuccessFlag As Boolean = TryReadBool(dict, "success", "ok", "esito", "isValid", "valid", "valido")

            If hasSuccessFlag OrElse Not String.IsNullOrWhiteSpace(transactionId) Then
                Return New CargosLineOutcome With {
                    .OutcomeType = CargosOutcomeType.Success,
                    .TransactionId = transactionId
                }
            End If

            Return New CargosLineOutcome With {
                .OutcomeType = CargosOutcomeType.DataError,
                .ErrorMessage = If(String.IsNullOrWhiteSpace(errorMessage), "CaRGOS rejected payload line.", errorMessage)
            }
        End Function

        Private Shared Function ExtractResultItems(root As Object) As List(Of Object)
            Dim result As New List(Of Object)()
            If root Is Nothing Then
                Return result
            End If

            Dim rootArray As Object() = TryCast(root, Object())
            If rootArray IsNot Nothing Then
                result.AddRange(rootArray)
                Return result
            End If

            Dim rootList As ArrayList = TryCast(root, ArrayList)
            If rootList IsNot Nothing Then
                For Each item As Object In rootList
                    result.Add(item)
                Next

                Return result
            End If

            Dim rootDict As IDictionary(Of String, Object) = TryCast(root, IDictionary(Of String, Object))
            If rootDict Is Nothing Then
                Return result
            End If

            Dim knownCollectionKeys As String() = {"result", "results", "data", "items", "esiti", "output"}
            For Each wantedKey In knownCollectionKeys
                For Each kvp In rootDict
                    If String.Equals(kvp.Key, wantedKey, StringComparison.OrdinalIgnoreCase) Then
                        Dim arrayValue As Object() = TryCast(kvp.Value, Object())
                        If arrayValue IsNot Nothing Then
                            result.AddRange(arrayValue)
                            Return result
                        End If

                        Dim listValue As ArrayList = TryCast(kvp.Value, ArrayList)
                        If listValue IsNot Nothing Then
                            For Each item As Object In listValue
                                result.Add(item)
                            Next

                            Return result
                        End If
                    End If
                Next
            Next

            Return result
        End Function

        Private Shared Function TryReadBool(root As IDictionary(Of String, Object), ParamArray keys As String()) As Boolean
            For Each key In keys
                For Each kvp In root
                    If String.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) Then
                        If kvp.Value Is Nothing Then
                            Return False
                        End If

                        Dim boolValue As Boolean
                        If Boolean.TryParse(Convert.ToString(kvp.Value), boolValue) Then
                            Return boolValue
                        End If

                        Dim intValue As Integer
                        If Integer.TryParse(Convert.ToString(kvp.Value), intValue) Then
                            Return intValue <> 0
                        End If
                    End If
                Next
            Next

            Return False
        End Function

        Private Shared Function ReadString(root As IDictionary(Of String, Object), ParamArray keys As String()) As String
            For Each key In keys
                For Each kvp In root
                    If String.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) Then
                        If kvp.Value Is Nothing Then
                            Return String.Empty
                        End If

                        Return Convert.ToString(kvp.Value)
                    End If
                Next
            Next

            Return String.Empty
        End Function

        Private Shared Function CreateUniformOutcomes(count As Integer, outcomeType As CargosOutcomeType, transactionId As String, errorMessage As String) As IList(Of CargosLineOutcome)
            Dim outcomes As New List(Of CargosLineOutcome)()
            For i As Integer = 1 To count
                outcomes.Add(New CargosLineOutcome With {
                    .OutcomeType = outcomeType,
                    .TransactionId = transactionId,
                    .ErrorMessage = errorMessage
                })
            Next

            Return outcomes
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
