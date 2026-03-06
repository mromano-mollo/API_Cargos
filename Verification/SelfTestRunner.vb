Imports API_Cargos.Contracts
Imports API_Cargos.Integration
Imports API_Cargos.Persistence
Imports API_Cargos.Validation

Namespace Verification
    Public NotInheritable Class SelfTestRunner
        Public Shared Sub Run()
            VerifyValidation()
            VerifyLookupResolution()
            VerifyRecordBuilder()
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
            Dim line As String = builder.Build(CreateSampleRecord())
            AssertTrue(line.Length = 1505, "RecordBuilder must return a 1505-char line.")
            AssertTrue(line.StartsWith("CTRTEST001"), "RecordBuilder should place contract id at the beginning of the line.")
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
            record.ConducenteContraenteDocideTipoCod = "Carta Identita"
            record.ConducenteContraenteDocideLuogorilCod = "Roma"
            record.ConducenteContraentePatenteLuogorilCod = "Roma"

            Dim validation As New ValidationResult()
            service.Resolve(record, validation)

            AssertTrue(validation.IsValid, "Lookup resolution should succeed for sample values.")
            AssertTrue(record.AgenziaLuogoCod = "405028001", "Lookup should resolve LUOGHI to cached code.")
            AssertTrue(record.ContrattoTipoP = "P", "Lookup should resolve payment type to cached code.")
            AssertTrue(record.VeicoloTipo = "A", "Lookup should resolve vehicle type to cached code.")
            AssertTrue(record.ConducenteContraenteDocideTipoCod = "CI", "Lookup should resolve document type to cached code.")
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
                    Case 2
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "405028001", .Description = "Roma"}
                        }
                    Case 9
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "A", .Description = "Autovettura"}
                        }
                    Case 10
                        Return New List(Of CargosReferenceTableRow) From {
                            New CargosReferenceTableRow With {.RowNumber = 1, .Code = "CI", .Description = "Carta Identita"}
                        }
                    Case 11
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
