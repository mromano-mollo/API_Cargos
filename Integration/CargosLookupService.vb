Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports API_Cargos.Contracts
Imports API_Cargos.Persistence
Imports API_Cargos.Validation

Namespace Integration
    Public Interface ICargosLookupService
        Sub Resolve(item As OutboxRecord, validation As ValidationResult)
    End Interface

    Public NotInheritable Class CargosLookupService
        Implements ICargosLookupService

        Private Const LuoghiTableId As Integer = 2
        Private Const TipoVeicoloTableId As Integer = 9
        Private Const TipoDocumentoTableId As Integer = 10
        Private Const TipoPagamentoTableId As Integer = 11

        Private ReadOnly _repository As ICargosReferenceTableRepository
        Private ReadOnly _cache As New Dictionary(Of Integer, IList(Of CargosReferenceTableRow))()
        Private ReadOnly _syncRoot As New Object()

        Public Sub New(repository As ICargosReferenceTableRepository)
            _repository = repository
        End Sub

        Public Sub Resolve(item As OutboxRecord, validation As ValidationResult) Implements ICargosLookupService.Resolve
            If item Is Nothing Then
                Throw New ArgumentNullException(NameOf(item))
            End If

            If validation Is Nothing Then
                Throw New ArgumentNullException(NameOf(validation))
            End If

            item.ContrattoTipoP = ResolveValue(TipoPagamentoTableId, item.ContrattoTipoP, "CONTRATTO_TIPOP", validation)
            item.ContrattoCheckoutLuogoCod = ResolveValue(LuoghiTableId, item.ContrattoCheckoutLuogoCod, "CONTRATTO_CHECKOUT_LUOGO_COD", validation)
            item.ContrattoCheckinLuogoCod = ResolveValue(LuoghiTableId, item.ContrattoCheckinLuogoCod, "CONTRATTO_CHECKIN_LUOGO_COD", validation)
            item.AgenziaLuogoCod = ResolveValue(LuoghiTableId, item.AgenziaLuogoCod, "AGENZIA_LUOGO_COD", validation)
            item.VeicoloTipo = ResolveValue(TipoVeicoloTableId, item.VeicoloTipo, "VEICOLO_TIPO", validation)
            item.ConducenteContraenteNascitaLuogoCod = ResolveValue(LuoghiTableId, item.ConducenteContraenteNascitaLuogoCod, "CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD", validation)
            item.ConducenteContraenteDocideTipoCod = ResolveValue(TipoDocumentoTableId, item.ConducenteContraenteDocideTipoCod, "CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD", validation)
            item.ConducenteContraenteDocideLuogorilCod = ResolveValue(LuoghiTableId, item.ConducenteContraenteDocideLuogorilCod, "CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD", validation)
            item.ConducenteContraentePatenteLuogorilCod = ResolveValue(LuoghiTableId, item.ConducenteContraentePatenteLuogorilCod, "CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD", validation)
        End Sub

        Private Function ResolveValue(tableId As Integer, rawValue As String, fieldName As String, validation As ValidationResult) As String
            If String.IsNullOrWhiteSpace(rawValue) Then
                Return rawValue
            End If

            Dim rows = GetRows(tableId)
            If rows.Count = 0 Then
                validation.Errors.Add(String.Format("{0} lookup cache is empty for table {1}.", fieldName, tableId))
                Return rawValue
            End If

            Dim normalizedInput As String = Normalize(rawValue)

            Dim exactCodeMatch = rows.FirstOrDefault(Function(r) Normalize(r.Code) = normalizedInput)
            If exactCodeMatch IsNot Nothing Then
                Return exactCodeMatch.Code
            End If

            Dim exactTextMatches = rows.
                Where(Function(r) MatchesExactly(r, normalizedInput)).
                GroupBy(Function(r) r.Code, StringComparer.OrdinalIgnoreCase).
                Select(Function(g) g.First()).
                ToList()

            If exactTextMatches.Count = 1 Then
                Return exactTextMatches(0).Code
            End If

            If exactTextMatches.Count > 1 Then
                validation.Errors.Add(String.Format("{0} lookup is ambiguous for value '{1}'.", fieldName, rawValue))
                Return rawValue
            End If

            Dim containsMatches = rows.
                Where(Function(r) MatchesContains(r, normalizedInput)).
                GroupBy(Function(r) r.Code, StringComparer.OrdinalIgnoreCase).
                Select(Function(g) g.First()).
                ToList()

            If containsMatches.Count = 1 Then
                Return containsMatches(0).Code
            End If

            If containsMatches.Count > 1 Then
                validation.Errors.Add(String.Format("{0} lookup contains multiple matches for value '{1}'.", fieldName, rawValue))
                Return rawValue
            End If

            validation.Errors.Add(String.Format("{0} lookup value '{1}' was not found in CaRGOS table {2}.", fieldName, rawValue, tableId))
            Return rawValue
        End Function

        Private Function GetRows(tableId As Integer) As IList(Of CargosReferenceTableRow)
            SyncLock _syncRoot
                If Not _cache.ContainsKey(tableId) Then
                    _cache(tableId) = _repository.GetRows(tableId)
                End If

                Return _cache(tableId)
            End SyncLock
        End Function

        Private Shared Function MatchesExactly(row As CargosReferenceTableRow, normalizedInput As String) As Boolean
            Return GetComparableValues(row).Any(Function(v) v = normalizedInput)
        End Function

        Private Shared Function MatchesContains(row As CargosReferenceTableRow, normalizedInput As String) As Boolean
            If normalizedInput.Length < 3 Then
                Return False
            End If

            Return GetComparableValues(row).Any(Function(v) v.Contains(normalizedInput))
        End Function

        Private Shared Function GetComparableValues(row As CargosReferenceTableRow) As IEnumerable(Of String)
            Return New String() {
                Normalize(row.Code),
                Normalize(row.Description),
                Normalize(row.Column3),
                Normalize(row.Column4),
                Normalize(row.Column5),
                Normalize(row.Column6),
                Normalize(row.Column7),
                Normalize(row.Column8)
            }.Where(Function(v) Not String.IsNullOrWhiteSpace(v))
        End Function

        Private Shared Function Normalize(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then
                Return String.Empty
            End If

            Dim normalized As String = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD)
            Dim builder As New StringBuilder()

            For Each ch As Char In normalized
                Dim category = CharUnicodeInfo.GetUnicodeCategory(ch)
                If category <> UnicodeCategory.NonSpacingMark Then
                    builder.Append(ch)
                End If
            Next

            Return builder.ToString().Normalize(NormalizationForm.FormC)
        End Function
    End Class
End Namespace
