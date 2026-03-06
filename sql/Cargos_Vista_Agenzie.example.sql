/*
    Template for dbo.Cargos_Vista_Agenzie

    Important:
    - The agency bootstrap does NOT require the view to expose the final Polizia code directly.
    - The view can expose a business/source value in AgenziaLuogoValue (for example comune/locality),
      and the app lookup service will resolve it to Agenzia.AGENZIA_LUOGO_COD through Cargos_Tabella_Righe.
    - You still need a real source column/table for the agency location. It cannot be derived safely from Address alone.
*/

CREATE OR ALTER VIEW [dbo].[Cargos_Vista_Agenzie]

AS

SELECT a.Code AS BranchId,
       CAST(NULL AS NVARCHAR(255)) AS BranchEmail,
	   a.Code AS AgenziaId,
       a.Name AS AgenziaNome,
	   CAST(NULL AS NVARCHAR(255)) AS AgenziaLuogoValue,
	   a.Address AS AgenziaIndirizzo,
	   a.City AS AgenziaCity,
	   a.County AS AgenziaCounty,
	   a.[post code] AS AgenziaPostCode,
	   ISNULL(b.Interno, '000') AS AgenziaRecapitoTel

  FROM filialiNolo a

  LEFT JOIN organigramma b
    ON b.codiceAzienda = '500'
   AND b.Cognome = 'FILIALE NOLEGGIO'
   AND b.Nome = a.name

 WHERE 1 = 1
   AND a.Attiva = 1
   AND a.Caldo = 0
   AND a.chimici = 0
   AND a.virtuale = 0
   AND a.Code NOT IN ('A311 ATTRE')
   AND a.Code NOT LIKE ('P% %');
