Imports System
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Namespace Integration
    Public NotInheritable Class CryptoService
        Public Function EncryptAccessToken(accessToken As String, apiKey As String) As String
            If String.IsNullOrWhiteSpace(accessToken) Then
                Throw New ArgumentException("Access token is required.", NameOf(accessToken))
            End If

            If String.IsNullOrWhiteSpace(apiKey) OrElse apiKey.Length < 48 Then
                Throw New ArgumentException("ApiKey must contain at least 48 characters.", NameOf(apiKey))
            End If

            Dim keyBytes = Encoding.UTF8.GetBytes(apiKey.Substring(0, 32))
            Dim ivBytes = Encoding.UTF8.GetBytes(apiKey.Substring(32, 16))
            Dim plainBytes = Encoding.UTF8.GetBytes(accessToken)

            Using aes As Aes = Aes.Create()
                aes.Mode = CipherMode.CBC
                aes.Padding = PaddingMode.PKCS7
                aes.Key = keyBytes
                aes.IV = ivBytes

                Using encryptor As ICryptoTransform = aes.CreateEncryptor()
                    Using ms As New MemoryStream()
                        Using cs As New CryptoStream(ms, encryptor, CryptoStreamMode.Write)
                            cs.Write(plainBytes, 0, plainBytes.Length)
                            cs.FlushFinalBlock()
                        End Using

                        Return Convert.ToBase64String(ms.ToArray())
                    End Using
                End Using
            End Using
        End Function
    End Class
End Namespace
