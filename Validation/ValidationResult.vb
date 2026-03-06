Imports System.Collections.Generic
Imports System.Linq

Namespace Validation
    Public NotInheritable Class ValidationResult
        Public Sub New()
            MissingFields = New List(Of String)()
            Errors = New List(Of String)()
        End Sub

        Public ReadOnly Property MissingFields As List(Of String)
        Public ReadOnly Property Errors As List(Of String)

        Public ReadOnly Property IsValid As Boolean
            Get
                Return Not MissingFields.Any() AndAlso Not Errors.Any()
            End Get
        End Property

        Public Function ToSummary() As String
            Dim parts As New List(Of String)()
            If MissingFields.Any() Then
                parts.Add("Missing fields: " & String.Join(", ", MissingFields))
            End If

            If Errors.Any() Then
                parts.Add("Validation errors: " & String.Join(" | ", Errors))
            End If

            Return String.Join(" || ", parts)
        End Function

        Public Sub Merge(other As ValidationResult)
            If other Is Nothing Then
                Return
            End If

            For Each fieldName In other.MissingFields
                If Not MissingFields.Contains(fieldName) Then
                    MissingFields.Add(fieldName)
                End If
            Next

            For Each errorMessage In other.Errors
                If Not Errors.Contains(errorMessage) Then
                    Errors.Add(errorMessage)
                End If
            Next
        End Sub
    End Class
End Namespace
