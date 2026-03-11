/*
    Template for dbo.Cargos_Vista_Contratti
*/

CREATE OR ALTER VIEW [dbo].[Cargos_Vista_Contratti]

AS

SELECT * FROM dbo.Cargos_Vista_Contratti_MOLLO

UNION ALL

SELECT * FROM dbo.Cargos_Vista_Contratti_MANETTA

UNION ALL

SELECT * FROM dbo.Cargos_Vista_Contratti_PARMIANI
