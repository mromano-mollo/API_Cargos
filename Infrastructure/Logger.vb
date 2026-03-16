Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Threading

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

    Public NotInheritable Class CompositeLogger
        Implements ILogger

        Private ReadOnly _loggers As IReadOnlyList(Of ILogger)

        Public Sub New(ParamArray loggers As ILogger())
            Dim items As New List(Of ILogger)()

            If loggers IsNot Nothing Then
                For Each logger As ILogger In loggers
                    If logger IsNot Nothing Then
                        items.Add(logger)
                    End If
                Next
            End If

            _loggers = items
        End Sub

        Public Sub Info(message As String) Implements ILogger.Info
            For Each logger As ILogger In _loggers
                Try
                    logger.Info(message)
                Catch ex As Exception
                    ReportSinkFailure("INFO", ex)
                End Try
            Next
        End Sub

        Public Sub Warn(message As String) Implements ILogger.Warn
            For Each logger As ILogger In _loggers
                Try
                    logger.Warn(message)
                Catch ex As Exception
                    ReportSinkFailure("WARN", ex)
                End Try
            Next
        End Sub

        Public Sub [Error](message As String, Optional ex As Exception = Nothing) Implements ILogger.Error
            For Each logger As ILogger In _loggers
                Try
                    logger.Error(message, ex)
                Catch sinkEx As Exception
                    ReportSinkFailure("ERROR", sinkEx)
                End Try
            Next
        End Sub

        Private Shared Sub ReportSinkFailure(level As String, ex As Exception)
            Console.Error.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss} [LOGGER_FAIL] level={1} sink error: {2}", DateTime.Now, level, ex.Message))
        End Sub
    End Class

    Public NotInheritable Class SqlLogger
        Implements ILogger

        Private Const InsertSql As String =
            "INSERT INTO dbo.Cargos_Log " &
            "(CreatedAt, [Level], [Message], ExceptionType, ExceptionMessage, ExceptionStackTrace, MachineName, ProcessId, ThreadId) " &
            "VALUES (@CreatedAt, @Level, @Message, @ExceptionType, @ExceptionMessage, @ExceptionStackTrace, @MachineName, @ProcessId, @ThreadId);"

        Private ReadOnly _connectionString As String
        Private ReadOnly _commandTimeoutSeconds As Integer
        Private ReadOnly _machineName As String
        Private ReadOnly _processId As Integer
        Private _writeFailureReported As Integer

        Public Sub New(connectionString As String, commandTimeoutSeconds As Integer)
            _connectionString = If(connectionString, String.Empty).Trim()
            _commandTimeoutSeconds = Math.Max(1, commandTimeoutSeconds)
            _machineName = Environment.MachineName
            _processId = Process.GetCurrentProcess().Id
        End Sub

        Public Sub Info(message As String) Implements ILogger.Info
            Return
        End Sub

        Public Sub Warn(message As String) Implements ILogger.Warn
            Write("WARN", message, Nothing)
        End Sub

        Public Sub [Error](message As String, Optional ex As Exception = Nothing) Implements ILogger.Error
            Write("ERROR", message, ex)
        End Sub

        Private Sub Write(level As String, message As String, ex As Exception)
            If String.IsNullOrWhiteSpace(_connectionString) Then
                Return
            End If

            Try
                Using connection As New SqlConnection(_connectionString)
                    connection.Open()

                    Using command As New SqlCommand(InsertSql, connection)
                        command.CommandType = CommandType.Text
                        command.CommandTimeout = _commandTimeoutSeconds

                        AddNVarChar(command, "@Level", 20, level)
                        AddNVarCharMax(command, "@Message", If(message, String.Empty))
                        AddNVarChar(command, "@ExceptionType", 500, If(ex?.GetType().FullName, Nothing))
                        AddNVarCharMax(command, "@ExceptionMessage", If(ex?.Message, Nothing))
                        AddNVarCharMax(command, "@ExceptionStackTrace", If(ex?.ToString(), Nothing))
                        AddNVarChar(command, "@MachineName", 100, _machineName)
                        command.Parameters.Add("@ProcessId", SqlDbType.Int).Value = _processId
                        command.Parameters.Add("@ThreadId", SqlDbType.Int).Value = Thread.CurrentThread.ManagedThreadId
                        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.Now

                        command.ExecuteNonQuery()
                    End Using
                End Using

                Interlocked.Exchange(_writeFailureReported, 0)
            Catch logEx As Exception
                If Interlocked.Exchange(_writeFailureReported, 1) = 0 Then
                    Console.Error.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss} [LOGGER_FAIL] SQL logging disabled: {1}", DateTime.Now, logEx.Message))
                End If
            End Try
        End Sub

        Private Shared Sub AddNVarChar(command As SqlCommand, parameterName As String, size As Integer, value As String)
            Dim parameter As SqlParameter = command.Parameters.Add(parameterName, SqlDbType.NVarChar, size)
            If value Is Nothing Then
                parameter.Value = DBNull.Value
            Else
                parameter.Value = value
            End If
        End Sub

        Private Shared Sub AddNVarCharMax(command As SqlCommand, parameterName As String, value As String)
            Dim parameter As SqlParameter = command.Parameters.Add(parameterName, SqlDbType.NVarChar, -1)
            If value Is Nothing Then
                parameter.Value = DBNull.Value
            Else
                parameter.Value = value
            End If
        End Sub
    End Class
End Namespace
