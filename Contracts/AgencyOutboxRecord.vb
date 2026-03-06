Imports System

Namespace Contracts
    Public NotInheritable Class AgencyOutboxRecord
        Public Property Id As Long
        Public Property BranchId As String
        Public Property BranchEmail As String
        Public Property AgenziaId As String
        Public Property AgenziaNome As String
        Public Property AgenziaLuogoValue As String
        Public Property AgenziaCity As String
        Public Property AgenziaCounty As String
        Public Property AgenziaPostCode As String
        Public Property AgenziaLuogoCod As String
        Public Property AgenziaIndirizzo As String
        Public Property AgenziaRecapitoTel As String
        Public Property Reason As String
        Public Property SnapshotHash As String
        Public Property Status As String
        Public Property LastError As String
        Public Property AttemptCount As Integer
        Public Property LastAttemptAt As Nullable(Of DateTime)
        Public Property NextRetryAt As Nullable(Of DateTime)
        Public Property CreatedAt As DateTime
    End Class
End Namespace
