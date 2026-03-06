Imports System

Namespace Infrastructure
    Public Interface ILogger
        Sub Info(message As String)
        Sub Warn(message As String)
        Sub [Error](message As String, Optional ex As Exception = Nothing)
    End Interface

    Public NotInheritable Class ConsoleLogger
        Implements ILogger

        Public Sub Info(message As String) Implements ILogger.Info
            Write("INFO", message)
        End Sub

        Public Sub Warn(message As String) Implements ILogger.Warn
            Write("WARN", message)
        End Sub

        Public Sub [Error](message As String, Optional ex As Exception = Nothing) Implements ILogger.Error
            Dim fullMessage As String = message
            If ex IsNot Nothing Then
                fullMessage &= " | " & ex.Message
            End If

            Write("ERROR", fullMessage)
        End Sub

        Private Sub Write(level As String, message As String)
            Console.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}", DateTime.Now, level, message))
        End Sub
    End Class
End Namespace
