/*
    Template for dbo.Cargos_Vista_Agenzie

    Important:
    - The preferred source for luogo resolution is AgenziaCity + AgenziaCounty (+ AgenziaPostCode as tie-breaker).
    - AgenziaLuogoValue is now only an optional direct/fallback value and is limited to 9 chars.
    - You still need a real source column/table for the agency location. It cannot be derived safely from Address alone.
*/

CREATE OR ALTER VIEW [dbo].[Cargos_Vista_Agenzie]

AS

SELECT a.Code AS BranchId,
       CAST(NULL AS NVARCHAR(255)) AS BranchEmail,
	     CAST(a.Code AS NVARCHAR(50)) AS AgenziaId,
       CAST(a.Name AS NVARCHAR(70)) AS AgenziaNome,
	     CAST(ISNULL(c.Code, d.Code) AS NVARCHAR(9)) AS AgenziaLuogoValue,
	     a.City AS AgenziaCity,
	     a.County AS AgenziaCounty,
	     a.[post code] AS AgenziaPostCode,
	     CAST(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
         a.Address,
	     '/', ''), ',', ''), '.', ''), '-', ''), '+', ''), '\', ''), '#', ''), '[', ''), ']', ''), '°', ''), '''', '') AS NVARCHAR(150)) AS AgenziaIndirizzo,
	     CAST(ISNULL(REPLACE(REPLACE(
          -- Step 1: Taglia la stringa prima di "int" (se esiste)
          CASE 
              WHEN CHARINDEX('int', LOWER(interno)) > 0 
              THEN LEFT(interno, CHARINDEX('int', LOWER(interno)) - 1)
              ELSE interno -- Se non c'è "int", prende tutto il campo
          END, 
		   ' ', ''), '.', ''), '000') AS NVARCHAR(20)) AS AgenziaRecapitoTel

  FROM filialiNolo a WITH(NOLOCK)

  LEFT JOIN organigramma b WITH(NOLOCK)
    ON b.codiceAzienda = '500'
   AND b.Cognome = 'FILIALE NOLEGGIO'
   AND b.Nome = a.name

 OUTER APPLY (
    SELECT TOP 1 c.Code
    FROM Cargos_Tabella_Righe c WITH(NOLOCK)
    WHERE REPLACE(a.City, '''','') = REPLACE(c.Description, '''','')
	  AND a.County = c.Column3
    ORDER BY c.Code 
 ) AS c

 OUTER APPLY (
    SELECT TOP 1 d.Code
    FROM Cargos_Tabella_Righe d WITH(NOLOCK)
    WHERE REPLACE(a.Name, '''','') = REPLACE(d.Description, '''','')
	  AND a.County = d.Column3
    ORDER BY d.Code 
 ) AS d

 WHERE 1 = 1
   AND a.Attiva = 1
   AND a.Caldo = 0
   AND a.chimici = 0
   AND a.virtuale = 0
   AND a.Code NOT IN ('A311 ATTRE')
   AND a.Code NOT LIKE ('P% %')
   AND c.Code IS NOT NULL;