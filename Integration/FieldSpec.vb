Imports API_Cargos.Contracts

Namespace Integration
    Public NotInheritable Class FieldSpec
        Public Property Name As String
        Public Property Length As Integer
        Public Property ValueProvider As Func(Of OutboxRecord, String)
    End Class
End Namespace
