Imports System.Collections.Generic

Namespace Integration
    Public Interface ICargosClient
        Function Send(recordLines As IList(Of String)) As IList(Of CargosLineOutcome)
    End Interface
End Namespace
