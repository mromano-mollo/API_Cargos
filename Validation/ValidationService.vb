Imports System
Imports API_Cargos.Contracts

Namespace Validation
    Public Interface IValidationService
        Function Validate(item As OutboxRecord) As ValidationResult
    End Interface

    Public NotInheritable Class ValidationService
        Implements IValidationService

        Public Function Validate(item As OutboxRecord) As ValidationResult Implements IValidationService.Validate
            If item Is Nothing Then
                Throw New ArgumentNullException(NameOf(item))
            End If

            Dim result As New ValidationResult()

            RequireString(result, "CONTRATTO_ID", item.ContrattoId)
            RequireDate(result, "CONTRATTO_DATA", item.ContrattoData)
            RequireString(result, "CONTRATTO_TIPOP", item.ContrattoTipoP)
            RequireDateTime(result, "CONTRATTO_CHECKOUT_DATA", item.ContrattoCheckoutData)
            RequireString(result, "CONTRATTO_CHECKOUT_LUOGO_COD", item.ContrattoCheckoutLuogoCod)
            RequireString(result, "CONTRATTO_CHECKOUT_INDIRIZZO", item.ContrattoCheckoutIndirizzo)
            RequireDateTime(result, "CONTRATTO_CHECKIN_DATA", item.ContrattoCheckinData)
            RequireString(result, "CONTRATTO_CHECKIN_LUOGO_COD", item.ContrattoCheckinLuogoCod)
            RequireString(result, "CONTRATTO_CHECKIN_INDIRIZZO", item.ContrattoCheckinIndirizzo)
            RequireString(result, "OPERATORE_ID", item.OperatoreId)
            RequireString(result, "AGENZIA_ID", item.AgenziaId)
            RequireString(result, "AGENZIA_NOME", item.AgenziaNome)
            RequireString(result, "AGENZIA_LUOGO_COD", item.AgenziaLuogoCod)
            RequireString(result, "AGENZIA_INDIRIZZO", item.AgenziaIndirizzo)
            RequireString(result, "AGENZIA_RECAPITO_TEL", item.AgenziaRecapitoTel)
            RequireString(result, "VEICOLO_TIPO", item.VeicoloTipo)
            RequireString(result, "VEICOLO_MARCA", item.VeicoloMarca)
            RequireString(result, "VEICOLO_MODELLO", item.VeicoloModello)
            RequireString(result, "VEICOLO_TARGA", item.VeicoloTarga)
            RequireString(result, "CONDUCENTE_CONTRAENTE_COGNOME", item.ConducenteContraenteCognome)
            RequireString(result, "CONDUCENTE_CONTRAENTE_NOME", item.ConducenteContraenteNome)
            RequireDate(result, "CONDUCENTE_CONTRAENTE_NASCITA_DATA", item.ConducenteContraenteNascitaData)
            RequireString(result, "CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD", item.ConducenteContraenteNascitaLuogoCod)
            RequireString(result, "CONDUCENTE_CONTRAENTE_CITTADINANZA_COD", item.ConducenteContraenteCittadinanzaCod)
            RequireString(result, "CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD", item.ConducenteContraenteDocideTipoCod)
            RequireString(result, "CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO", item.ConducenteContraenteDocideNumero)
            RequireString(result, "CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD", item.ConducenteContraenteDocideLuogorilCod)
            RequireString(result, "CONDUCENTE_CONTRAENTE_PATENTE_NUMERO", item.ConducenteContraentePatenteNumero)
            RequireString(result, "CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD", item.ConducenteContraentePatenteLuogorilCod)

            ValidateLength(result, "CONTRATTO_ID", item.ContrattoId, 50)
            ValidateLength(result, "CONTRATTO_TIPOP", item.ContrattoTipoP, 1)
            ValidateLength(result, "CONTRATTO_CHECKOUT_LUOGO_COD", item.ContrattoCheckoutLuogoCod, 9)
            ValidateLength(result, "CONTRATTO_CHECKOUT_INDIRIZZO", item.ContrattoCheckoutIndirizzo, 150)
            ValidateLength(result, "CONTRATTO_CHECKIN_LUOGO_COD", item.ContrattoCheckinLuogoCod, 9)
            ValidateLength(result, "CONTRATTO_CHECKIN_INDIRIZZO", item.ContrattoCheckinIndirizzo, 150)
            ValidateLength(result, "OPERATORE_ID", item.OperatoreId, 50)
            ValidateLength(result, "AGENZIA_ID", item.AgenziaId, 30)
            ValidateLength(result, "AGENZIA_NOME", item.AgenziaNome, 70)
            ValidateLength(result, "AGENZIA_LUOGO_COD", item.AgenziaLuogoCod, 9)
            ValidateLength(result, "AGENZIA_INDIRIZZO", item.AgenziaIndirizzo, 150)
            ValidateLength(result, "AGENZIA_RECAPITO_TEL", item.AgenziaRecapitoTel, 20)
            ValidateLength(result, "VEICOLO_TIPO", item.VeicoloTipo, 1)
            ValidateLength(result, "VEICOLO_MARCA", item.VeicoloMarca, 50)
            ValidateLength(result, "VEICOLO_MODELLO", item.VeicoloModello, 100)
            ValidateLength(result, "VEICOLO_TARGA", item.VeicoloTarga, 15)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_COGNOME", item.ConducenteContraenteCognome, 50)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_NOME", item.ConducenteContraenteNome, 30)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD", item.ConducenteContraenteNascitaLuogoCod, 9)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_CITTADINANZA_COD", item.ConducenteContraenteCittadinanzaCod, 9)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD", item.ConducenteContraenteDocideTipoCod, 5)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO", item.ConducenteContraenteDocideNumero, 20)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD", item.ConducenteContraenteDocideLuogorilCod, 9)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_PATENTE_NUMERO", item.ConducenteContraentePatenteNumero, 20)
            ValidateLength(result, "CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD", item.ConducenteContraentePatenteLuogorilCod, 9)

            Return result
        End Function

        Private Shared Sub RequireString(result As ValidationResult, fieldName As String, value As String)
            If String.IsNullOrWhiteSpace(value) Then
                result.MissingFields.Add(fieldName)
            End If
        End Sub

        Private Shared Sub RequireDate(result As ValidationResult, fieldName As String, value As Nullable(Of DateTime))
            If Not value.HasValue Then
                result.MissingFields.Add(fieldName)
            End If
        End Sub

        Private Shared Sub RequireDateTime(result As ValidationResult, fieldName As String, value As Nullable(Of DateTime))
            If Not value.HasValue Then
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
