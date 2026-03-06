Imports System.Net
Imports System.Net.Mail
Imports System.Security.Cryptography
Imports System.Text
Imports API_Cargos.Contracts
Imports API_Cargos.Infrastructure
Imports API_Cargos.Validation

Namespace Notifications
    Public Interface INotificationService
        Function TrySendMissingData(item As OutboxRecord, validation As ValidationResult) As String
        Function TrySendReject(item As OutboxRecord, rejectMessage As String) As String
    End Interface

    Public NotInheritable Class NotificationService
        Implements INotificationService

        Private ReadOnly _settings As AppSettings
        Private ReadOnly _logger As ILogger

        Public Sub New(settings As AppSettings, logger As ILogger)
            _settings = settings
            _logger = logger
        End Sub

        Public Function TrySendMissingData(item As OutboxRecord, validation As ValidationResult) As String Implements INotificationService.TrySendMissingData
            Dim detail As String = String.Join(",", validation.MissingFields.OrderBy(Function(x) x))
            Dim detailHash As String = ComputeHash(detail)

            If Not ShouldSend(item.BranchEmail, item.LastMissingEmailAt, item.LastMissingFieldsHash, detailHash, "missing-data", item) Then
                Return String.Empty
            End If

            Dim subject As String = String.Format("CARGOS - Missing mandatory data for contract {0}/{1}", item.ContractNo, item.LineNo)
            Dim body As String =
                "Contract: " & item.ContractNo & "/" & item.LineNo & Environment.NewLine &
                "Branch: " & item.BranchId & Environment.NewLine &
                "Missing CaRGOS fields: " & Environment.NewLine &
                String.Join(Environment.NewLine, validation.MissingFields.Select(Function(x) "- " & x))

            SendMail(item.BranchEmail, subject, body)
            Return detailHash
        End Function

        Public Function TrySendReject(item As OutboxRecord, rejectMessage As String) As String Implements INotificationService.TrySendReject
            Dim normalizedMessage As String = If(rejectMessage, String.Empty).Trim()
            Dim detailHash As String = ComputeHash(normalizedMessage)

            If Not ShouldSend(item.BranchEmail, item.LastRejectEmailAt, item.LastRejectHash, detailHash, "reject", item) Then
                Return String.Empty
            End If

            Dim subject As String = String.Format("CARGOS - Rejected contract {0}/{1}", item.ContractNo, item.LineNo)
            Dim body As String =
                "Contract: " & item.ContractNo & "/" & item.LineNo & Environment.NewLine &
                "Branch: " & item.BranchId & Environment.NewLine &
                "CaRGOS reject details: " & Environment.NewLine &
                normalizedMessage

            SendMail(item.BranchEmail, subject, body)
            Return detailHash
        End Function

        Private Function ShouldSend(recipient As String, lastSentAt As Nullable(Of DateTime), lastHash As String, currentHash As String, scenario As String, item As OutboxRecord) As Boolean
            If Not _settings.EmailEnabled Then
                _logger.Warn("Notifications disabled by configuration.")
                Return False
            End If

            If String.IsNullOrWhiteSpace(recipient) Then
                _logger.Warn(String.Format("Skipping {0} email for {1}/{2}: missing BranchEmail.", scenario, item.ContractNo, item.LineNo))
                Return False
            End If

            If lastSentAt.HasValue AndAlso String.Equals(lastHash, currentHash, StringComparison.OrdinalIgnoreCase) AndAlso lastSentAt.Value > DateTime.UtcNow.AddHours(-_settings.EmailCooldownHours) Then
                _logger.Info(String.Format("Skipping {0} email for {1}/{2}: anti-spam cooldown active.", scenario, item.ContractNo, item.LineNo))
                Return False
            End If

            Return True
        End Function

        Private Sub SendMail(recipient As String, subject As String, body As String)
            Using message As New MailMessage(_settings.EmailFrom, recipient, subject, body)
                message.IsBodyHtml = False

                Using client As New SmtpClient(_settings.EmailSmtpHost, _settings.EmailSmtpPort)
                    client.EnableSsl = _settings.EmailEnableSsl
                    If Not String.IsNullOrWhiteSpace(_settings.EmailUser) Then
                        client.Credentials = New NetworkCredential(_settings.EmailUser, _settings.EmailPassword)
                    End If

                    client.Send(message)
                End Using
            End Using
        End Sub

        Private Shared Function ComputeHash(value As String) As String
            Using sha As SHA256 = SHA256.Create()
                Dim bytes = Encoding.UTF8.GetBytes(If(value, String.Empty))
                Return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", String.Empty)
            End Using
        End Function
    End Class
End Namespace
