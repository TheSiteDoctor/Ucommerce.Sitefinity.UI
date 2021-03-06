GO
DELETE from uCommerce_ProductDescriptionProperty where Value = ''
GO
DELETE from uCommerce_ProductDescriptionProperty where Value is null
GO
DELETE pdp
FROM uCommerce_ProductDescriptionProperty pdp
INNER JOIN (SELECT
                pdp1.ProductDescriptionPropertyId,pdp1.ProductDescriptionId,pdp1.ProductDefinitionFieldId,ROW_NUMBER() OVER(PARTITION BY pdp1.ProductdescriptionId,pdp1.ProductDefinitionFieldId ORDER BY pdp1.ProductdescriptionId,pdp1.ProductDefinitionFieldId) AS RowRank
                FROM uCommerce_ProductDescriptionProperty pdp1
                    INNER JOIN (SELECT
                                    ProductDescriptionId,ProductDefinitionFieldId, COUNT(*) AS CountOf
                                    FROM uCommerce_ProductDescriptionProperty
                                    GROUP BY ProductDescriptionId,ProductDefinitionFieldId
                                    HAVING COUNT(*)>1
                                ) dt ON pdp1.ProductDescriptionId=dt.ProductDescriptionId and pdp1.ProductDefinitionFieldId=dt.ProductDefinitionFieldId
            ) dt2 ON pdp.ProductDescriptionPropertyId=dt2.ProductDescriptionPropertyId
WHERE dt2.RowRank!=1
GO
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE CONSTRAINT_NAME = 'ProductDescriptionProperty_Unique_ProductDescriptionId_ProductDefinitionFieldId')
BEGIN
	ALTER TABLE uCommerce_ProductDescriptionProperty ADD CONSTRAINT
				ProductDescriptionProperty_Unique_ProductDescriptionId_ProductDefinitionFieldId UNIQUE NONCLUSTERED
		(
					ProductDescriptionId,ProductDefinitionFieldId
		)
END