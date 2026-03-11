/*
    Template for dbo.Cargos_Vista_Contratti
*/

CREATE OR ALTER VIEW [dbo].[Cargos_Vista_Contratti]

AS

SELECT HCTR.[Contract No_] AS ContractNo,
       LCTR.[Line No_] AS ContractLineNo,
       CONCAT(HCTR.[Contract No_], '-', LCTR.[Line No_]) AS CONTRATTO_ID,
	     HCTR.[Starting Date] AS CONTRATTO_DATA,
	     CASE
		    WHEN dbo.GetMetodoPagamento(HCTR.[Payment Method Code]) LIKE '%Credito%' THEN '0'
		    WHEN HCTR.[Payment Method Code] LIKE '%CONTANTI%' THEN '1'
		    WHEN dbo.GetMetodoPagamento(HCTR.[Payment Method Code]) LIKE '%Bonifico%' THEN '3'
		    WHEN dbo.GetMetodoPagamento(HCTR.[Payment Method Code]) LIKE '%RID%' THEN '4'
		    ELSE '9'
	     END AS CONTRATTO_TIPOP,
	     LCTR.[Start Rental Period] AS CONTRATTO_CHECKOUT_DATA,
	     Cargos_CheckOut.AgenziaLuogoValue AS CONTRATTO_CHECKOUT_LUOGO_COD,
	     Cargos_CheckOut.AgenziaIndirizzo AS CONTRATTO_CHECKOUT_INDIRIZZO,
	     LCTR.[End Rental Period] AS CONTRATTO_CHECKIN_DATA,
	     Cargos_CheckIn.AgenziaLuogoValue AS CONTRATTO_CHECKIN_LUOGO_COD,
	     Cargos_CheckIn.AgenziaIndirizzo AS CONTRATTO_CHECKIN_INDIRIZZO,
	     'C00000818' AS OPERATORE_ID, -- CAPIRE CON GIAN
	     Cargos_CheckOut.AgenziaId AS AGENZIA_ID,
	     Cargos_CheckOut.AgenziaNome AS AGENZIA_NOME,
	     Cargos_CheckOut.AgenziaLuogoValue AS AGENZIA_LUOGO_COD,
	     Cargos_CheckOut.AgenziaIndirizzo AS AGENZIA_INDIRIZZO,
	     Cargos_CheckOut.AgenziaRecapitoTel AS AGENZIA_RECAPITO_TEL,
	     CASE
		    WHEN dbo.GetTipoVeicolo([Cod_ Ragg_ Macrotipo]) LIKE '%AUTOCARR%' THEN '0'
		    ELSE 'A'
	     END AS VEICOLO_TIPO,
	     Obj.Marca AS VEICOLO_MARCA,
	     Obj.Modello AS VEICOLO_MODELLO,
	     Obj.Targa AS VEICOLO_TARGA,
	     C.[Surname] AS CONDUCENTE_CONTRAENTE_COGNOME,
	     C.[Name] AS CONDUCENTE_CONTRAENTE_NOME,
	     CExt.[MOL001 Data Nascita$7faa03b3-b4e5-4f14-b271-d84574b763cf] AS CONDUCENTE_CONTRAENTE_NASCITA_DATA,
	     dbo.CargosGetLuogo(CExt.[MOL001 Luogo Nascita$7faa03b3-b4e5-4f14-b271-d84574b763cf], 0) AS CONDUCENTE_CONTRAENTE_NASCITA_LUOGO_COD,
	     ISNULL(dbo.CargosGetLuogo(CExt.[MOL001 Cittadinanza$7faa03b3-b4e5-4f14-b271-d84574b763cf], 1), dbo.CargosGetLuogo(LEFT(CExt.[MOL001 Cittadinanza$7faa03b3-b4e5-4f14-b271-d84574b763cf], 4), 1)) AS CONDUCENTE_CONTRAENTE_CITTADINANZA_COD,
	     dbo.CargosGetDocID(CExt.[MOL001 Identity Document Type$7faa03b3-b4e5-4f14-b271-d84574b763cf]) AS CONDUCENTE_CONTRAENTE_DOCIDE_TIPO_COD, -- PRENDERE DA TABELLA DOCUMENTI POLIZIA
	     CExt.[MOL001 Identity Document Nr_$7faa03b3-b4e5-4f14-b271-d84574b763cf] AS CONDUCENTE_CONTRAENTE_DOCIDE_NUMERO,
	     dbo.CargosGetLuogo(CExt.[MOL001 Identity Doc Rel_ Place$7faa03b3-b4e5-4f14-b271-d84574b763cf], 0) AS CONDUCENTE_CONTRAENTE_DOCIDE_LUOGORIL_COD,
	     CExt.[MOL001 Driving License Nr_$7faa03b3-b4e5-4f14-b271-d84574b763cf] AS CONDUCENTE_CONTRAENTE_PATENTE_NUMERO,
	     dbo.CargosGetLuogo(CExt.[MOL001 Driving License Place$7faa03b3-b4e5-4f14-b271-d84574b763cf], 0) AS CONDUCENTE_CONTRAENTE_PATENTE_LUOGORIL_COD

  FROM BC.dbo.[Mollo Srl$AR Contract Header] HCTR WITH(NOLOCK)

  LEFT JOIN BC.dbo.[Mollo Srl$AR Contract Line] LCTR WITH(NOLOCK)
    ON HCTR.[Contract Type] = LCTR.[Contract Type]
   AND HCTR.[Contract No_] = LCTR.[Contract No_]

  LEFT JOIN [BC-SQLSRV01].[MOLLO].[dbo].[Mollo$Contact$437dbf0e-84ff-417a-965d-ed2bb9650972] C WITH(NOLOCK)
    ON C.[no_] = HCTR.[Cargos Contatto 1]

  LEFT JOIN [BC-SQLSRV01].[MOLLO].[dbo].[Mollo$Contact$437dbf0e-84ff-417a-965d-ed2bb9650972$ext] CExt WITH(NOLOCK)
    ON CExt.[no_] = C.[no_]

  LEFT JOIN Cargos_Agenzie Cargos_CheckOut WITH(NOLOCK)
    ON Cargos_CheckOut.AgenziaId = LCTR.[Ubicazione Consegna] COLLATE Latin1_General_100_CI_AS

  LEFT JOIN Cargos_Agenzie Cargos_CheckIn WITH(NOLOCK)
    ON Cargos_CheckIn.AgenziaId = ISNULL(LCTR.[Ubicazione Reso], LCTR.[Ubicazione Consegna]) COLLATE Latin1_General_100_CI_AS

  LEFT JOIN BC.dbo.[Mollo Srl$AR Object Card] Obj WITH(NOLOCK)
    ON Obj.[Object No_] = LCTR.No_

  LEFT JOIN BC.dbo.[Mollo Srl$AR Object Type] ObjType WITH(NOLOCK)
    ON ObjType.[Object No_] = Obj.[Object Type]

 WHERE 1 = 1
   AND HCTR.[Contract Type] = 1
   AND HCTR.[Status] = 1
   AND LCTR.Type = 6
   AND LCTR.[Entry Status] = 2
   AND ObjType.[CargosInfo] = 1
