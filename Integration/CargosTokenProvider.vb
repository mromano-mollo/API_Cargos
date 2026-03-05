Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Web.Script.Serialization
Imports API_Cargos.Infrastructure

Namespace Integration
    Public Interface ICargosTokenProvider
        Function GetEncryptedToken() As String
    End Interface

    Public NotInheritable Class CargosTokenProvider
        Implements ICargosTokenProvider

        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger
        Private ReadOnly _httpClient As HttpClient
        Private ReadOnly _cryptoService As CryptoService
        Private ReadOnly _syncRoot As New Object()

        Private _accessToken As String
        Private _expiresAtUtc As DateTime = DateTime.MinValue

        Public Sub New(settings As AppSettings, logger As ILogger, httpClient As HttpClient, cryptoService As CryptoService)
            _settings = settings
            _logger = logger
            _httpClient = httpClient
            _cryptoService = cryptoService
        End Sub

        Public Function GetEncryptedToken() As String Implements ICargosTokenProvider.GetEncryptedToken
            SyncLock _syncRoot
                If String.IsNullOrWhiteSpace(_accessToken) OrElse _expiresAtUtc <= DateTime.UtcNow.AddMinutes(2) Then
                    RefreshToken()
                End If

                Return _cryptoService.EncryptAccessToken(_accessToken, _settings.CargosApiKey)
            End SyncLock
        End Function

        Private Sub RefreshToken()
            Dim requestUrl As String = BuildUrl(_settings.CargosBaseUrl, _settings.CargosTokenPath)
            Dim credentialsRaw As String = _settings.CargosUsername & ":" & _settings.CargosPassword
            Dim credentialsEncoded As String = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentialsRaw))

            Using request As New HttpRequestMessage(HttpMethod.[Get], requestUrl)
                request.Headers.Authorization = New AuthenticationHeaderValue("Basic", credentialsEncoded)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim responseBody As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(
                            String.Format(
                                "Token request failed ({0}): {1}",
                                CInt(response.StatusCode),
                                Truncate(responseBody, 400)
                            )
                        )
                    End If

                    Dim serializer As New JavaScriptSerializer()
                    Dim parsed As Object = serializer.DeserializeObject(responseBody)
                    Dim root As IDictionary(Of String, Object) = TryCast(parsed, IDictionary(Of String, Object))
                    If root Is Nothing Then
                        Throw New InvalidOperationException("Token response JSON is not an object.")
                    End If

                    Dim token As String = ReadString(root, "access_token", "accessToken", "token")
                    If String.IsNullOrWhiteSpace(token) Then
                        Throw New InvalidOperationException("Token response does not contain access_token.")
                    End If

                    _accessToken = token
                    _expiresAtUtc = ResolveExpiry(root)

                    _logger.Info("CaRGOS token refreshed.")
                End Using
            End Using
        End Sub

        Private Function ResolveExpiry(root As IDictionary(Of String, Object)) As DateTime
            Dim expiresInSeconds As Integer = ReadInt(root, "expires_in", "expiresIn", "expires")
            If expiresInSeconds > 0 Then
                Return DateTime.UtcNow.AddSeconds(expiresInSeconds)
            End If

            Dim expiryRaw As String = ReadString(root, "expiration", "expires_at", "expiresAt")
            If Not String.IsNullOrWhiteSpace(expiryRaw) Then
                Dim parsed As DateTime
                If DateTime.TryParse(expiryRaw, parsed) Then
                    Return parsed.ToUniversalTime()
                End If
            End If

            Return DateTime.UtcNow.AddMinutes(30)
        End Function

        Private Shared Function BuildUrl(baseUrl As String, relativePath As String) As String
            Dim safeBase As String = If(baseUrl, String.Empty).Trim().TrimEnd("/"c)
            Dim safePath As String = If(relativePath, String.Empty).Trim()
            If Not safePath.StartsWith("/", StringComparison.Ordinal) Then
                safePath = "/" & safePath
            End If

            Return safeBase & safePath
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

        Private Shared Function ReadInt(root As IDictionary(Of String, Object), ParamArray keys As String()) As Integer
            For Each key In keys
                Dim rawValue As String = ReadString(root, key)
                Dim parsed As Integer
                If Integer.TryParse(rawValue, parsed) Then
                    Return parsed
                End If
            Next

            Return 0
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
