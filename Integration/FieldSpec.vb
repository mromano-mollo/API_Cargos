Imports API_Cargos.Contracts

Namespace Integration
    Public Enum FieldNormalizationMode
        Literal = 0
        Text = 1
        Identifier = 2
        Numeric = 3
    End Enum

    Public NotInheritable Class FieldSpec
        Public Property Name As String
        Public Property Length As Integer
        Public Property NormalizationMode As FieldNormalizationMode = FieldNormalizationMode.Identifier
        Public Property ValueProvider As Func(Of OutboxRecord, String)
    End Class
End Namespace
