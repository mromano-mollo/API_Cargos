Imports System.Globalization
Imports System.Linq
Imports API_Cargos.Contracts

Namespace Integration
    Public Interface IRecordBuilder
        Function Build(item As OutboxRecord) As String
    End Interface

    Public NotInheritable Class RecordBuilder
        Implements IRecordBuilder

        Private ReadOnly _specs As IList(Of FieldSpec)

        Public Sub New()
            _specs = BuildSpecs()
            Dim totalLength As Integer = _specs.Sum(Function(x) x.Length)
            If totalLength <> 1505 Then
                Throw New InvalidOperationException(String.Format("CaRGOS field specs must total 1505 characters. Current total: {0}.", totalLength))
            End If
        End Sub

        Public Function Build(item As OutboxRecord) As String Implements IRecordBuilder.Build
            If item Is Nothing Then
                Throw New ArgumentNullException(NameOf(item))
            End If

            Dim buffer As Char() = New String(" "c, 1505).ToCharArray()
            Dim offset As Integer = 0

            For Each spec In _specs
                Dim rawValue As String = String.Empty
                If spec.ValueProvider IsNot Nothing Then
                    rawValue = spec.ValueProvider(item)
                End If

                Dim normalized As String = Normalize(rawValue, spec.Length)
                normalized.CopyTo(0, buffer, offset, normalized.Length)
                offset += spec.Length
            Next

            Return New String(buffer)
        End Function

        Private Shared Function BuildSpecs() As IList(Of FieldSpec)
            Return New List(Of FieldSpec) From
            {
                New FieldSpec With {.Name = "CONTRATTO_ID", .Length = 50, .ValueProvider = Function(x) x.ContrattoId},
                New FieldSpec With {.Name = "CONTRATTO_DATA", .Length = 16, .ValueProvider = Function(x) FormatDateTime16(x.ContrattoData)},
                New FieldSpec With {.Name = "CONTRATTO_TIPOP", .Length = 1, .ValueProvider = Function(x) x.ContrattoTipoP},
                New FieldSpec With {.Name = "CONTRATTO_CHECKOUT_DATA", .Length = 16, .ValueProvider = Function(x) FormatDateTime16(x.ContrattoCheckoutData)},
                New FieldSpec With {.Name = "CONTRATTO_CHECKOUT_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) x.ContrattoCheckoutLuogoCod},
                New FieldSpec With {.Name = "CONTRATTO_CHECKOUT_INDIRIZZO", .Length = 150, .ValueProvider = Function(x) x.ContrattoCheckoutIndirizzo},
                New FieldSpec With {.Name = "CONTRATTO_CHECKIN_DATA", .Length = 16, .ValueProvider = Function(x) FormatDateTime16(x.ContrattoCheckinData)},
                New FieldSpec With {.Name = "CONTRATTO_CHECKIN_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) x.ContrattoCheckinLuogoCod},
                New FieldSpec With {.Name = "CONTRATTO_CHECKIN_INDIRIZZO", .Length = 150, .ValueProvider = Function(x) x.ContrattoCheckinIndirizzo},
                New FieldSpec With {.Name = "OPERATORE_ID", .Length = 50, .ValueProvider = Function(x) x.OperatoreId},
                New FieldSpec With {.Name = "AGENZIA_ID", .Length = 30, .ValueProvider = Function(x) x.AgenziaId},
                New FieldSpec With {.Name = "AGENZIA_NOME", .Length = 70, .ValueProvider = Function(x) x.AgenziaNome},
                New FieldSpec With {.Name = "AGENZIA_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) x.AgenziaLuogoCod},
                New FieldSpec With {.Name = "AGENZIA_INDIRIZZO", .Length = 150, .ValueProvider = Function(x) x.AgenziaIndirizzo},
                New FieldSpec With {.Name = "AGENZIA_RECAPITO_TEL", .Length = 20, .ValueProvider = Function(x) x.AgenziaRecapitoTel},
                New FieldSpec With {.Name = "VEICOLO_TIPO", .Length = 1, .ValueProvider = Function(x) x.VeicoloTipo},
                New FieldSpec With {.Name = "VEICOLO_MARCA", .Length = 50, .ValueProvider = Function(x) x.VeicoloMarca},
                New FieldSpec With {.Name = "VEICOLO_MODELLO", .Length = 100, .ValueProvider = Function(x) x.VeicoloModello},
                New FieldSpec With {.Name = "VEICOLO_TARGA", .Length = 15, .ValueProvider = Function(x) x.VeicoloTarga},
                New FieldSpec With {.Name = "VEICOLO_COLORE", .Length = 50, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "VEICOLO_GPS", .Length = 1, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "VEICOLO_BLOCCOM", .Length = 1, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_COGNOME", .Length = 50, .ValueProvider = Function(x) x.ConducenteContraenteCognome},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_NOME", .Length = 30, .ValueProvider = Function(x) x.ConducenteContraenteNome},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_NASCITA_DATA", .Length = 10, .ValueProvider = Function(x) FormatDate10(x.ConducenteContraenteNascitaData)},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) x.ConducenteContraenteNascitaLuogoCod},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_CITTADINANZA_COD", .Length = 9, .ValueProvider = Function(x) x.ConducenteContraenteCittadinanzaCod},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_RESIDENZA_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_RESIDENZA_INDIRIZZO", .Length = 150, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD", .Length = 5, .ValueProvider = Function(x) x.ConducenteContraenteDocideTipoCod},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO", .Length = 20, .ValueProvider = Function(x) x.ConducenteContraenteDocideNumero},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD", .Length = 9, .ValueProvider = Function(x) x.ConducenteContraenteDocideLuogorilCod},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_PATENTE_NUMERO", .Length = 20, .ValueProvider = Function(x) x.ConducenteContraentePatenteNumero},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD", .Length = 9, .ValueProvider = Function(x) x.ConducenteContraentePatenteLuogorilCod},
                New FieldSpec With {.Name = "CONDUCENTE_CONTRAENTE_RECAPITO_TEL", .Length = 20, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_COGNOME", .Length = 50, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_NOME", .Length = 30, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_NASCITA_DATA", .Length = 10, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_NASCITA_LUOGO_COD", .Length = 9, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_CITTADINANZA_COD", .Length = 9, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_DOCIDE_TIPO_COD", .Length = 5, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_DOCIDE_NUMERO", .Length = 20, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_DOCIDE_LUOGORIL_COD", .Length = 9, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_PATENTE_NUMERO", .Length = 20, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_PATENTE_LUOGORIL_COD", .Length = 9, .ValueProvider = Function(x) String.Empty},
                New FieldSpec With {.Name = "CONDUCENTE2_RECAPITO_TEL", .Length = 20, .ValueProvider = Function(x) String.Empty}
            }
        End Function

        Private Shared Function FormatDate10(value As Nullable(Of DateTime)) As String
            If Not value.HasValue Then
                Return String.Empty
            End If

            Return value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function FormatDateTime16(value As Nullable(Of DateTime)) As String
            If Not value.HasValue Then
                Return String.Empty
            End If

            Return value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Normalize(value As String, length As Integer) As String
            Dim safeValue As String = If(value, String.Empty).Trim()
            safeValue = safeValue.Replace(vbCr, " ").Replace(vbLf, " ").Replace(vbTab, " ")

            If safeValue.Length > length Then
                safeValue = safeValue.Substring(0, length)
            End If

            If safeValue.Length < length Then
                safeValue = safeValue.PadRight(length, " "c)
            End If

            Return safeValue
        End Function
    End Class
End Namespace
