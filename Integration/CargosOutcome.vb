Namespace Integration
    Public Enum CargosOutcomeType
        Success
        DataError
        TechnicalError
    End Enum

    Public NotInheritable Class CargosLineOutcome
        Public Property OutcomeType As CargosOutcomeType
        Public Property TransactionId As String
        Public Property ErrorMessage As String
    End Class
End Namespace
