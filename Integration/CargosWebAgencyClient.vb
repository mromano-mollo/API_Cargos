Imports System.Collections.Generic
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports API_Cargos.Contracts
Imports API_Cargos.Infrastructure

Namespace Integration
    Public NotInheritable Class CargosWebAgencyClient
        Implements IAgencyCreateClient

        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger
        Private ReadOnly _handler As HttpClientHandler
        Private ReadOnly _httpClient As HttpClient
        Private _isAuthenticated As Boolean

        Public Sub New(settings As AppSettings, logger As ILogger)
            _settings = settings
            _logger = logger
            _handler = New HttpClientHandler() With {
                .AllowAutoRedirect = True,
                .CookieContainer = New CookieContainer()
            }
            _httpClient = New HttpClient(_handler) With {
                .Timeout = TimeSpan.FromSeconds(settings.CargosHttpTimeoutSeconds)
            }
        End Sub

        Public Function CreateAgency(item As AgencyOutboxRecord) As CargosLineOutcome Implements IAgencyCreateClient.CreateAgency
            Try
                EnsureAuthenticated()

                Dim createUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebAgencyCreatePath)
                Dim createPage As String = GetHtml(createUrl)
                Dim verificationToken As String = ExtractVerificationToken(createPage, _settings.CargosWebVerifyTokenField)
                If String.IsNullOrWhiteSpace(verificationToken) Then
                    Return New CargosLineOutcome With {
                        .OutcomeType = CargosOutcomeType.TechnicalError,
                        .ErrorMessage = "Unable to extract __RequestVerificationToken from agency create page."
                    }
                End If

                Dim formData As New List(Of KeyValuePair(Of String, String)) From
                {
                    New KeyValuePair(Of String, String)(_settings.CargosWebVerifyTokenField, verificationToken),
                    New KeyValuePair(Of String, String)("Agenzia.AGENZIA_ID", If(item.AgenziaId, String.Empty)),
                    New KeyValuePair(Of String, String)("Agenzia.AGENZIA_NOME", If(item.AgenziaNome, String.Empty)),
                    New KeyValuePair(Of String, String)("Agenzia.AGENZIA_LUOGO_COD", If(item.AgenziaLuogoCod, String.Empty)),
                    New KeyValuePair(Of String, String)("Agenzia.AGENZIA_INDIRIZZO", If(item.AgenziaIndirizzo, String.Empty)),
                    New KeyValuePair(Of String, String)("Agenzia.AGENZIA_RECAPITO_TEL", If(item.AgenziaRecapitoTel, String.Empty))
                }

                Using request As New HttpRequestMessage(HttpMethod.Post, createUrl)
                    ApplyCookieHeader(request)
                    request.Content = New FormUrlEncodedContent(formData)
                    request.Headers.Referrer = New Uri(createUrl)

                    Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                        Dim responseBody As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                        If Not response.IsSuccessStatusCode Then
                            Return New CargosLineOutcome With {
                                .OutcomeType = ClassifyHttpFailure(response.StatusCode),
                                .ErrorMessage = String.Format("Agency create failed ({0}): {1}", CInt(response.StatusCode), Truncate(responseBody, 1000))
                            }
                        End If

                        If ContainsValidationErrors(responseBody) Then
                            Return New CargosLineOutcome With {
                                .OutcomeType = CargosOutcomeType.DataError,
                                .ErrorMessage = Truncate(StripHtml(responseBody), 1000)
                            }
                        End If

                        Return New CargosLineOutcome With {
                            .OutcomeType = CargosOutcomeType.Success
                        }
                    End Using
                End Using
            Catch ex As Exception
                _logger.Error("CARGOS_WEB agency create call failed.", ex)
                Return New CargosLineOutcome With {
                    .OutcomeType = CargosOutcomeType.TechnicalError,
                    .ErrorMessage = ex.Message
                }
            End Try
        End Function

        Private Sub EnsureAuthenticated()
            If _isAuthenticated Then
                Return
            End If

            If Not String.IsNullOrWhiteSpace(_settings.CargosWebAuthCookieHeader) Then
                _isAuthenticated = True
                Return
            End If

            If String.IsNullOrWhiteSpace(_settings.CargosWebUsername) OrElse String.IsNullOrWhiteSpace(_settings.CargosWebPassword) Then
                Throw New InvalidOperationException("CargosWeb authentication requires either AuthCookieHeader or Username/Password.")
            End If

            Dim loginUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebLoginPath)
            Dim loginPage As String = GetHtml(loginUrl)
            Dim verificationToken As String = ExtractVerificationToken(loginPage, _settings.CargosWebVerifyTokenField)

            Dim formData As New List(Of KeyValuePair(Of String, String))()
            If Not String.IsNullOrWhiteSpace(verificationToken) Then
                formData.Add(New KeyValuePair(Of String, String)(_settings.CargosWebVerifyTokenField, verificationToken))
            End If

            formData.Add(New KeyValuePair(Of String, String)(_settings.CargosWebLoginUsernameField, _settings.CargosWebUsername))
            formData.Add(New KeyValuePair(Of String, String)(_settings.CargosWebLoginPasswordField, _settings.CargosWebPassword))

            Using request As New HttpRequestMessage(HttpMethod.Post, loginUrl)
                ApplyCookieHeader(request)
                request.Content = New FormUrlEncodedContent(formData)
                request.Headers.Referrer = New Uri(loginUrl)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim responseBody As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(String.Format("CARGOS_WEB login failed ({0}): {1}", CInt(response.StatusCode), Truncate(responseBody, 1000)))
                    End If

                    If response.RequestMessage IsNot Nothing AndAlso response.RequestMessage.RequestUri IsNot Nothing Then
                        If response.RequestMessage.RequestUri.AbsolutePath.IndexOf("/Login", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso ContainsValidationErrors(responseBody) Then
                            Throw New InvalidOperationException("CARGOS_WEB login returned validation errors.")
                        End If
                    End If
                End Using
            End Using

            _isAuthenticated = True
        End Sub

        Private Function GetHtml(url As String) As String
            Using request As New HttpRequestMessage(HttpMethod.Get, url)
                ApplyCookieHeader(request)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim html As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(String.Format("CARGOS_WEB GET failed ({0}): {1}", CInt(response.StatusCode), Truncate(html, 1000)))
                    End If

                    Return html
                End Using
            End Using
        End Function

        Private Sub ApplyCookieHeader(request As HttpRequestMessage)
            If Not String.IsNullOrWhiteSpace(_settings.CargosWebAuthCookieHeader) Then
                request.Headers.TryAddWithoutValidation("Cookie", _settings.CargosWebAuthCookieHeader)
            End If
        End Sub

        Private Shared Function ExtractVerificationToken(html As String, fieldName As String) As String
            If String.IsNullOrWhiteSpace(html) Then
                Return String.Empty
            End If

            Dim pattern As String = String.Format("<input[^>]*name=""{0}""[^>]*value=""([^""]+)""", Regex.Escape(fieldName))
            Dim match As Match = Regex.Match(html, pattern, RegexOptions.IgnoreCase)
            If match.Success Then
                Return match.Groups(1).Value
            End If

            Return String.Empty
        End Function

        Private Shared Function ContainsValidationErrors(html As String) As Boolean
            If String.IsNullOrWhiteSpace(html) Then
                Return False
            End If

            Return html.IndexOf("validation-summary-errors", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   html.IndexOf("field-validation-error", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   html.IndexOf("input-validation-error", StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Private Shared Function StripHtml(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then
                Return String.Empty
            End If

            Return Regex.Replace(value, "<[^>]+>", " ").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
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

        Private Shared Function ClassifyHttpFailure(statusCode As HttpStatusCode) As CargosOutcomeType
            If CInt(statusCode) >= 400 AndAlso CInt(statusCode) < 500 Then
                Return CargosOutcomeType.DataError
            End If

            Return CargosOutcomeType.TechnicalError
        End Function
    End Class
End Namespace
