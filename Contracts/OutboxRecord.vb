Imports System

Namespace Contracts
    Public NotInheritable Class OutboxRecord
        Public Property Id As Long
        Public Property ContractNo As String
        Public Property LineNo As Long
        Public Property CargosContractId As String
        Public Property BranchId As String
        Public Property Reason As String
        Public Property SnapshotHash As String
        Public Property RecordLine As String
        Public Property Status As String
        Public Property AttemptCount As Integer
        Public Property LastAttemptAt As Nullable(Of DateTime)
        Public Property NextRetryAt As Nullable(Of DateTime)
        Public Property CreatedAt As DateTime
    End Class
End Namespace
