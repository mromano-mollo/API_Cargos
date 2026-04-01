Imports API_Cargos.Contracts
Imports API_Cargos.Integration
Imports API_Cargos.Persistence
Imports API_Cargos.Validation
Imports System.Text
Imports System.Web.Script.Serialization

Namespace Verification
    Public NotInheritable Class SelfTestRunner
        Public Shared Sub Run()
            VerifyValidation()
            VerifyLookupResolution()
            VerifyRecordBuilder()
            VerifyCargosClientErrorParsing()
            VerifyReferenceTableParsing()
            VerifyCrypto()
        End Sub

        Private Shared Sub VerifyValidation()
            Dim service As New ValidationService()
            Dim result = service.Validate(New OutboxRecord())
            AssertTrue(Not result.IsValid, "Validation should fail for empty payload.")
            AssertTrue(result.MissingFields.Contains("CONTRATTO_ID"), "Validation should detect missing CONTRATTO_ID.")
        End Sub

        Private Shared Sub VerifyRecordBuilder()
            Dim builder As New RecordBuilder()
            Dim record = CreateSampleRecord()
            record.ContrattoCheckoutIndirizzo = "Via 1" & ChrW(&H00BA) & " Maggio, 39"
            record.ContrattoCheckinIndirizzo = "Corso dell" & ChrW(&H2019) & "Unit" & ChrW(&HE0) & " n. 7"
            record.AgenziaNome = "Nol" & ChrW(&HE8) & "ggio " & ChrW(&H201C) & "Test" & ChrW(&H201D) & " S.r.l."
            record.AgenziaIndirizzo = "Via 1" & ChrW(&H00BA) & " Maggio, 39"

            Dim line As String = builder.Build(record)
            AssertTrue(line.Length = 1505, "RecordBuilder must return a 1505-char line.")
            AssertTrue(line.StartsWith("CTRTEST001"), "RecordBuilder should place contract id at the beginning of the line.")
            AssertTrue(line.Contains("Via 1 Maggio 39"), "RecordBuilder should normalize unsupported address characters before send.")
            AssertTrue(line.Contains("Noleggio Test S r l"), "RecordBuilder should normalize unsupported free-text characters before send.")
            AssertTrue(Not line.Contains("º") AndAlso Not line.Contains("’") AndAlso Not line.Contains("è"), "RecordBuilder output should not contain unsupported special characters.")
        End Sub

        Private Shared Sub VerifyLookupResolution()
            Dim repository As New FakeReferenceTableRepository()
            Dim service As ICargosLookupService = New CargosLookupService(repository)
            Dim record = CreateSampleRecord()
            record.ContrattoTipoP = "contanti"
            record.ContrattoCheckoutLuogoCod = "Roma"
            record.ContrattoCheckinLuogoCod = "Roma"
            record.AgenziaLuogoCod = "Roma"
            record.VeicoloTipo = "Autovettura"
            record.ConducenteContraenteNascitaLuogoCod = "Roma"
            record.ConducenteContraenteCittadinanzaCod = "Italia"
            record.ConducenteContraenteDocideTipoCod = "Carta Identita"
            record.ConducenteContraenteDocideLuogorilCod = "Roma"
            record.ConducenteContraentePatenteLuogorilCod = "Roma"

            Dim validation As New ValidationResult()
            service.Resolve(record, validation)

            AssertTrue(validation.IsValid, "Lookup resolution should succeed for sample values.")
            AssertTrue(record.ContrattoTipoP = "P", "Lookup should resolve payment type to cached code.")
            AssertTrue(record.VeicoloTipo = "A", "Lookup should resolve vehicle type to cached code.")
            AssertTrue(record.ConducenteContraenteDocideTipoCod = "CI", "Lookup should resolve document type to cached code.")
            AssertTrue(record.ConducenteContraenteCittadinanzaCod = "100000100", "Lookup should resolve Italian citizenship to cached code.")

            Dim foreignRecord = CreateSampleRecord()
            foreignRecord.ContrattoCheckoutLuogoCod = "Roma"
            foreignRecord.ContrattoCheckinLuogoCod = "Roma"
            foreignRecord.AgenziaLuogoCod = "Roma"
            foreignRecord.ConducenteContraenteCittadinanzaCod = "FRANCIA"
            foreignRecord.ConducenteContraenteNascitaLuogoCod = "Roma"
            foreignRecord.ConducenteContraenteDocideLuogorilCod = "Roma"
            foreignRecord.ConducenteContraentePatenteLuogorilCod = "Roma"
            foreignRecord.ConducenteContraenteDocideTipoCod = "Carta Identita"

            Dim foreignValidation As New ValidationResult()
            service.Resolve(foreignRecord, foreignValidation)

            AssertTrue(foreignValidation.IsValid, "Foreign citizenship normalization should succeed when citizenship resolves. Errors: " & String.Join(" | ", foreignValidation.Errors))
            AssertTrue(foreignRecord.ConducenteContraenteCittadinanzaCod = "200000250", "Foreign citizenship should resolve to cached code.")
            AssertTrue(foreignRecord.ConducenteContraenteNascitaLuogoCod = foreignRecord.ConducenteContraenteCittadinanzaCod, "Birth place should be forced to citizenship code for foreign drivers.")
            AssertTrue(foreignRecord.ConducenteContraenteDocideLuogorilCod = foreignRecord.ConducenteContraenteCittadinanzaCod, "Document release place should be forced to citizenship code for foreign drivers.")
            AssertTrue(foreignRecord.ConducenteContraentePatenteLuogorilCod = foreignRecord.ConducenteContraenteCittadinanzaCod, "Driving license release place should be forced to citizenship code for foreign drivers.")

            Dim luogoValidation As New ValidationResult()
            Dim luogoCode = service.ResolveLuogoCode("Alba", "CN", "12051", String.Empty, "AGENZIA_LUOGO_COD", luogoValidation)
            AssertTrue(luogoValidation.IsValid, "Structured LUOGHI lookup should succeed.")
            AssertTrue(luogoCode = "405028001", "Structured LUOGHI lookup should resolve ALBA/CN/12051.")
        End Sub

        Private Shared Sub VerifyReferenceTableParsing()
            Dim decodedContent As String =
                "ID#Descrizione" & vbCrLf &
                "0#Carta di Credito" & vbCrLf &
                "1#Contanti" & vbCrLf
            Dim payload As String = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedContent))
            Dim json As String = "{""esito"":true,""errore"":null,""filename"":""TIPO_PAGAMENTO.dat"",""file"":""" & payload & """}"
            Dim wrappedJson As String = New JavaScriptSerializer().Serialize(json)

            Dim rows = CargosReferenceTableSyncService.ParseRowsFromApiResponse(json, "TIPO_PAGAMENTO")
            Dim wrappedRows = CargosReferenceTableSyncService.ParseRowsFromApiResponse(wrappedJson, "TIPO_PAGAMENTO")

            AssertTrue(rows.Count = 2, "Reference table parser should skip header and read data rows.")
            AssertTrue(rows(0).Code = "0", "Reference table parser should read the first code column.")
            AssertTrue(rows(1).Description = "Contanti", "Reference table parser should decode base64 content.")
            AssertTrue(wrappedRows.Count = 2, "Reference table parser should also handle JSON-string wrapped payloads.")
        End Sub

        Private Shared Sub VerifyCargosClientErrorParsing()
            Dim json As String =
                "[{""esito"":false,""transactionid"":null,""errore"":{""error"":""TracciatoErrato"",""error_description"":""Campo: AGENZIA_NOME - Vincolo SetCaratteri - Sono presenti caratteri non validi"",""error_code"":4,""timestamp"":""2026-03-12T17:45:40.7252783+01:00""}}]"

            Dim sendOutcomes = CargosClient.ParseLineResponse(1, json, True)
            AssertTrue(sendOutcomes.Count = 1, "CargosClient error parser should return one outcome.")
            AssertTrue(sendOutcomes(0).OutcomeType = CargosOutcomeType.DataError, "CargosClient should classify esito=false as DataError.")
            AssertTrue(sendOutcomes(0).ErrorMessage = "TracciatoErrato - Campo: AGENZIA_NOME - Vincolo SetCaratteri - Sono presenti caratteri non validi",
                "CargosClient should flatten nested error + error_description.")

            Dim checkOutcomes = CargosClient.ParseLineResponse(1, json, False)
            AssertTrue(checkOutcomes(0).OutcomeType = CargosOutcomeType.DataError, "Check parsing should not treat esito=false as success.")
        End Sub

        Private Shared Sub VerifyCrypto()
            Dim crypto As New CryptoService()
            Dim encrypted As String = crypto.EncryptAccessToken("sample-token", "12345678901234567890123456789012abcdefghijklmnop")
            AssertTrue(Not String.IsNullOrWhiteSpace(encrypted), "CryptoService must return a Base64 token.")
        End Sub

        Private Shared Function CreateSampleRecord() As OutboxRecord
            Return New OutboxRecord() With {
                .ContrattoId = "CTRTEST001",
                .ContrattoData = New DateTime(2026, 3, 6),
                .ContrattoTipoP = "P",
                .ContrattoCheckoutData = New DateTime(2026, 3, 6, 10, 0, 0),
                .ContrattoCheckoutLuogoCod = "ROMA001",
                .ContrattoCheckoutIndirizzo = "Via Roma 1",
                .ContrattoCheckinData = New DateTime(2026, 3, 7, 10, 0, 0),
                .ContrattoCheckinLuogoCod = "ROMA001",
                .ContrattoCheckinIndirizzo = "Via Roma 1",
                .OperatoreId = "OPER001",
                .AgenziaId = "AG001",
                .AgenziaNome = "Agenzia Test",
                .AgenziaLuogoCod = "ROMA001",
                .AgenziaIndirizzo = "Via Roma 1",
                .AgenziaRecapitoTel = "0612345678",
                .VeicoloTipo = "A",
                .VeicoloMarca = "Fiat",
                .VeicoloModello = "Panda",
                .VeicoloTarga = "AB123CD",
                .ConducenteContraenteCognome = "Rossi",
                .ConducenteContraenteNome = "Mario",
                .ConducenteContraenteNascitaData = New DateTime(1980, 1, 1),
                .ConducenteContraenteNascitaLuogoCod = "ROMA001",
                .ConducenteContraenteCittadinanzaCod = "ITA",
                .ConducenteContraenteDocideTipoCod = "CI",
                .ConducenteContraenteDocideNumero = "AA123456",
                .ConducenteContraenteDocideLuogorilCod = "ROMA001",
                .ConducenteContraentePatenteNumero = "RM1234567",
                .ConducenteContraentePatenteLuogorilCod = "ROMA001"
            }
        End Function

        Private Shared Sub AssertTrue(condition As Boolean, message As String)
            If Not condition Then
                Throw New InvalidOperationException(message)
            End If
        End Sub

        Private NotInheritable Class FakeReferenceTableRepository
            Implements ICargosReferenceTableRepository

            Public Function GetRows(tableId As Integer) As IList(Of CargosReferenceTableRow) Implements ICargosReferenceTableRepository.GetRows
                Select Case tableId
                    Case 1
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "405028001", .Description = "ALBA", .Column3 = "CN", .Column4 = "12051", .RawLine = "405028001#ALBA#CN#12051"},
                            New CargosReferenceTableRow With {.RowNumber = 2, .Code = "405058001", .Description = "ROMA", .Column3 = "RM", .Column4 = "00100", .RawLine = "405058001#ROMA#RM#00100"},
                            New CargosReferenceTableRow With {.RowNumber = 3, .Code = "100000100", .Description = "ITALIA", .Column3 = "EE", .RawLine = "100000100#ITALIA#EE"},
                            New CargosReferenceTableRow With {.RowNumber = 4, .Code = "200000250", .Description = "FRANCIA", .Column3 = "EE", .RawLine = "200000250#FRANCIA#EE"}
                        }
                    Case 2
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "A", .Description = "Autovettura"}
                        }
                    Case 3
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "CI", .Description = "Carta Identita"}
                        }
                    Case 0
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "P", .Description = "Contanti"}
                        }
                    Case Else
                        Return New List(Of CargosReferenceTableRow)()
                End Select
            End Function

            Public Sub ReplaceTable(definition As CargosReferenceTableDefinition, rows As IList(Of CargosReferenceTableRow), syncedAtUtc As DateTime) Implements ICargosReferenceTableRepository.ReplaceTable
            End Sub

            Public Sub MarkSyncFailure(definition As CargosReferenceTableDefinition, failureMessage As String, attemptedAtUtc As DateTime) Implements ICargosReferenceTableRepository.MarkSyncFailure
            End Sub
        End Class
    End Class
End Namespace
