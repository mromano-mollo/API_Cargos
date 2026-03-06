Namespace Persistence
    Public Interface IAgencySyncRepository
        Function Execute(syncProcedureName As String) As Integer
    End Interface
End Namespace
