Imports API_Cargos.Contracts

Namespace Integration
    Public Interface IAgencyCreateClient
        Function CreateAgency(item As AgencyOutboxRecord) As CargosLineOutcome
    End Interface
End Namespace
