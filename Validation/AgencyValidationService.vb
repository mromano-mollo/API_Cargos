Imports API_Cargos.Contracts

Namespace Validation
    Public Interface IAgencyValidationService
        Function Validate(item As AgencyOutboxRecord) As ValidationResult
    End Interface

    Public NotInheritable Class AgencyValidationService
        Implements IAgencyValidationService

        Public Function Validate(item As AgencyOutboxRecord) As ValidationResult Implements IAgencyValidationService.Validate
            If item Is Nothing Then
                Throw New ArgumentNullException(NameOf(item))
            End If

            Dim result As New ValidationResult()

            RequireString(result, "Agenzia.AGENZIA_ID", item.AgenziaId)
            RequireString(result, "Agenzia.AGENZIA_NOME", item.AgenziaNome)
            RequireString(result, "Agenzia.AGENZIA_LUOGO_COD", item.AgenziaLuogoCod)
            RequireString(result, "Agenzia.AGENZIA_INDIRIZZO", item.AgenziaIndirizzo)
            RequireString(result, "Agenzia.AGENZIA_RECAPITO_TEL", item.AgenziaRecapitoTel)

            ValidateLength(result, "Agenzia.AGENZIA_ID", item.AgenziaId, 9)
            ValidateLength(result, "Agenzia.AGENZIA_NOME", item.AgenziaNome, 38)
            ValidateLength(result, "Agenzia.AGENZIA_LUOGO_COD", item.AgenziaLuogoCod, 9)
            ValidateLength(result, "Agenzia.AGENZIA_INDIRIZZO", item.AgenziaIndirizzo, 80)
            ValidateLength(result, "Agenzia.AGENZIA_RECAPITO_TEL", item.AgenziaRecapitoTel, 15)

            Return result
        End Function

        Private Shared Sub RequireString(result As ValidationResult, fieldName As String, value As String)
            If String.IsNullOrWhiteSpace(value) Then
                result.MissingFields.Add(fieldName)
            End If
        End Sub

        Private Shared Sub ValidateLength(result As ValidationResult, fieldName As String, value As String, maxLength As Integer)
            If String.IsNullOrWhiteSpace(value) Then
                Return
            End If

            If value.Trim().Length > maxLength Then
                result.Errors.Add(String.Format("{0} exceeds max length {1}.", fieldName, maxLength))
            End If
        End Sub
    End Class
End Namespace
