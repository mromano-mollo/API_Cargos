Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports API_Cargos.Contracts
Imports API_Cargos.Persistence
Imports API_Cargos.Validation

Namespace Integration
    Public Interface ICargosLookupService
        Sub Resolve(item As OutboxRecord, validation As ValidationResult)
        Function ResolveLuogoCode(city As String, county As String, postCode As String, fallbackValue As String, fieldName As String, validation As ValidationResult) As String
    End Interface

    Public NotInheritable Class CargosLookupService
        Implements ICargosLookupService

        Private Const TipoPagamentoTableId As Integer = 0
        Private Const LuoghiTableId As Integer = 1
        Private Const TipoVeicoloTableId As Integer = 2
        Private Const TipoDocumentoTableId As Integer = 3
        Private Const ItaliaCode As String = "100000100"

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
            item.ConducenteContraenteCittadinanzaCod = ResolveValue(LuoghiTableId, item.ConducenteContraenteCittadinanzaCod, "CONDUCENTE_CONTRAENTE_CITTADINANZA_COD", validation)
            item.ConducenteContraenteDocideTipoCod = ResolveValue(TipoDocumentoTableId, item.ConducenteContraenteDocideTipoCod, "CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD", validation)

            If IsForeignCitizenshipCode(item.ConducenteContraenteCittadinanzaCod) Then
                item.ConducenteContraenteNascitaLuogoCod = item.ConducenteContraenteCittadinanzaCod
                item.ConducenteContraenteDocideLuogorilCod = item.ConducenteContraenteCittadinanzaCod
                item.ConducenteContraentePatenteLuogorilCod = item.ConducenteContraenteCittadinanzaCod
            Else
                item.ConducenteContraenteNascitaLuogoCod = ResolveValue(LuoghiTableId, item.ConducenteContraenteNascitaLuogoCod, "CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD", validation)
                item.ConducenteContraenteDocideLuogorilCod = ResolveValue(LuoghiTableId, item.ConducenteContraenteDocideLuogorilCod, "CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD", validation)
                item.ConducenteContraentePatenteLuogorilCod = ResolveValue(LuoghiTableId, item.ConducenteContraentePatenteLuogorilCod, "CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD", validation)
            End If
        End Sub

        Public Function ResolveLuogoCode(city As String, county As String, postCode As String, fallbackValue As String, fieldName As String, validation As ValidationResult) As String Implements ICargosLookupService.ResolveLuogoCode
            Dim rows = GetRows(LuoghiTableId)
            If rows.Count = 0 Then
                validation.Errors.Add(String.Format("{0} lookup cache is empty for table {1}.", fieldName, LuoghiTableId))
                Return fallbackValue
            End If

            If Not String.IsNullOrWhiteSpace(fallbackValue) Then
                Dim directCode = rows.FirstOrDefault(Function(r) Normalize(r.Code) = Normalize(fallbackValue))
                If directCode IsNot Nothing Then
                    Return directCode.Code
                End If
            End If

            Dim normalizedCity As String = Normalize(city)
            Dim normalizedCounty As String = Normalize(county)
            Dim normalizedPostCode As String = Normalize(postCode)

            Dim candidates = rows.Where(Function(r) MatchesStructuredLuogo(r, normalizedCity, normalizedCounty, normalizedPostCode)).
                GroupBy(Function(r) r.Code, StringComparer.OrdinalIgnoreCase).
                Select(Function(g) g.First()).
                ToList()

            If candidates.Count = 1 Then
                Return candidates(0).Code
            End If

            If candidates.Count > 1 AndAlso Not String.IsNullOrWhiteSpace(normalizedPostCode) Then
                Dim narrowed = candidates.Where(Function(r) GetSearchText(r).Contains(normalizedPostCode)).ToList()
                If narrowed.Count = 1 Then
                    Return narrowed(0).Code
                End If
            End If

            If candidates.Count > 1 Then
                validation.Errors.Add(String.Format("{0} lookup is ambiguous for city '{1}', county '{2}', postcode '{3}'.", fieldName, city, county, postCode))
                Return fallbackValue
            End If

            If Not String.IsNullOrWhiteSpace(fallbackValue) Then
                Return ResolveValue(LuoghiTableId, fallbackValue, fieldName, validation)
            End If

            validation.Errors.Add(String.Format("{0} lookup value was not found for city '{1}', county '{2}', postcode '{3}'.", fieldName, city, county, postCode))
            Return fallbackValue
        End Function

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
                Normalize(row.Column8),
                Normalize(row.RawLine)
            }.Where(Function(v) Not String.IsNullOrWhiteSpace(v))
        End Function

        Private Shared Function GetSearchText(row As CargosReferenceTableRow) As String
            Return String.Join(" ", GetComparableValues(row))
        End Function

        Private Shared Function MatchesStructuredLuogo(row As CargosReferenceTableRow, normalizedCity As String, normalizedCounty As String, normalizedPostCode As String) As Boolean
            Dim searchText As String = GetSearchText(row)
            If String.IsNullOrWhiteSpace(normalizedCity) Then
                Return False
            End If

            If Not searchText.Contains(normalizedCity) Then
                Return False
            End If

            If Not String.IsNullOrWhiteSpace(normalizedCounty) AndAlso Not searchText.Contains(normalizedCounty) Then
                Return False
            End If

            If Not String.IsNullOrWhiteSpace(normalizedPostCode) AndAlso Not searchText.Contains(normalizedPostCode) Then
                Return False
            End If

            Return True
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

        Private Shared Function IsForeignCitizenshipCode(value As String) As Boolean
            Dim normalized As String = Normalize(value)
            If String.IsNullOrWhiteSpace(normalized) OrElse normalized = ItaliaCode Then
                Return False
            End If

            Return normalized.Length = 9 AndAlso normalized.All(Function(ch) Char.IsDigit(ch))
        End Function
    End Class
End Namespace
