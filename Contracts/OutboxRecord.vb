Imports System

Namespace Contracts
    Public NotInheritable Class OutboxRecord
        Public Property Id As Long
        Public Property ContractNo As String
        Public Property LineNo As Long
        Public Property CargosContractId As String
        Public Property BranchId As String
        Public Property BranchEmail As String
        Public Property ContrattoId As String
        Public Property ContrattoData As Nullable(Of DateTime)
        Public Property ContrattoTipoP As String
        Public Property ContrattoCheckoutData As Nullable(Of DateTime)
        Public Property ContrattoCheckoutLuogoCod As String
        Public Property ContrattoCheckoutIndirizzo As String
        Public Property ContrattoCheckinData As Nullable(Of DateTime)
        Public Property ContrattoCheckinLuogoCod As String
        Public Property ContrattoCheckinIndirizzo As String
        Public Property OperatoreId As String
        Public Property AgenziaId As String
        Public Property AgenziaNome As String
        Public Property AgenziaLuogoCod As String
        Public Property AgenziaIndirizzo As String
        Public Property AgenziaRecapitoTel As String
        Public Property VeicoloTipo As String
        Public Property VeicoloMarca As String
        Public Property VeicoloModello As String
        Public Property VeicoloTarga As String
        Public Property ConducenteContraenteCognome As String
        Public Property ConducenteContraenteNome As String
        Public Property ConducenteContraenteNascitaData As Nullable(Of DateTime)
        Public Property ConducenteContraenteNascitaLuogoCod As String
        Public Property ConducenteContraenteCittadinanzaCod As String
        Public Property ConducenteContraenteDocideTipoCod As String
        Public Property ConducenteContraenteDocideNumero As String
        Public Property ConducenteContraenteDocideLuogorilCod As String
        Public Property ConducenteContraentePatenteNumero As String
        Public Property ConducenteContraentePatenteLuogorilCod As String
        Public Property Reason As String
        Public Property SnapshotHash As String
        Public Property RecordLine As String
        Public Property Status As String
        Public Property MissingFields As String
        Public Property LastError As String
        Public Property AttemptCount As Integer
        Public Property LastAttemptAt As Nullable(Of DateTime)
        Public Property NextRetryAt As Nullable(Of DateTime)
        Public Property LastMissingEmailAt As Nullable(Of DateTime)
        Public Property LastMissingFieldsHash As String
        Public Property LastRejectEmailAt As Nullable(Of DateTime)
        Public Property LastRejectHash As String
        Public Property CreatedAt As DateTime
    End Class
End Namespace
