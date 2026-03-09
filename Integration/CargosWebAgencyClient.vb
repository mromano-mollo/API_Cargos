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
        Private _handler As HttpClientHandler
        Private _httpClient As HttpClient
        Private _isAuthenticated As Boolean

        Public Sub New(settings As AppSettings, logger As ILogger)
            _settings = settings
            _logger = logger
            RecreateHttpClient()
        End Sub

        Private Sub RecreateHttpClient()
            If _httpClient IsNot Nothing Then
                _httpClient.Dispose()
            End If

            If _handler IsNot Nothing Then
                _handler.Dispose()
            End If

            _handler = New HttpClientHandler() With {
                .AllowAutoRedirect = True,
                .CookieContainer = New CookieContainer()
            }
            _httpClient = New HttpClient(_handler) With {
                .Timeout = TimeSpan.FromSeconds(_settings.CargosHttpTimeoutSeconds)
            }
        End Sub

        Public Function CreateAgency(item As AgencyOutboxRecord) As CargosLineOutcome Implements IAgencyCreateClient.CreateAgency
            Return CreateAgency(item, True)
        End Function

        Private Function CreateAgency(item As AgencyOutboxRecord, allowReauthenticate As Boolean) As CargosLineOutcome
            Try
                EnsureAuthenticated()

                Dim createUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebAgencyCreatePath)
                Dim createPageUri As Uri = Nothing
                Dim createPage As String = GetHtml(createUrl, createPageUri, True)
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
                        Dim finalUri As Uri = If(response.RequestMessage IsNot Nothing, response.RequestMessage.RequestUri, Nothing)
                        If Not response.IsSuccessStatusCode Then
                            Return New CargosLineOutcome With {
                                .OutcomeType = ClassifyHttpFailure(response.StatusCode),
                                .ErrorMessage = String.Format("Agency create failed ({0}): {1}", CInt(response.StatusCode), Truncate(responseBody, 1000))
                            }
                        End If

                        If IsLoginResponse(responseBody, finalUri) Then
                            Throw New InvalidOperationException(String.Format("CARGOS_WEB returned the login page instead of the agency create result. FinalUri={0}", If(finalUri Is Nothing, String.Empty, finalUri.AbsoluteUri)))
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
                If allowReauthenticate AndAlso IsAuthenticationFailure(ex) Then
                    _logger.Info("CARGOS_WEB session expired. Re-authenticating and retrying the agency create once.")
                    ResetAuthenticationState()
                    Return CreateAgency(item, False)
                End If

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

            Dim loginPageUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebLoginPagePath)
            Dim loginUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebLoginPath)
            Dim loginPage As String = GetHtml(loginPageUrl, Nothing, False)
            Dim formData As Dictionary(Of String, String) = ExtractFormFields(loginPage)
            UpsertFormField(formData, _settings.CargosWebVerifyTokenField, ExtractVerificationToken(loginPage, _settings.CargosWebVerifyTokenField))
            UpsertFormField(formData, _settings.CargosWebLoginUsernameField, _settings.CargosWebUsername)
            UpsertFormField(formData, _settings.CargosWebLoginPasswordField, _settings.CargosWebPassword)
            UpsertFormField(formData, _settings.CargosWebLoginAccediField, "Accedi")

            Dim loginResponseUri As Uri = Nothing
            Dim loginResponseBody As String = PostForm(loginUrl, formData, loginPageUrl, loginResponseUri)

            If RequiresOtpChallenge(loginResponseBody) Then
                Dim otpCode As String = PromptForOtp()
                Dim otpUrl As String = BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebLoginOtpPath)
                Dim otpFormData As Dictionary(Of String, String) = ExtractFormFields(loginResponseBody)
                UpsertFormField(otpFormData, _settings.CargosWebVerifyTokenField, ExtractVerificationToken(loginResponseBody, _settings.CargosWebVerifyTokenField))
                UpsertFormField(otpFormData, _settings.CargosWebLoginUsernameField, _settings.CargosWebUsername)
                UpsertFormField(otpFormData, _settings.CargosWebLoginPasswordField, _settings.CargosWebPassword)
                UpsertFormField(otpFormData, _settings.CargosWebOtpCodeField, otpCode)
                UpsertFormField(otpFormData, _settings.CargosWebLoginAccediField, "Accedi")

                Dim otpResponseUri As Uri = Nothing
                Dim otpResponseBody As String = PostForm(otpUrl, otpFormData, otpUrl, otpResponseUri)
                If IsLoginResponse(otpResponseBody, otpResponseUri) Then
                    If ContainsValidationErrors(otpResponseBody) Then
                        Throw New InvalidOperationException("CARGOS_WEB OTP validation failed.")
                    End If

                    Throw New InvalidOperationException(String.Format("CARGOS_WEB OTP step did not create an authenticated session. FinalUri={0}", If(otpResponseUri Is Nothing, String.Empty, otpResponseUri.AbsoluteUri)))
                End If
            ElseIf IsLoginResponse(loginResponseBody, loginResponseUri) Then
                If ContainsValidationErrors(loginResponseBody) Then
                    Throw New InvalidOperationException("CARGOS_WEB login returned validation errors.")
                End If

                Throw New InvalidOperationException(String.Format("CARGOS_WEB credentials step did not reach OTP or an authenticated session. FinalUri={0}", If(loginResponseUri Is Nothing, String.Empty, loginResponseUri.AbsoluteUri)))
            End If

            Dim protectedPageUri As Uri = Nothing
            GetHtml(BuildUrl(_settings.CargosWebBaseUrl, _settings.CargosWebAgencyCreatePath), protectedPageUri, True)

            _isAuthenticated = True
        End Sub

        Private Function GetHtml(url As String, ByRef finalUri As Uri, expectAuthenticated As Boolean) As String
            Using request As New HttpRequestMessage(HttpMethod.Get, url)
                ApplyCookieHeader(request)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim html As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    finalUri = If(response.RequestMessage IsNot Nothing, response.RequestMessage.RequestUri, Nothing)
                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(String.Format("CARGOS_WEB GET failed ({0}): {1}", CInt(response.StatusCode), Truncate(html, 1000)))
                    End If

                    If expectAuthenticated AndAlso IsLoginResponse(html, finalUri) Then
                        Throw New InvalidOperationException(String.Format("CARGOS_WEB session is not authenticated. FinalUri={0}", If(finalUri Is Nothing, String.Empty, finalUri.AbsoluteUri)))
                    End If

                    Return html
                End Using
            End Using
        End Function

        Private Function PostForm(url As String, formData As IDictionary(Of String, String), referrerUrl As String, ByRef finalUri As Uri) As String
            Using request As New HttpRequestMessage(HttpMethod.Post, url)
                ApplyCookieHeader(request)
                request.Content = New FormUrlEncodedContent(formData)
                request.Headers.Referrer = New Uri(referrerUrl)

                Using response As HttpResponseMessage = _httpClient.SendAsync(request).GetAwaiter().GetResult()
                    Dim responseBody As String = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    finalUri = If(response.RequestMessage IsNot Nothing, response.RequestMessage.RequestUri, Nothing)

                    If Not response.IsSuccessStatusCode Then
                        Throw New InvalidOperationException(String.Format("CARGOS_WEB POST failed ({0}) on {1}: {2}", CInt(response.StatusCode), url, Truncate(responseBody, 1000)))
                    End If

                    Return responseBody
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

        Private Function RequiresOtpChallenge(html As String) As Boolean
            If String.IsNullOrWhiteSpace(html) Then
                Return False
            End If

            Return Regex.IsMatch(html, String.Format("name=""{0}""", Regex.Escape(_settings.CargosWebOtpCodeField)), RegexOptions.IgnoreCase)
        End Function

        Private Function IsLoginResponse(html As String, requestUri As Uri) As Boolean
            If requestUri IsNot Nothing AndAlso requestUri.AbsolutePath.IndexOf("/Login", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return True
            End If

            If String.IsNullOrWhiteSpace(html) Then
                Return False
            End If

            Dim usernameField As String = Regex.Escape(If(_settings.CargosWebLoginUsernameField, String.Empty))
            Dim passwordField As String = Regex.Escape(If(_settings.CargosWebLoginPasswordField, String.Empty))

            If Not String.IsNullOrWhiteSpace(usernameField) AndAlso
               Regex.IsMatch(html, String.Format("name=""{0}""", usernameField), RegexOptions.IgnoreCase) AndAlso
               Regex.IsMatch(html, String.Format("name=""{0}""", passwordField), RegexOptions.IgnoreCase) Then
                Return True
            End If

            Return html.IndexOf("type=""password""", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
                   html.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Private Shared Function IsAuthenticationFailure(ex As Exception) As Boolean
            If ex Is Nothing OrElse String.IsNullOrWhiteSpace(ex.Message) Then
                Return False
            End If

            Return ex.Message.IndexOf("session is not authenticated", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   ex.Message.IndexOf("returned the login page", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   ex.Message.IndexOf("did not create an authenticated session", StringComparison.OrdinalIgnoreCase) >= 0
        End Function

        Private Sub ResetAuthenticationState()
            _isAuthenticated = False

            If String.IsNullOrWhiteSpace(_settings.CargosWebAuthCookieHeader) Then
                RecreateHttpClient()
            End If
        End Sub

        Private Shared Function ExtractFormFields(html As String) As Dictionary(Of String, String)
            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrWhiteSpace(html) Then
                Return result
            End If

            For Each tagMatch As Match In Regex.Matches(html, "<input\b[^>]*>", RegexOptions.IgnoreCase)
                Dim tag As String = tagMatch.Value
                Dim name As String = ExtractAttribute(tag, "name")
                If String.IsNullOrWhiteSpace(name) Then
                    Continue For
                End If

                Dim value As String = ExtractAttribute(tag, "value")
                result(name) = value
            Next

            Return result
        End Function

        Private Shared Function ExtractAttribute(tag As String, attributeName As String) As String
            If String.IsNullOrWhiteSpace(tag) OrElse String.IsNullOrWhiteSpace(attributeName) Then
                Return String.Empty
            End If

            Dim match As Match = Regex.Match(tag, String.Format("{0}=""([^""]*)""", Regex.Escape(attributeName)), RegexOptions.IgnoreCase)
            If match.Success Then
                Return WebUtility.HtmlDecode(match.Groups(1).Value)
            End If

            Return String.Empty
        End Function

        Private Shared Sub UpsertFormField(formData As IDictionary(Of String, String), fieldName As String, fieldValue As String)
            If formData Is Nothing OrElse String.IsNullOrWhiteSpace(fieldName) Then
                Return
            End If

            formData(fieldName) = If(fieldValue, String.Empty)
        End Sub

        Private Function PromptForOtp() As String
            Console.Write("Insert CARGOS_WEB OTP code: ")
            Dim otpCode As String = Console.ReadLine()
            If String.IsNullOrWhiteSpace(otpCode) Then
                Throw New InvalidOperationException("OTP code is required to complete CARGOS_WEB login.")
            End If

            Return otpCode.Trim()
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
