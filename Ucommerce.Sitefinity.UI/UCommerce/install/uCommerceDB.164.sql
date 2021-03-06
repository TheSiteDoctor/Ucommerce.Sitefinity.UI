IF EXISTS (SELECT * FROM sys.indexes WHERE name='UX_uCommerce_ProductCatalog_Name_ProductCatalogGroupId' AND object_id = OBJECT_ID('dbo.uCommerce_ProductCatalog'))
BEGIN
	DROP INDEX UX_uCommerce_ProductCatalog_Name_ProductCatalogGroupId 
	ON dbo.uCommerce_ProductCatalog;
END

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='UX_uCommerce_ProductCatalog_Name_ProductCatalogGroupId' AND object_id = OBJECT_ID('dbo.uCommerce_ProductCatalog'))
BEGIN
	CREATE UNIQUE INDEX UX_uCommerce_ProductCatalog_Name_ProductCatalogGroupId
	ON uCommerce_ProductCatalog(Name, ProductCatalogGroupId)
	WHERE Deleted = 0
END

/*Adds a definition field on the datatype*/

IF NOT EXISTS (SELECT * FROM sys.columns
  WHERE  object_id = OBJECT_ID(N'[dbo].[uCommerce_DataType]') 
         AND name = 'DefinitionId'
)
BEGIN
  ALTER TABLE uCommerce_DataType
    ADD DefinitionId int,
    FOREIGN KEY(DefinitionId) REFERENCES uCommerce_Definition(DefinitionId);
END